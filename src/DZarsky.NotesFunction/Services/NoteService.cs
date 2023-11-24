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

namespace DZarsky.NotesFunction.Services;

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
            IsEncrypted = note.IsEncrypted,
            Category = note.Category,
            DueDate = note.DueDate
        });

        if (response.StatusCode == HttpStatusCode.Created)
        {
            return new GenericResult<NoteDto>(ResultStatus.Success, _mapper.Map<NoteDto>(response.Resource));
        }

        _logger.LogError("Create note request failed with status {ResponseStatusCode} - Result {Result}",
            response.StatusCode, JsonConvert.SerializeObject(response));
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
            _logger.LogInformation("Unauthorized access to note {Id} by user {UserId} with action {UpdateName}",
                note.Id, userId, nameof(Update));
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

        _logger.LogError("Create note request failed with status {ResponseStatusCode} - Result {ResponseResult}",
            response.StatusCode, JsonConvert.SerializeObject(response));
        return new GenericResult<NoteDto>(ResultStatus.Failed);
    }

    public async Task<GenericResult<IList<NoteDto>>> List(string userId, string? category = null,
        bool getDeleted = false)
    {
        var container = GetContainer();

        var notes = new List<Note>();

        var response = container
                       .GetItemLinqQueryable<Note>()
                       .Where(x => x.UserId == userId && (getDeleted || !x.IsDeleted) &&
                                   (string.IsNullOrWhiteSpace(category) || x.Category == category))
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
                "Unauthorized access on note {Id} by user {UserId} with action {Action}", id, userId, nameof(Delete));
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

        _logger.LogError("Delete note request failed with status {ResponseStatusCode} - Result {ResponseResult}",
            response.StatusCode, JsonConvert.SerializeObject(response));
        return new GenericResult(ResultStatus.Failed);
    }

    public async Task<IList<UserNotes>> GetNotesForNotificationProcessing()
    {
        var container = GetContainer();
        var result = new List<UserNotes>();

        var response = container
                       .GetItemLinqQueryable<Note>()
                       .Where(x => !x.IsDeleted && x.DueDate <= DateTime.Now && !x.DueNotificationSent)
                       .GroupBy(x => x.UserId)
                       .ToFeedIterator();

        foreach (var userNotes in await response.ReadNextAsync())
        {
            result.Add(new UserNotes(userNotes.Key, _mapper.Map<List<NoteDto>>(userNotes)));
        }

        return result;
    }

    public async Task SetNotificationSent(string noteId)
    {
        var container = GetContainer();

        var response = await container.PatchItemAsync<Note>(noteId, new PartitionKey(noteId),
            new List<PatchOperation>
            {
                PatchOperation.Replace("/DueNotificationSent", true)
            });

        if (response.StatusCode != HttpStatusCode.OK)
        {
            _logger.LogError(
                "Set notification sent request failed with status {ResponseStatusCode} - Result {ResponseResult}",
                response.StatusCode, JsonConvert.SerializeObject(response));
        }
    }

    private Container GetContainer() => _db.GetContainer(_config.DatabaseID, Constants.NotesContainerId);

    public sealed record UserNotes(string UserId, IList<NoteDto> Notes);
}
