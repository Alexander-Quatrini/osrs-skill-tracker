using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Helpers;
using OsrsSkillTracker.Models;

namespace OsrsSkillTracker.ViewModels;

public partial class GoalViewModel : BaseViewModel, IQueryAttributable
{
    private readonly AppDbContext _db;
    private int? _editingGoalId;

    public GoalViewModel(AppDbContext db)
    {
        _db = db;
        Title = "Goal";
    }

    [ObservableProperty]
    string goalName = string.Empty;

    [ObservableProperty]
    bool goalExists;

    [ObservableProperty]
    ObservableCollection<GoalRequirementRowViewModel> requirements = [];

    [ObservableProperty]
    List<SkillPickerItem> availableSkills = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("goalId", out var goalIdObj) && goalIdObj is int goalId)
            LoadGoalForEditing(goalId);

        if (query.TryGetValue("preselectedSkillId", out var skillIdObj) && skillIdObj is int skillId)
            AddPreselectedRequirement(skillId);
    }

    private async void LoadGoalForEditing(int goalId)
    {
        var goal = await _db.Goals
            .Include(g => g.Requirements)
            .FirstOrDefaultAsync(g => g.Id == goalId);

        if (goal is null) return;

        _editingGoalId = goalId;
        GoalName = goal.Name;
        GoalExists = true;

        await EnsureAvailableSkillsLoaded();

        Requirements = new ObservableCollection<GoalRequirementRowViewModel>(
            goal.Requirements.Select(req =>
            {
                var skill = AvailableSkills.FirstOrDefault(s => s.Id == req.SkillId);
                return new GoalRequirementRowViewModel
                {
                    SelectedSkillId = req.SkillId,
                    SelectedSkillName = skill?.Name ?? string.Empty,
                    TargetLevel = req.TargetLevel
                };
            }));
    }

    private async void AddPreselectedRequirement(int skillId)
    {
        await EnsureAvailableSkillsLoaded();
        var skill = AvailableSkills.FirstOrDefault(s => s.Id == skillId);
        Requirements.Add(new GoalRequirementRowViewModel
        {
            SelectedSkillId = skillId,
            SelectedSkillName = skill?.Name ?? string.Empty,
            TargetLevel = 1
        });
    }

    private async Task EnsureAvailableSkillsLoaded()
    {
        if (AvailableSkills.Count > 0) return;
        var skills = await _db.Skills.OrderBy(s => s.DisplayOrder).ToListAsync();
        AvailableSkills = skills.Select(s => new SkillPickerItem { Id = s.Id, Name = s.Name }).ToList();
    }

    [RelayCommand]
    async Task InitAsync()
    {
        await EnsureAvailableSkillsLoaded();
    }

    [RelayCommand]
    void AddRequirement()
    {
        var first = AvailableSkills.FirstOrDefault();
        Requirements.Add(new GoalRequirementRowViewModel
        {
            SelectedSkillId = first?.Id ?? 0,
            SelectedSkillName = first?.Name ?? string.Empty,
            TargetLevel = 1
        });
    }

    [RelayCommand]
    void RemoveRequirement(GoalRequirementRowViewModel req) => Requirements.Remove(req);

    [RelayCommand]
    async Task SaveGoalAsync()
    {
        if (string.IsNullOrWhiteSpace(GoalName) || Requirements.Count == 0) return;

        var username = Preferences.Get("active_username", string.Empty);
        if (string.IsNullOrEmpty(username)) return;

        var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);
        if (player is null) return;

        if (_editingGoalId.HasValue)
        {
            var goal = await _db.Goals
                .Include(g => g.Requirements)
                .FirstOrDefaultAsync(g => g.Id == _editingGoalId.Value);

            if (goal is null) return;

            goal.Name = GoalName;
            _db.GoalRequirements.RemoveRange(goal.Requirements);

            foreach (var req in Requirements)
            {
                goal.Requirements.Add(new GoalRequirement
                {
                    SkillId = req.SelectedSkillId,
                    TargetLevel = req.TargetLevel,
                    TargetXp = OsrsXpTable.GetXpForLevel(req.TargetLevel)
                });
            }
        }
        else
        {
            var goal = new Goal
            {
                PlayerId = player.Id,
                Name = GoalName,
                CreatedAt = DateTime.UtcNow,
                IsCompleted = false
            };

            foreach (var req in Requirements)
            {
                goal.Requirements.Add(new GoalRequirement
                {
                    SkillId = req.SelectedSkillId,
                    TargetLevel = req.TargetLevel,
                    TargetXp = OsrsXpTable.GetXpForLevel(req.TargetLevel)
                });
            }

            _db.Goals.Add(goal);
        }

        await _db.SaveChangesAsync();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    async Task DeleteGoalAsync()
    {
        if (!_editingGoalId.HasValue) return;

        var goal = await _db.Goals
            .Include(g => g.Requirements)
            .FirstOrDefaultAsync(g => g.Id == _editingGoalId.Value);

        if (goal is null) return;

        _db.Goals.Remove(goal);
        await _db.SaveChangesAsync();
        await Shell.Current.GoToAsync("..");
    }
}
