using DZarsky.NotesFunction.Configuration;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Integration.Models;
using DZarsky.NotesFunction.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DZarsky.NotesFunction;

public sealed class NoteDueDateFunction
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly FirebaseCloudMessagingConfiguration _configuration;
    private readonly NoteService _noteService;
    private readonly ILogger<NoteDueDateFunction> _logger;

    public NoteDueDateFunction(IHttpClientFactory httpClientFactory, FirebaseCloudMessagingConfiguration configuration,
        NoteService noteService, ILogger<NoteDueDateFunction> logger)
    {
        _clientFactory = httpClientFactory;
        _configuration = configuration;
        _noteService = noteService;
        _logger = logger;
    }

    [Function("NoteDueDateFunction")]
    public async Task RunAsync([TimerTrigger("* * * * *")] TimerInfo timer)
    {
        if (!_configuration.IsEnabled)
        {
            return;
        }

        var client = _clientFactory.CreateClient(HttpClients.FirebaseCloudMessagingClient);

        try
        {
            var noteGroups = await _noteService.GetNotesForNotificationProcessing();

            foreach (var group in noteGroups)
            {
                foreach (var note in group.Notes)
                {
                    var result = await client.PostAsJsonAsync("send", new SendMessageRequest
                    {
                        To = $"/topics/{_configuration.Topic}_{group.UserId}",
                        Notification = new Notification
                        {
                            Title = "A note is due",
                            Body = $"Note {note.Title} is due on {note.DueDate}"
                        }
                    });

                    if (!result.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Failed to send notification for note {NoteId}. The response from the FCM API was {Response}",
                            note.Id, await result.Content.ReadAsStringAsync());
                        continue;
                    }

                    await _noteService.SetNotificationSent(note.Id!);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occured. Error message: {Message}", ex.Message);
        }
    }
}
