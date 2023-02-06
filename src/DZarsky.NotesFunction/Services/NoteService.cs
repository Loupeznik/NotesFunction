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
                Text = note.Text,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId
            });

            if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                _logger.LogError($"Create note request failed with status {response.StatusCode}", JsonConvert.SerializeObject(response));
                return new GenericResult<NoteDto>(ResultStatus.Failed);
            }

            return new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(response.Resource));
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
                _logger.LogInformation($"Unauthorized access to note {note.Id} by user {userId} with action {nameof(Update)}");
                return new GenericResult<NoteDto>(ResultStatus.NotFound);
            }

            var dbNote = _mapper.Map<Note>(note);
            dbNote.UpdatedAt = DateTime.UtcNow;

            var response = await container.ReplaceItemAsync(dbNote, note.Id);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"Create note request failed with status {response.StatusCode}", JsonConvert.SerializeObject(response));
                return new GenericResult<NoteDto>(ResultStatus.Failed);
            }

            return new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(response.Resource));
        }

        public async Task<GenericResult<IList<NoteDto>>> List(string userId)
        {
            var container = GetContainer();

            var notes = new List<Note>();

            var response = container
                .GetItemLinqQueryable<Note>()
                .Where(x => x.UserId == userId)
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

            var note = (await notes.ReadNextAsync()).FirstOrDefault();

            if (note is null)
            {
                return new GenericResult<NoteDto>(ResultStatus.NotFound);
            }

            return new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(note));
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
                _logger.LogInformation($"Unauthorized access on note {id} by user {userId} with action {nameof(Delete)}");
                return new GenericResult(ResultStatus.NotFound);
            }

            var response = await container.DeleteItemAsync<Note>(id, new PartitionKey(id));

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                _logger.LogError($"Delete note request failed with status {response.StatusCode}", JsonConvert.SerializeObject(response));
                return new GenericResult(ResultStatus.Failed);
            }

            return new GenericResult(ResultStatus.Success);
        }

        private Container GetContainer()
        {
            return _db.GetContainer(_config.DatabaseID, Constants.NotesContainerId);
        }
    }
}
