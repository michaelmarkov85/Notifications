using System.Collections.Generic;
using System.Net.WebSockets;

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
	}
}
