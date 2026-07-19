using Sonata.Server.Memories;

namespace Sonata.Server.Controllers;

public sealed record CreateMemoryRequest(
    long SourceMessageId,
    Guid MovementId,
    string? Text,
    string? Type);
    
public sealed record CreateNoteRequest(
    long SourceMessageId,
    Guid MovementId,
    string? Text,
    string? Type);
    
public sealed record SourceNoteResponse(
    Guid Id,
    long MessageId,
    string Excerpt,
    DateTimeOffset CreatedAt);

public sealed record MemoryResponse(
    Guid Id,
    Guid MovementId,
    string Text,
    string Type,
    string LifecycleState,
    SourceNoteResponse SourceNote,
    DateTimeOffset CreatedAt)
{
    public static MemoryResponse From(MemoryDetails memory)
    {
        return new MemoryResponse(
            memory.Id,
            memory.MovementId,
            memory.Text,
            memory.Type.ToString(),
            memory.LifecycleState.ToString(),
            new SourceNoteResponse(
                memory.SourceNote.Id,
                memory.SourceNote.MessageId,
                memory.SourceNote.Excerpt,
                memory.SourceNote.CreatedAt),
            memory.CreatedAt);
    }
}