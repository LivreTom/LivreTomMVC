using LivreTom.Components;
using LivreTom.Controllers;
using LivreTom.Data;
using LivreTom.Models;
using LivreTom.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Resend;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
    builder.WebHost.UseUrls($"http://*:{port}");
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

        // Sempre exibir o seletor de contas Google ao fazer login
        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uri = context.RedirectUri;
            if (!uri.Contains("prompt="))
                uri += "&prompt=select_account";
            context.Response.Redirect(uri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.LoginPath = "/Account/login";
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

// Suporte para Antiforgery nos formulários do Modal
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();

// Infraestrutura
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AudioController>();
builder.Services.AddScoped<AuthenticationStateService>();

// Serviços de negócio
builder.Services.AddScoped<QuestionService>();
builder.Services.AddScoped<CreditService>();
builder.Services.AddScoped<MusicService>();
builder.Services.AddSingleton<IResend>(_ =>
    ResendClient.Create(builder.Configuration["Resend:ApiKey"]!));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<TicketService>();

// Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

// Validação de configurações críticas no startup
// Seed da role Admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var adminEmails = app.Configuration.GetSection("Admin:Emails")
        .Get<string[]>() ?? [];

    // Adiciona role para emails configurados
    foreach (var email in adminEmails)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is not null && !await userManager.IsInRoleAsync(user, "Admin"))
            await userManager.AddToRoleAsync(user, "Admin");
    }

    // Remove role de quem não está mais na lista
    var currentAdmins = await userManager.GetUsersInRoleAsync("Admin");
    foreach (var admin in currentAdmins)
    {
        if (!adminEmails.Contains(admin.Email!, StringComparer.OrdinalIgnoreCase))
            await userManager.RemoveFromRoleAsync(admin, "Admin");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

app.Run();
