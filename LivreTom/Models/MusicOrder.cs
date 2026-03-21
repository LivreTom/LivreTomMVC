namespace LivreTom.Models;

public class MusicOrder
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? FinalPrompt { get; set; }
    public int CreditsSpent { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pendente";
    public string? SunoSongId { get; set; }
    public string? AudioUrl { get; set; }
    public string? Lyrics { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Title { get; set; }

    public string? ResolvedAudioUrl => !string.IsNullOrEmpty(AudioUrl)
        ? AudioUrl
        : !string.IsNullOrEmpty(SunoSongId)
            ? $"https://cdn1.suno.ai/{SunoSongId}.mp3"
            : null;

    public bool IsExpired => CreatedAt < DateTime.UtcNow.AddDays(-30);

    // NOVOS CAMPOS: confirmação de download (termo)
    public bool DownloadConfirmed { get; set; } = false;
    public DateTime? DownloadConfirmedAt { get; set; }

    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<UserAnswer> Answers { get; set; } = [];
}
