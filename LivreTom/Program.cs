using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LivreTom.Data;
using LivreTom.Models;
using LivreTom.Components;
using LivreTom.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// 0. CONFIGURAÇÃO DA PORTA (Render)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://*:{port}");

// 1. CONFIGURAÇÃO DO BANCO DE DADOS COM SSL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Garantir que SSL esteja configurado para o Render
if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 1.1 CONFIGURAÇÃO DO DATA PROTECTION (RESOLVE O ERRO DE OAUTH)
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("LivreTom");

// 2. CONFIGURAÇÃO DO IDENTITY
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
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        options.Events.OnTicketReceived = context =>
        {
            return Task.CompletedTask;
        };
    });

// 4. CONFIGURAÇÃO DE COOKIES
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// 5. SERVIÇOS DO BLAZOR E INTERFACE
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers();

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

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// 8. MAPEAMENTO DE ENDPOINTS
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();