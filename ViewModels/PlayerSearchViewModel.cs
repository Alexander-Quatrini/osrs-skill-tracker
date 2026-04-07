using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Models;
using OsrsSkillTracker.Services;

namespace OsrsSkillTracker.ViewModels;

public partial class PlayerSearchViewModel : BaseViewModel
{
    private readonly IHiscoresService _hiscores;
    private readonly AppDbContext _db;

    public PlayerSearchViewModel(IHiscoresService hiscores, AppDbContext db)
    {
        _hiscores = hiscores;
        _db = db;
        Title = "Find Player";
    }

    [ObservableProperty]
    string username = string.Empty;

    [ObservableProperty]
    string errorMessage = string.Empty;

    [ObservableProperty]
    bool hasError;

    [RelayCommand]
    async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Please enter a username.";
            HasError = true;
            return;
        }

        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _hiscores.FetchPlayerStatsAsync(Username);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Failed to fetch player stats.";
                HasError = true;
                return;
            }

            var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == Username);
            if (player is null)
            {
                player = new Player { Username = Username, DisplayName = Username, LastUpdated = DateTime.UtcNow };
                _db.Players.Add(player);
                await _db.SaveChangesAsync();
            }
            else
            {
                player.LastUpdated = DateTime.UtcNow;
            }

            var now = DateTime.UtcNow;

            foreach (var skill in result.Skills)
            {
                _db.XpSnapshots.Add(new XpSnapshot
                {
                    PlayerId = player.Id,
                    SkillId = skill.SkillId,
                    Level = skill.Level,
                    Xp = skill.Xp,
                    Rank = skill.Rank,
                    RecordedAt = now
                });
            }

            foreach (var boss in result.BossKills)
            {
                _db.BossKills.Add(new BossKill
                {
                    PlayerId = player.Id,
                    BossKey = boss.BossKey,
                    KillCount = boss.KillCount,
                    Rank = boss.Rank,
                    RecordedAt = now
                });
            }

            await _db.SaveChangesAsync();

            Preferences.Set("active_username", Username);
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
