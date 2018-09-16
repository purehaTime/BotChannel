using BotChannel.DataManager;
using BotChannel.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotChannel
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMvc();

			services.AddScoped<IUpdateService, UpdateService>();
			services.AddSingleton<IBotService, BotService>();

			services.Configure<BotConfiguration>(Configuration.GetSection("BotConfiguration"));

			DbManager.Dbfile = Configuration.GetSection("Db")?.GetValue<string>("File");

		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseMvc();
		}
	}
}