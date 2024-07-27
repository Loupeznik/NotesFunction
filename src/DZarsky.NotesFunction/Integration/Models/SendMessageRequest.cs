using Newtonsoft.Json;

namespace DZarsky.NotesFunction.Integration.Models;

public sealed record SendMessageRequest
{
    [JsonProperty("message")]
    public Message? Message { get; set; }
}
