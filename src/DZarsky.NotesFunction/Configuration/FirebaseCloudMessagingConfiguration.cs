namespace DZarsky.NotesFunction.Configuration;

public record FirebaseCloudMessagingConfiguration
{
    public bool IsEnabled { get; init; }

    public required string Topic { get; init; }

    public string BaseUrl { get; init; } = "https://fcm.googleapis.com/v1/projects/";

    public required string ProjectId { get; init; }
}
