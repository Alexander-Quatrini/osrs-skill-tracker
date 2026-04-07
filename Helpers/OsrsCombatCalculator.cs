namespace OsrsSkillTracker.Helpers;

public static class OsrsCombatCalculator
{
    public static int GetCombatLevel(int attack, int strength, int defence, int hitpoints, int prayer, int ranged, int magic)
    {
        double @base = 0.25 * (defence + hitpoints + Math.Floor(prayer / 2.0));
        double melee = 0.325 * (attack + strength);
        double rangedCalc = 0.325 * Math.Floor(ranged * 1.5);
        double magicCalc = 0.325 * Math.Floor(magic * 1.5);
        return (int)Math.Floor(@base + Math.Max(melee, Math.Max(rangedCalc, magicCalc)));
    }
}
