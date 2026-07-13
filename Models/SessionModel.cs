using System.ComponentModel.DataAnnotations.Schema;
namespace qwen_hackathon_api.Models;

[Table("sessions")]
public class Session
{
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.Now;
    [Column("ended_at")]
    public DateTime? EndedAt { get; set; }
}