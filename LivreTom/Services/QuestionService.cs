using LivreTom.Data;
using LivreTom.Models;
using Microsoft.EntityFrameworkCore;

namespace LivreTom.Services;

public class QuestionService(ApplicationDbContext context)
{
    public async Task<List<StepQuestion>> GetQuestionsByCategoryAsync(string category)
    {
        return await context.StepQuestions
            .Where(q => q.Category == category)
            .OrderBy(q => q.StepOrder)
            .ToListAsync();
    }
}
