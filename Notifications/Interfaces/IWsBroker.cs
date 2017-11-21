using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications
{
	public interface IWsBroker
	{
		Task BroadcastAsync(string data, List<string> recipients, List<WebSocket> exceptSockets = null, CancellationToken ct = default(CancellationToken));
		Task BroadcastToAllAsync(string data, List<string> exceptRecipients = null, List<WebSocket> exceptSockets = null, CancellationToken ct = default(CancellationToken));
		Task SendAsync(string data, string recipient, CancellationToken ct = default(CancellationToken));
		Task SendAsync(string data, WebSocket socket, CancellationToken ct = default(CancellationToken));
		Task Listen(WebSocket socket, Func<string, WebSocket, Task> messageHandler, CancellationToken ct = default(CancellationToken));
	}
}
