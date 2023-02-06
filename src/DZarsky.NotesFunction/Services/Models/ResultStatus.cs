using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DZarsky.NotesFunction.Services.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResultStatus
    {
        Success,
        Failed,
        AlreadyExists,
        NotFound,
        BadRequest
    }
}
