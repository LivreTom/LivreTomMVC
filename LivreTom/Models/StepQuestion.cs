namespace LivreTom.Models;

public class StepQuestion
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty; // ex: Homenagem, Relax, Criadores
    public int StepOrder { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string SystemKey { get; set; } = string.Empty; // ex: "nome_alvo", "ritmo_preferido"
    public string InputType { get; set; } = "text"; // text, textarea, select
    public string? OptionsJson { get; set; } // Caso seja um select, salvamos as opções aqui
}