using System.Threading;
using System.Threading.Tasks;

namespace Notifications
{
	public interface IBackendMessageHandler
	{
		Task ProcessMessage(string message, CancellationToken ct = default(CancellationToken));
	}
}
