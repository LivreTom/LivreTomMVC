using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LivreTom.Models;

namespace LivreTom.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<MusicOrder> MusicOrders { get; set; }
    public DbSet<StepQuestion> StepQuestions { get; set; }
    public DbSet<UserAnswer> UserAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configuração para garantir que créditos padrão sejam 1 ao criar conta
        builder.Entity<ApplicationUser>()
            .Property(u => u.Credits)
            .HasDefaultValue(1);
    }
}