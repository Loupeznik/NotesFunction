using Newtonsoft.Json;
using System;

namespace DZarsky.NotesFunction.Models
{
    public sealed class Note
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        public string? UserId { get; set; }

        public string? Title { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsResolved { get; set; }

        public bool IsEncrypted { get; set; }
    }
}
