namespace DZarsky.NotesFunction.Configuration;

public record FirebaseCloudMessagingConfiguration
{
    public bool IsEnabled { get; set; }
    
    public string? ServerKey { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://fcm.googleapis.com/fcm/";
}
