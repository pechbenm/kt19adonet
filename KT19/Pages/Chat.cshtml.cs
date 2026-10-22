using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KT19.Pages;

/// <summary>Страница чата — только для авторизованных. Аноним будет перенаправлен на /Account/Login.</summary>
[Authorize]
public class ChatModel : PageModel
{
    public void OnGet() { }
}
