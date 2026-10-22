using System.ComponentModel.DataAnnotations;
using KT19.Models;
using KT19.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KT19.Pages.Account;

[AllowAnonymous]
public class LoginModel(IUserRepository users, IPasswordHasher<AppUser> hasher) : PageModel
{
    // Фиктивный хеш: проверяем его, если пользователь не найден, —
    // время ответа не выдаёт, существует ли такое имя.
    private static readonly AppUser DummyUser = new();
    private static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(DummyUser, "dummy-password");

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите имя пользователя.")]
        [Display(Name = "Имя пользователя")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль.")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? RedirectToPage("/Chat") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = await users.FindByNameAsync(Input.UserName.Trim());
        var result = hasher.VerifyHashedPassword(
            user ?? DummyUser, user?.PasswordHash ?? DummyHash, Input.Password);

        if (user is null || result == PasswordVerificationResult.Failed)
        {
            // Одно и то же сообщение — не подсказываем, что именно неверно.
            ModelState.AddModelError(string.Empty, "Неверное имя пользователя или пароль.");
            return Page();
        }

        await HttpContext.SignInUserAsync(user, Input.RememberMe);

        // Редирект только на локальные адреса — защита от open redirect.
        return !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? LocalRedirect(ReturnUrl)
            : RedirectToPage("/Chat");
    }
}
