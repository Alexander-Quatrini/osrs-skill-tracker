using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OsrsSkillTracker.Data;
using OsrsSkillTracker.Services;
using OsrsSkillTracker.ViewModels;

namespace OsrsSkillTracker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "osrs_tracker.db");
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		builder.Services.AddSingleton<HttpClient>(_ => new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15)
		});

		builder.Services.AddScoped<IHiscoresService, HiscoresService>();
		builder.Services.AddSingleton<IPollingService, PollingService>();
		builder.Services.AddSingleton<IDropRateService, DropRateService>();

		builder.Services.AddTransient<PlayerSearchViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<SkillListViewModel>();
		builder.Services.AddTransient<SkillDetailViewModel>();
		builder.Services.AddTransient<BossLogViewModel>();
		builder.Services.AddTransient<GoalViewModel>();

		var app = builder.Build();

		using (var scope = app.Services.CreateScope())
		{
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
			db.Database.EnsureCreated();
		}

		return app;
	}
}
