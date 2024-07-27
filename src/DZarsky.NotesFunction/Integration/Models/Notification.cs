using Newtonsoft.Json;

namespace DZarsky.NotesFunction.Integration.Models;

public sealed record Notification
{
    [JsonProperty("title")]
    public required string Title { get; set; }
    
    [JsonProperty("body")]
    public required string Body { get; set; }
}
