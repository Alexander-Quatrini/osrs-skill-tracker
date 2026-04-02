using Microsoft.EntityFrameworkCore;
using OsrsSkillTracker.Models;

namespace OsrsSkillTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalRequirement> GoalRequirements => Set<GoalRequirement>();
    public DbSet<XpSnapshot> XpSnapshots => Set<XpSnapshot>();
    public DbSet<BossKill> BossKills => Set<BossKill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>()
            .HasMany(p => p.Goals)
            .WithOne(g => g.Player)
            .HasForeignKey(g => g.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Player>()
            .HasMany(p => p.XpSnapshots)
            .WithOne(s => s.Player)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Player>()
            .HasMany(p => p.BossKills)
            .WithOne(b => b.Player)
            .HasForeignKey(b => b.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Goal>()
            .HasMany(g => g.Requirements)
            .WithOne(r => r.Goal)
            .HasForeignKey(r => r.GoalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoalRequirement>()
            .HasOne(r => r.Skill)
            .WithMany()
            .HasForeignKey(r => r.SkillId);

        modelBuilder.Entity<Skill>().HasData(
            new Skill { Id = 1,  Name = "Attack",       IconKey = "attack",       DisplayOrder = 1  },
            new Skill { Id = 2,  Name = "Hitpoints",    IconKey = "hitpoints",    DisplayOrder = 2  },
            new Skill { Id = 3,  Name = "Mining",       IconKey = "mining",       DisplayOrder = 3  },
            new Skill { Id = 4,  Name = "Strength",     IconKey = "strength",     DisplayOrder = 4  },
            new Skill { Id = 5,  Name = "Agility",      IconKey = "agility",      DisplayOrder = 5  },
            new Skill { Id = 6,  Name = "Smithing",     IconKey = "smithing",     DisplayOrder = 6  },
            new Skill { Id = 7,  Name = "Defence",      IconKey = "defence",      DisplayOrder = 7  },
            new Skill { Id = 8,  Name = "Herblore",     IconKey = "herblore",     DisplayOrder = 8  },
            new Skill { Id = 9,  Name = "Fishing",      IconKey = "fishing",      DisplayOrder = 9  },
            new Skill { Id = 10, Name = "Ranged",       IconKey = "ranged",       DisplayOrder = 10 },
            new Skill { Id = 11, Name = "Thieving",     IconKey = "thieving",     DisplayOrder = 11 },
            new Skill { Id = 12, Name = "Cooking",      IconKey = "cooking",      DisplayOrder = 12 },
            new Skill { Id = 13, Name = "Prayer",       IconKey = "prayer",       DisplayOrder = 13 },
            new Skill { Id = 14, Name = "Crafting",     IconKey = "crafting",     DisplayOrder = 14 },
            new Skill { Id = 15, Name = "Firemaking",   IconKey = "firemaking",   DisplayOrder = 15 },
            new Skill { Id = 16, Name = "Magic",        IconKey = "magic",        DisplayOrder = 16 },
            new Skill { Id = 17, Name = "Fletching",    IconKey = "fletching",    DisplayOrder = 17 },
            new Skill { Id = 18, Name = "Woodcutting",  IconKey = "woodcutting",  DisplayOrder = 18 },
            new Skill { Id = 19, Name = "Runecraft",    IconKey = "runecraft",    DisplayOrder = 19 },
            new Skill { Id = 20, Name = "Slayer",       IconKey = "slayer",       DisplayOrder = 20 },
            new Skill { Id = 21, Name = "Farming",      IconKey = "farming",      DisplayOrder = 21 },
            new Skill { Id = 22, Name = "Construction", IconKey = "construction", DisplayOrder = 22 },
            new Skill { Id = 23, Name = "Hunter",       IconKey = "hunter",       DisplayOrder = 23 }
        );
    }
}
