using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class QuestionService
{
    private readonly ApplicationDbContext _context;

    public QuestionService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Busca todas as perguntas de uma categoria específica (Homenagem, Relax, etc)
    public async Task<List<StepQuestion>> GetQuestionsByCategoryAsync(string category)
    {
        return await _context.StepQuestions
            .Where(q => q.Category == category)
            .OrderBy(q => q.StepOrder)
            .ToListAsync();
    }
}