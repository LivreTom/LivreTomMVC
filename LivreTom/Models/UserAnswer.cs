namespace LivreTom.Models;

public class UserAnswer
{
    public int Id { get; set; }
    public int MusicOrderId { get; set; }
    public string QuestionKey { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;

    public virtual MusicOrder? MusicOrder { get; set; }
}
