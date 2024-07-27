using System;
using System.Net.Http;
using System.Threading.Tasks;
using DZarsky.NotesFunction.Configuration;
using DZarsky.NotesFunction.General;
using DZarsky.NotesFunction.Integration.Models;
using DZarsky.NotesFunction.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace DZarsky.NotesFunction;

public sealed class NoteDueDateFunction
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly FirebaseCloudMessagingConfiguration _configuration;
    private readonly NoteService _noteService;

    public NoteDueDateFunction(IHttpClientFactory httpClientFactory, FirebaseCloudMessagingConfiguration configuration,
        NoteService noteService)
    {
        _clientFactory = httpClientFactory;
        _configuration = configuration;
        _noteService = noteService;
    }

    [FunctionName("NoteDueDateFunction")]
    public async Task RunAsync([TimerTrigger("* * * * *")] TimerInfo timer, ILogger log)
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
                    var result = await client.PostAsJsonAsync("messages:send", new SendMessageRequest
                    {
                        Message = new Message
                        {
                            Topic = $"{_configuration.Topic}_{group.UserId}",
                            Notification = new Notification
                            {
                                Title = "A note is due",
                                Body = $"Note {note.Title} is due on {note.DueDate}"
                            }
                        }
                    });

                    if (!result.IsSuccessStatusCode)
                    {
                        log.LogWarning(
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
            log.LogError(ex, "An error has occured. Error message: {Message}", ex.Message);
        }
    }
}
