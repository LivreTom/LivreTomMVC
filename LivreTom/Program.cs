using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LivreTom.Data;
using LivreTom.Models;
using LivreTom.Components;
using LivreTom.Services;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DO BANCO DE DADOS
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. CONFIGURAÇÃO DO IDENTITY
// Usamos AddIdentity em vez de AddIdentityCore para garantir que todos os serviços de UI e Cookies sejam registrados
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. AUTENTICAÇÃO E GOOGLE
builder.Services.AddAuthentication(options =>
{
    // Define que o esquema padrão de autenticação é o de Cookies do Identity
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        // Mapeia o e-mail do Google para o UserName do Identity
        options.Events.OnTicketReceived = context =>
        {
            return Task.CompletedTask;
        };
    });

// 4. CONFIGURAÇÃO DE COOKIES (Evita o problema de voltar deslogado)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/login"; // Caminho do seu Controller
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax; // Necessário para redirects externos
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 5. SERVIÇOS DO BLAZOR E INTERFACE
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers(); // Habilita o AccountController.cs

// 6. NOSSOS SERVIÇOS DE NEGÓCIO (LIVRETOM)
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<CreditService>();
builder.Services.AddScoped<MusicService>();

var app = builder.Build();

// 7. PIPELINE DE REQUISIÇÕES
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); // Importante: Routing antes de Auth

app.UseAuthentication(); // Autenticação vem primeiro
app.UseAuthorization();  // Autorização vem depois

app.UseAntiforgery();

// 8. MAPEAMENTO DE ENDPOINTS
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers(); // Mapeia o AccountController

app.Run();