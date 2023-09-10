using DZarsky.CommonLibraries.AzureFunctions.Configuration;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Models;
using DZarsky.NotesFunction.Services.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UltraMapper;

namespace DZarsky.NotesFunction.Services
{
	public sealed class NoteService
	{
		private readonly CosmosClient _db;
		private readonly CosmosConfiguration _config;
		private readonly ILogger<NoteService> _logger;
		private readonly Mapper _mapper;

		public NoteService(CosmosClient db, CosmosConfiguration config, ILogger<NoteService> logger)
		{
			_db = db;
			_config = config;
			_logger = logger;
			_mapper = new Mapper();
		}

		public async Task<GenericResult<NoteDto>> Create(NoteDto note, string userId)
		{
			var container = GetContainer();

			var response = await container.CreateItemAsync(new Note
			{
				Id = Guid.NewGuid().ToString(),
				Title = note.Title,
				Text = note.Text ?? string.Empty,
				EncodedText = note.EncodedText,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				UserId = userId,
				IsEncrypted = note.IsEncrypted
			});

			if (response.StatusCode == HttpStatusCode.Created)
			{
				return new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(response.Resource));
			}

			_logger.LogError($"Create note request failed with status {response.StatusCode}",
				JsonConvert.SerializeObject(response));
			return new GenericResult<NoteDto>(ResultStatus.Failed);
		}

		public async Task<GenericResult<NoteDto>> Update(NoteDto note, string userId)
		{
			if (string.IsNullOrWhiteSpace(note.Id))
			{
				return new GenericResult<NoteDto>(ResultStatus.BadRequest);
			}

			var container = GetContainer();

			var noteById = container
				.GetItemLinqQueryable<Note>()
				.Where(x => x.UserId == userId && x.Id == note.Id)
				.ToFeedIterator();

			if (!(await noteById.ReadNextAsync()).Any())
			{
				_logger.LogInformation(
					$"Unauthorized access to note {note.Id} by user {userId} with action {nameof(Update)}");
				return new GenericResult<NoteDto>(ResultStatus.NotFound);
			}

			var dbNote = _mapper.Map<Note>(note);
			dbNote.UpdatedAt = DateTime.UtcNow;
			dbNote.UserId = userId;

			var response = await container.ReplaceItemAsync(dbNote, note.Id);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				return new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(response.Resource));
			}

			_logger.LogError($"Create note request failed with status {response.StatusCode}",
				JsonConvert.SerializeObject(response));
			return new GenericResult<NoteDto>(ResultStatus.Failed);
		}

		public async Task<GenericResult<IList<NoteDto>>> List(string userId, bool getDeleted = false)
		{
			var container = GetContainer();

			var notes = new List<Note>();

			var response = container
				.GetItemLinqQueryable<Note>()
				.Where(x => x.UserId == userId && (getDeleted || !x.IsDeleted))
				.ToFeedIterator();

			notes.AddRange(await response.ReadNextAsync());

			return new GenericResult<IList<NoteDto>>(ResultStatus.Success, _mapper.Map<List<NoteDto>>(notes));
		}

		public async Task<GenericResult<NoteDto>> Get(string? id, string userId)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				return new GenericResult<NoteDto>(ResultStatus.BadRequest);
			}

			var container = GetContainer();

			var notes = container
				.GetItemLinqQueryable<Note>()
				.Where(x => x.UserId == userId && x.Id == id)
				.ToFeedIterator();

			var note = (await notes.ReadNextAsync()).Resource.FirstOrDefault();

			return note is null
				? new GenericResult<NoteDto>(ResultStatus.NotFound)
				: new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(note));
		}

		public async Task<GenericResult> Delete(string? id, string userId)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				return new GenericResult<NoteDto>(ResultStatus.BadRequest);
			}

			var container = GetContainer();

			var noteById = container
				.GetItemLinqQueryable<Note>()
				.Where(x => x.UserId == userId && x.Id == id)
				.ToFeedIterator();

			if (!(await noteById.ReadNextAsync()).Any())
			{
				_logger.LogInformation(
					$"Unauthorized access on note {id} by user {userId} with action {nameof(Delete)}");
				return new GenericResult(ResultStatus.NotFound);
			}

			var response = await container.PatchItemAsync<Note>(id, new PartitionKey(id),
				new List<PatchOperation>
				{
					PatchOperation.Replace("/IsDeleted", true)
				});

			if (response.StatusCode == HttpStatusCode.OK)
			{
				return new GenericResult(ResultStatus.Success);
			}

			_logger.LogError($"Delete note request failed with status {response.StatusCode}",
				JsonConvert.SerializeObject(response));
			return new GenericResult(ResultStatus.Failed);
		}

		private Container GetContainer()
		{
			return _db.GetContainer(_config.DatabaseID, Constants.NotesContainerId);
		}
	}
}
