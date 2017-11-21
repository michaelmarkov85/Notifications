using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Notifications.WebSockets
{
	public class WsBroker : IWsBroker
	{
		private readonly IWsManager _wsManager;

		public WsBroker(IWsManager wsManager)
		{
			_wsManager = wsManager;
		}

		// Interface

		public async Task SendAsync(string data, string recipient, CancellationToken ct = default(CancellationToken))
		{
			if (string.IsNullOrEmpty(recipient) || string.IsNullOrEmpty(data))
			{
				Console.WriteLine($"[SendToRecipientAsync] Invalid arguments: recipient:{recipient}, data:{data}");
				return;
			}

			List<WebSocket> sockets = _wsManager.GetSockets(recipient).ToList();
			if (sockets == null || sockets.Count() < 1)
				return;

			await SendToMultipleSocketsAsync(data, sockets, ct);
		}
		public async Task SendAsync(string data, WebSocket socket, CancellationToken ct = default(CancellationToken))
		{
			if (socket == null || socket.State != WebSocketState.Open)
			{
				Console.WriteLine($"[WebsocketManager.SendAsync] WebSocket is null or not opened.");
				return;
			}
			if (string.IsNullOrWhiteSpace(data))
			{
				Console.WriteLine($"[WebsocketManager.SendAsync] Message data is null or whitespace.");
				return;
			}

			if (ct.IsCancellationRequested)
				return;

			await SendToSocketAsync(data, socket, ct);
		}
		public async Task BroadcastAsync(string data, List<string> recipients, List<WebSocket> exceptSockets = null, CancellationToken ct = default(CancellationToken))
		{
			if (recipients == null || recipients.Count < 1)
			{
				Console.WriteLine($"[WebsocketManager.BroadcastAsync] Recipients are null or none.");
				return;
			}

			List<WebSocket> sockets = _wsManager.GetSockets(recipients).ToList();

			// Excluding forbidden sockets
			if (exceptSockets != null && exceptSockets.Count > 0)
				sockets = sockets.Except(exceptSockets).ToList();

			await SendToMultipleSocketsAsync(data, sockets, ct);
		}
		public async Task BroadcastToAllAsync(string data, List<string> exceptRecipients = null, List<WebSocket> exceptSockets = null, CancellationToken ct = default(CancellationToken))
		{
			// Getting recipients
			List<string> recipients = _wsManager.GetAllRecipients().ToList();
			if (recipients == null || recipients.Count < 1)
			{
				Console.WriteLine($"[WebsocketManager.BroadcastToAllAsync] Recipients are null or none.");
				return;
			}
			// Excluding forbidden recipients
			if (exceptRecipients != null && exceptRecipients.Count > 0)
			{
				recipients = recipients.Except(exceptRecipients).ToList();
				// Check if anyone left
				if (recipients == null || recipients.Count < 1)
					return;
			}

			// Getting sockets
			List<WebSocket> sockets = _wsManager.GetSockets(recipients).ToList();
			if (recipients == null || recipients.Count < 1)
			{
				Console.WriteLine($"[WebsocketManager.BroadcastToAllAsync] There are no sockets for requested recipients.");
				return;
			}
			// Excluding forbidden sockets
			if (exceptSockets != null && exceptSockets.Count > 0)
			{
				sockets = sockets.Except(exceptSockets).ToList();
				// Check if any left
				if (sockets == null || sockets.Count < 1)
					return;
			}

			await SendToMultipleSocketsAsync(data, sockets, ct);
		}
		public async Task Listen(WebSocket socket, Func<string, WebSocket, Task> messageHandler, CancellationToken ct = default(CancellationToken))
		{
			while (true)
			{
				if (ct.IsCancellationRequested)
					break;

				string response = await ReceiveStringAsync(socket, ct);
				if (string.IsNullOrEmpty(response))
				{
					if (socket.State != WebSocketState.Open)
					{
						break;
					}
					continue;
				}

				await messageHandler(response, socket);
			}

			await KillSocket(socket, ct);
		}

		// Private

		private async Task SendToMultipleSocketsAsync(string data, List<WebSocket> sockets, CancellationToken ct)
		{
			List<Task> tasks = new List<Task>();
			foreach (var s in sockets)
			{
				if (ct.IsCancellationRequested)
					break;
				tasks.Add(Task.Run(async () => await SendToSocketAsync(data, s, ct)));
			}
			await Task.WhenAll(tasks);
		}
		private async Task SendToSocketAsync(string data, WebSocket socket, CancellationToken ct = default(CancellationToken))
		{
			byte[] buffer = System.Text.Encoding.UTF8.GetBytes(data);
			ArraySegment<byte> segment = new ArraySegment<byte>(buffer);
			await socket.SendAsync(segment, WebSocketMessageType.Text, true, ct);
		}


		/// <summary>
		/// Receives WebSocket frames to ArraySegment buffer, writes into memory stream in a loop
		/// until EndOfMessage. Then reads stream into a string.
		/// </summary>
		/// <param name="socket">WebSocket object.</param>
		/// <param name="ct">CancellationToken.</param>
		/// <returns>Combined message.</returns>
		private static async Task<string> ReceiveStringAsync(WebSocket socket, CancellationToken ct = default(CancellationToken))
		{
			ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024 * 4]);
			using (MemoryStream ms = new MemoryStream())
			{
				WebSocketReceiveResult result;
				do
				{
					ct.ThrowIfCancellationRequested();

					result = await socket.ReceiveAsync(buffer, ct);
					ms.Write(buffer.Array, buffer.Offset, result.Count);
				}
				while (!result.EndOfMessage);

				ms.Seek(0, SeekOrigin.Begin);
				if (result.MessageType != WebSocketMessageType.Text)
				{
					return null;
				}

				// Encoding UTF8: https://tools.ietf.org/html/rfc6455#section-5.6
				using (StreamReader reader = new StreamReader(ms, System.Text.Encoding.UTF8))
				{
					return await reader.ReadToEndAsync();
				}
			}
		}

		/// <summary>
		/// Removes socket from WebsocketManager collection, closes connection
		/// and disposes socket.
		/// </summary>
		/// <param name="socket">WebSocket object.</param>
		/// <param name="ct">CancellationToken.</param>
		private async Task KillSocket(WebSocket socket, CancellationToken ct)
		{
			await Task.Run(() => _wsManager.RemoveSocket(socket));
			await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", ct);
			socket.Dispose();
		}
	}
}
