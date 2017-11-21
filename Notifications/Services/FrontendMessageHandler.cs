using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using Newtonsoft.Json;

namespace Notifications.Services
{
	public class FrontendMessageHandler : IFrontendMessageHandler
	{
		private readonly IWsManager _wsManager;
		private readonly IWsBroker _wsBroker;


		public FrontendMessageHandler(IWsManager wsManager, IWsBroker wsBroker)
		{
			_wsManager = wsManager;
			_wsBroker = wsBroker;
		}

		/// <summary>
		/// Parse message and define its type. 
		/// Pass message further for processing according to this type.
		/// </summary>
		/// <param name="message">Stringified ClientMesage.</param>
		/// <param name="socket">WebSocket object.</param>
		/// <param name="ct">CancellationToken.</param>

		public async Task ProcessMessage(string message, WebSocket socket, CancellationToken ct = default(CancellationToken))
		{
			FrontendMessage msg;
			try
			{
				msg = FrontendMessage.Parse(message);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error! [FrontendMessageHandler.ProcessMessage] " +
					$"Cannot parse message: {message}. Exception: {ex.Message}.)");
				return;
			}

			if (!msg?.IsValid ?? true)
				return;

			switch (msg.Type.ToLower())
			{
				case "chat_from_merchant":
					await ProcessFrontendChatMessage(socket, msg, ct);
					break;
				default:
					Console.WriteLine($"Couldn't find match action for message type '{msg.Type}'");
					break;
			}
		}

		/// <summary>
		/// Parses message. Sends its body to all receiver's sockets and resends to all
		/// sender's sockets except this current one.
		/// </summary>
		/// <param name="socket">WebSocket object.</param>
		/// <param name="msg">ClientMesage.</param>
		/// <param name="ct">CancellationToken.</param>
		private async Task ProcessFrontendChatMessage(WebSocket socket, FrontendMessage msg, CancellationToken ct = default(CancellationToken))
		{
			ChatFrontendMessage chatMessage = msg.Data as ChatFrontendMessage;
			chatMessage = JsonConvert.DeserializeObject<ChatFrontendMessage>(JsonConvert.SerializeObject(msg.Data));
			if (!chatMessage?.IsValid ?? true)
				return;

			FrontendMessage messageToSend = new FrontendMessage
			{
				Type = "chat",
				Data = msg.Data
			};

			await _wsBroker.BroadcastAsync(messageToSend.ToString(),
				recipients: new List<string>() { chatMessage.From, chatMessage.To },
				exceptSockets: new List<WebSocket>() { socket });
		}
	}
}
