using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class TicketService(ApplicationDbContext context)
{
    public async Task<List<SupportTicket>> GetTicketsByUserAsync(string userId)
        => await context.SupportTickets
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<List<SupportTicket>> GetAllTicketsAsync()
        => await context.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task<SupportTicket> CreateTicketAsync(string userId, string email, string subject, string description)
    {
        var ticket = new SupportTicket
        {
            UserId = userId,
            Email = email,
            Subject = subject,
            Description = description
        };
        context.SupportTickets.Add(ticket);
        await context.SaveChangesAsync();
        return ticket;
    }

    public async Task<bool> UpdateStatusAsync(int ticketId, TicketStatus status, string requestingUserId, bool isAdmin, string? cancelReason = null)
    {
        var ticket = await context.SupportTickets.FindAsync(ticketId);
        if (ticket is null) return false;

        if (!isAdmin && ticket.Status != TicketStatus.Aberto) return false;

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (status == TicketStatus.Cancelado && !string.IsNullOrEmpty(cancelReason))
            ticket.CancelReason = cancelReason;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTicketAsync(int ticketId, string adminId)
    {
        var ticket = await context.SupportTickets.FindAsync(ticketId);
        if (ticket is null) return false;

        ticket.AssignedToId = adminId;
        ticket.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return true;
    }
}
