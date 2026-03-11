namespace LivreTom.Models;

public class MusicOrder
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? FinalPrompt { get; set; } // Prompt consolidado para o Suno
    public int CreditsSpent { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Pendente";
    public string? ResultUrl { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
}