using System.Threading.Tasks;

namespace BotChannel
{
	public class TaskWorker
	{
		public string Id { get; set; }
		public Task TaskInWork {get; set;}
	}
}