using System;

namespace DZarsky.NotesFunction.Models;

public sealed class NoteDto
{
    public string? Id { get; set; }

    public string? Title { get; set; }

    /// <summary>
    /// The plaintext representation of the note.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The quill delta representation of the note as JSON.
    /// </summary>
    public string? EncodedText { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool IsResolved { get; set; }

    public bool IsEncrypted { get; set; }

    public string? Category { get; set; }
}
