using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services;

public interface IDropRateService
{
    List<BossDropConfig> GetAllBosses();
    BossDropConfig? GetBoss(string bossKey);
}
