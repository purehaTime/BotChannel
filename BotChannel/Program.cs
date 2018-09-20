using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace BotChannel
{
	class Program
	{
		public static IConfiguration Configuration { get; set; }

		static async Task<int> Main(string[] args)
		{
			await BuildWebHost(args).RunAsync();
			return 0;
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>()
				.ConfigureAppConfiguration(conf => conf.AddJsonFile("appsettings.develop.json"))
				.Build();
	}
}
