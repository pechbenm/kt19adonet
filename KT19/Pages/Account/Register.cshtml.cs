using System.ComponentModel.DataAnnotations;
using KT19.Models;
using KT19.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KT19.Pages.Account;

[AllowAnonymous]
public class RegisterModel(IUserRepository users, IPasswordHasher<AppUser> hasher) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Введите имя пользователя.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Имя должно содержать от 3 до 20 символов.")]
        [RegularExpression(@"^[\p{L}\p{N}_\-]+$", ErrorMessage = "Допустимы буквы, цифры, «_» и «-».")]
        [Display(Name = "Имя пользователя")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Пароль должен содержать не менее 8 символов.")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повторите пароль.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Пароли не совпадают.")]
        [Display(Name = "Подтверждение пароля")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet() =>
        User.Identity?.IsAuthenticated == true ? RedirectToPage("/Chat") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = Input.UserName.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, Input.Password);

        if (!await users.TryCreateAsync(user))
        {
            ModelState.AddModelError("Input.UserName", "Это имя уже занято.");
            return Page();
        }

        // После регистрации сразу входим.
        await HttpContext.SignInUserAsync(user, isPersistent: false);
        return RedirectToPage("/Chat");
    }
}
