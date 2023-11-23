using Newtonsoft.Json;

namespace DZarsky.NotesFunction.Integration.Models;

public sealed record SendMessageRequest
{
    [JsonProperty("to")]
    public string To { get; set; } = string.Empty;
    
    [JsonProperty("notification")]
    public Notification? Notification { get; set; }
}
