using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications
{
	public interface IWsManager
	{
		bool AddSocket(WebSocket socket, string recipient);
		IEnumerable<string> GetAllRecipients();
		string GetRecipient(WebSocket socket);
		IEnumerable<WebSocket> GetSockets(List<string> recipients);
		IEnumerable<WebSocket> GetSockets(string recipient);
		bool RemoveSocket(WebSocket socket);
		Task KillSocket(WebSocket socket, CancellationToken ct);
	}
}
