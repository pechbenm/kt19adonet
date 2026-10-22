using KT19.Models;

namespace KT19.Services;

public interface IUserRepository
{
    Task<AppUser?> FindByNameAsync(string userName);

    /// <summary>Создаёт пользователя. Возвращает false, если имя уже занято.</summary>
    Task<bool> TryCreateAsync(AppUser user);
}
