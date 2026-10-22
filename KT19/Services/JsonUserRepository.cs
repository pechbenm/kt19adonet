using System.Text.Json;
using KT19.Models;

namespace KT19.Services;

/// <summary>
/// Простое файловое хранилище пользователей (App_Data/users.json).
/// Интерфейс <see cref="IUserRepository"/> позволяет заменить его на EF Core / ASP.NET Core Identity.
/// В файле хранятся только хеши паролей.
/// </summary>
public sealed class JsonUserRepository : IUserRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<AppUser>? _cache;

    public JsonUserRepository(IWebHostEnvironment env, IConfiguration configuration)
    {
        _path = configuration["Storage:UsersFile"]
                ?? Path.Combine(env.ContentRootPath, "App_Data", "users.json");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
    }

    public async Task<AppUser?> FindByNameAsync(string userName)
    {
        await _lock.WaitAsync();
        try
        {
            var users = await LoadAsync();
            return users.FirstOrDefault(u => SameName(u.UserName, userName));
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> TryCreateAsync(AppUser user)
    {
        await _lock.WaitAsync();
        try
        {
            var users = await LoadAsync();
            if (users.Any(u => SameName(u.UserName, user.UserName)))
                return false;

            users.Add(user);
            await SaveAsync(users);
            return true;
        }
        finally { _lock.Release(); }
    }

    private static bool SameName(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private async Task<List<AppUser>> LoadAsync()
    {
        if (_cache is not null) return _cache;
        if (!File.Exists(_path)) return _cache = new List<AppUser>();

        await using var stream = File.OpenRead(_path);
        return _cache = await JsonSerializer.DeserializeAsync<List<AppUser>>(stream) ?? new List<AppUser>();
    }

    private async Task SaveAsync(List<AppUser> users)
    {
        // Пишем во временный файл и подменяем — чтобы не потерять данные при сбое.
        var temp = _path + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, users, JsonOptions);
        File.Move(temp, _path, overwrite: true);
    }
}
