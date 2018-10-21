using System.Threading;
using System.Threading.Tasks;

namespace BotChannel
{
	public class TaskWorker
	{
		public string Id { get; set; }
		public CancellationTokenSource TaskToken {get; set;}
	}
}