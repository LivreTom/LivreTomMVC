namespace LivreTom.Models;

public class SupportTicket
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? AssignedToId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Aberto;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CancelReason { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual ApplicationUser? AssignedTo { get; set; }
}

public enum TicketStatus { Aberto, Resolvido, Cancelado }
