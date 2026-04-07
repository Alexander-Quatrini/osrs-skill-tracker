using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Services;
using OsrsSkillTracker.Services.Messages;

namespace OsrsSkillTracker.ViewModels;

public partial class BossLogViewModel : BaseViewModel
{
    private readonly AppDbContext _db;
    private readonly IDropRateService _dropRates;
    private readonly IPollingService _polling;

    public BossLogViewModel(AppDbContext db, IDropRateService dropRates, IPollingService polling)
    {
        _db = db;
        _dropRates = dropRates;
        _polling = polling;
        Title = "Boss Log";

        WeakReferenceMessenger.Default.Register<StatsRefreshedMessage>(this, (recipient, message) =>
            MainThread.BeginInvokeOnMainThread(async () => await ((BossLogViewModel)recipient).LoadAsync()));
    }

    [ObservableProperty]
    ObservableCollection<BossRowViewModel> bosses = [];

    [ObservableProperty]
    BossRowViewModel? selectedBoss;

    [RelayCommand]
    async Task LoadAsync()
    {
        var username = Preferences.Get("active_username", string.Empty);
        if (string.IsNullOrEmpty(username)) return;

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);
        if (player is null) return;

        // Latest kill count per boss key
        var latestKills = await _db.BossKills
            .Where(b => b.PlayerId == player.Id)
            .GroupBy(b => b.BossKey)
            .Select(g => g.OrderByDescending(b => b.RecordedAt).First())
            .ToListAsync();

        var killsByKey = latestKills.ToDictionary(b => b.BossKey);
        var dropConfigs = _dropRates.GetAllBosses();
        var dropConfigKeys = dropConfigs.Select(d => d.BossKey).ToHashSet();

        var result = new List<BossRowViewModel>();

        // All bosses from drop config (with or without kills)
        foreach (var config in dropConfigs)
        {
            killsByKey.TryGetValue(config.BossKey, out var kill);
            int kc = kill?.KillCount ?? 0;
            int rank = kill?.Rank ?? -1;

            var expectedDrops = config.Uniques.Select(u => new ExpectedDropViewModel
            {
                ItemName = u.ItemName,
                DropRate = u.DropRate,
                ExpectedCount = Math.Round(kc / (double)u.DropRate, 2)
            }).ToList();

            result.Add(new BossRowViewModel
            {
                BossKey = config.BossKey,
                DisplayName = config.DisplayName,
                KillCount = kc,
                Rank = rank,
                HasDropConfig = true,
                ExpectedDrops = expectedDrops
            });
        }

        // Bosses with kills but no drop config
        foreach (var kill in latestKills.Where(k => !dropConfigKeys.Contains(k.BossKey)))
        {
            result.Add(new BossRowViewModel
            {
                BossKey = kill.BossKey,
                DisplayName = System.Globalization.CultureInfo.CurrentCulture.TextInfo
                    .ToTitleCase(kill.BossKey.Replace('_', ' ')),
                KillCount = kill.KillCount,
                Rank = kill.Rank,
                HasDropConfig = false,
                ExpectedDrops = []
            });
        }

        Bosses = new ObservableCollection<BossRowViewModel>(
            result.OrderByDescending(b => b.KillCount));
    }

    [RelayCommand]
    void SelectBoss(BossRowViewModel boss) => SelectedBoss = boss;

    [RelayCommand]
    void CloseDetail() => SelectedBoss = null;
}
