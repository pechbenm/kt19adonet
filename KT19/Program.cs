using System.Text.Encodings.Web;
using System.Text.Unicode;
using KT19.Hubs;
using KT19.Models;
using KT19.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Не превращать кириллицу в числовые HTML-сущности (&#x41F;) — разметка короче и читаемее.
// Опасные символы (<, >, &, кавычки) по-прежнему экранируются.
builder.Services.AddWebEncoders(o =>
    o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic));
builder.Services.AddSignalR();

// Хранилище пользователей и хеширование паролей (PBKDF2 из Microsoft.Extensions.Identity.Core).
builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();

// Состояние чата живёт в памяти процесса.
builder.Services.AddSingleton<ChatHistory>();
builder.Services.AddSingleton<OnlineUsers>();

// ---- 1. Аутентификация на основе куки ----
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KT19.Auth";
        options.Cookie.HttpOnly = true;                    // недоступна из JavaScript
        options.Cookie.SameSite = SameSiteMode.Lax;        // не отправляется с чужих сайтов (защита от CSRF/CSWSH)
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest             // в разработке допускаем http
            : CookieSecurePolicy.Always;                   // в продакшене — только HTTPS
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Для запросов к хабу SignalR вместо редиректа на страницу входа возвращаем 401/403.
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/hubs"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/hubs"))
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();   // сначала «кто ты»…
app.UseAuthorization();    // …затем «что тебе можно»

app.MapRazorPages();
app.MapHub<ChatHub>("/hubs/chat");   // сам хаб помечен [Authorize]

app.Run();
