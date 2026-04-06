using System.Text.Json;
using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services;

public class DropRateService : IDropRateService
{
    // Load eagerly on construction so the first call to GetAllBosses() is instant.
    // GetAwaiter().GetResult() is safe here: this singleton is resolved on Windows
    // desktop where there is no single-threaded SynchronizationContext to deadlock.
    private readonly List<BossDropConfig> _cache;

    public DropRateService()
    {
        _cache = LoadAsync().GetAwaiter().GetResult();
    }

    private static async Task<List<BossDropConfig>> LoadAsync()
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("drop_rates.json");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var wrapper = await JsonSerializer.DeserializeAsync<DropRatesWrapper>(stream, options);
            return wrapper?.Bosses ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    public List<BossDropConfig> GetAllBosses() => _cache;

    public BossDropConfig? GetBoss(string bossKey) =>
        _cache.FirstOrDefault(b => b.BossKey == bossKey);
}

file sealed class DropRatesWrapper
{
    public List<BossDropConfig> Bosses { get; set; } = new();
}
