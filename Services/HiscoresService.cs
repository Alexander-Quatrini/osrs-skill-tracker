using OsrsSkillTracker.Services.Models;

namespace OsrsSkillTracker.Services;

public class HiscoresService : IHiscoresService
{
    private const string Endpoint =
        "https://secure.runescape.com/m=hiscore_oldschool/index_lite.ws?player=";

    // CSV line order for skills: line 1 = index 0, line 24 = index 23
    private static readonly string[] ApiSkillOrder =
    [
        "Attack", "Defence", "Strength", "Hitpoints", "Ranged", "Prayer",
        "Magic", "Cooking", "Woodcutting", "Fletching", "Fishing", "Firemaking",
        "Crafting", "Smithing", "Mining", "Herblore", "Agility", "Thieving",
        "Slayer", "Farming", "Runecrafting", "Construction", "Hunter", "Sailing"
    ];

    // Maps API skill name → DB Skill.Id (seed order differs from API order;
    // "Runecrafting" in API = "Runecraft" in DB with Id=19)
    private static readonly Dictionary<string, int> SkillNameToDbId = new()
    {
        { "Attack",       1  },
        { "Hitpoints",    2  },
        { "Mining",       3  },
        { "Strength",     4  },
        { "Agility",      5  },
        { "Smithing",     6  },
        { "Defence",      7  },
        { "Herblore",     8  },
        { "Fishing",      9  },
        { "Ranged",       10 },
        { "Thieving",     11 },
        { "Cooking",      12 },
        { "Prayer",       13 },
        { "Crafting",     14 },
        { "Firemaking",   15 },
        { "Magic",        16 },
        { "Fletching",    17 },
        { "Woodcutting",  18 },
        { "Runecrafting", 19 },
        { "Slayer",       20 },
        { "Farming",      21 },
        { "Construction", 22 },
        { "Hunter",       23 },
        { "Sailing",      24 },
    };

    // BossKey → CSV line index, verified against live API response for player IMQuatts
    private static readonly Dictionary<string, int> BossLineIndex = new()
    {
        { "abyssal_sire",                      45  },
        { "alchemical_hydra",                  46  },
        { "amoxliatl",                         47  }, // verified: 74 kc
        { "araxxor",                           48  },
        { "artio",                             49  },
        { "barrows_chests",                    50  }, // verified: 193 kc
        { "brutus",                            51  },
        { "bryophyta",                         52  },
        { "callisto",                          53  },
        { "calvarion",                         54  },
        { "cerberus",                          55  },
        { "chambers_of_xeric",                 56  },
        { "chambers_of_xeric_challenge_mode",  57  },
        { "chaos_elemental",                   58  },
        { "chaos_fanatic",                     59  },
        { "commander_zilyana",                 60  },
        { "corporeal_beast",                   61  },
        { "crazy_archaeologist",               62  },
        { "dagannoth_prime",                   63  },
        { "dagannoth_rex",                     64  }, // verified: 23 kc
        { "dagannoth_supreme",                 65  },
        { "deranged_archaeologist",            66  },
        { "doom_of_mokhaiotl",                 67  },
        { "duke_sucellus",                     68  },
        { "general_graardor",                  69  },
        { "giant_mole",                        70  },
        { "grotesque_guardians",               71  },
        { "hespori",                           72  },
        { "kalphite_queen",                    73  },
        { "king_black_dragon",                 74  },
        { "kraken",                            75  },
        { "kreearra",                          76  },
        { "kril_tsutsaroth",                   77  },
        { "lunar_chests",                      78  }, // verified: 167 kc
        { "mimic",                             79  },
        { "nex",                               80  },
        { "nightmare",                         81  },
        { "phosanis_nightmare",                82  },
        { "obor",                              83  },
        { "phantom_muspah",                    84  },
        { "sarachnis",                         85  },
        { "scorpia",                           86  },
        { "scurrius",                          87  },
        { "shellbane_gryphon",                 88  },
        { "skotizo",                           89  },
        { "sol_heredit",                       90  },
        { "spindel",                           91  },
        { "tempoross",                         92  },
        { "the_gauntlet",                      93  },
        { "the_corrupted_gauntlet",            94  },
        { "the_hueycoatl",                     95  },
        { "the_leviathan",                     96  },
        { "the_royal_titans",                  97  },
        { "the_whisperer",                     98  },
        { "theatre_of_blood",                  99  },
        { "theatre_of_blood_hard_mode",        100 },
        { "thermonuclear_smoke_devil",         101 },
        { "tombs_of_amascut",                  102 },
        { "tombs_of_amascut_expert_mode",      103 },
        { "tzkal_zuk",                         104 },
        { "tztok_jad",                         105 }, // verified: 1 kc
        { "vardorvis",                         106 },
        { "venenatis",                         107 },
        { "vetion",                            108 },
        { "vorkath",                           109 },
        { "wintertodt",                        110 }, // verified: 169 kc
        { "yama",                              111 },
        { "zalcano",                           112 },
        { "zulrah",                            113 },
    };

    private readonly HttpClient _http;

    public HiscoresService(HttpClient http)
    {
        _http = http;
    }

    public async Task<HiscoresResult> FetchPlayerStatsAsync(string username)
    {
        username = Uri.EscapeDataString(username.Trim());
        if (string.IsNullOrEmpty(username))
            return Fail("Username cannot be empty.");

        string body;
        try
        {
            var response = await _http.GetAsync(Endpoint + username);
            if (!response.IsSuccessStatusCode)
                return Fail($"Hiscores returned HTTP {(int)response.StatusCode}.");

            body = await response.Content.ReadAsStringAsync();
        }
        catch (TaskCanceledException)
        {
            return Fail("Request timed out.");
        }
        catch (HttpRequestException ex)
        {
            return Fail($"Network error: {ex.Message}");
        }

        var lines = body
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToArray();

        if (lines.Length < 25)
            return Fail("Unexpected response format from Hiscores API.");

        var skills = new List<SkillStat>(24);
        for (int i = 0; i < 24; i++)
        {
            var parts = lines[i + 1].Split(',');
            if (parts.Length < 3) continue;

            if (!int.TryParse(parts[0], out int rank)  ||
                !int.TryParse(parts[1], out int level)  ||
                !long.TryParse(parts[2], out long xp))
                continue;

            var name = ApiSkillOrder[i];
            skills.Add(new SkillStat
            {
                SkillId   = SkillNameToDbId[name],
                SkillName = name,
                Rank      = rank,
                Level     = level,
                Xp        = xp,
            });
        }

        var bossKills = new List<BossKillStat>();
        foreach (var (bossKey, lineIdx) in BossLineIndex)
        {
            if (lineIdx >= lines.Length) continue;

            var parts = lines[lineIdx].Split(',');
            if (parts.Length < 2) continue;

            if (!int.TryParse(parts[0], out int rank)  ||
                !int.TryParse(parts[1], out int kills))
                continue;

            if (kills == -1) continue; // never killed

            bossKills.Add(new BossKillStat
            {
                BossKey   = bossKey,
                Rank      = rank,
                KillCount = kills,
            });
        }

        return new HiscoresResult
        {
            Success   = true,
            Skills    = skills,
            BossKills = bossKills,
        };
    }

    private static HiscoresResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
