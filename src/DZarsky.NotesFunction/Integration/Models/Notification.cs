using Newtonsoft.Json;

namespace DZarsky.NotesFunction.Integration.Models;

public sealed record Notification
{
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonProperty("body")]
    public string Body { get; set; } = string.Empty;
}
