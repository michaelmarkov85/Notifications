using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications
{
	public interface IFrontendMessageHandler
	{
		Task ProcessMessage(string message, WebSocket socket, CancellationToken ct = default(CancellationToken));
	}
}
