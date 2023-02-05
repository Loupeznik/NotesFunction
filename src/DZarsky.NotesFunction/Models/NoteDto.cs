using System;

namespace DZarsky.NotesFunction.Models
{
    public sealed class NoteDto
    {
        public string? Id { get; set; }

        public string? Title { get; set; }

        public string? Text { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsResolved { get; set; }
    }
}
