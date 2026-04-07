using CommunityToolkit.Mvvm.ComponentModel;
using OsrsSkillTracker.Helpers;

namespace OsrsSkillTracker.ViewModels;

public partial class GoalRequirementRowViewModel : ObservableObject
{
    [ObservableProperty]
    int selectedSkillId;

    [ObservableProperty]
    string selectedSkillName = string.Empty;

    [ObservableProperty]
    int targetLevel = 1;

    public long ComputedTargetXp { get; private set; } = OsrsXpTable.GetXpForLevel(1);

    partial void OnTargetLevelChanged(int value)
    {
        ComputedTargetXp = OsrsXpTable.GetXpForLevel(value);
        OnPropertyChanged(nameof(ComputedTargetXp));
    }
}
