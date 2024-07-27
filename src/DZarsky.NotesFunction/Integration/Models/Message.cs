using Newtonsoft.Json;

namespace DZarsky.NotesFunction.Integration.Models;

public sealed record Message
{
    [JsonProperty("topic")]
    public required string Topic { get; set; }

    [JsonProperty("notification")]
    public required Notification Notification { get; set; }

    [JsonProperty("data")]
    public object? Data { get; set; }
}
