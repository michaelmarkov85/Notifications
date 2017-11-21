using System;
using System.Threading;
using System.Threading.Tasks;
using Messages;
using Newtonsoft.Json;

namespace Notifications.Services
{
	public class BackendMessageHandler : IBackendMessageHandler
	{
		IWsBroker _wsBroker;

		public BackendMessageHandler(IWsBroker wsBroker)
		{
			_wsBroker = wsBroker;
		}

		public async Task ProcessMessage(string message, CancellationToken ct = default(CancellationToken))
		{
			BackendMessage msg;
			try
			{
				msg = BackendMessage.Parse(message);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error! [BackendMessageHandler.ProcessMessage] " +
					$"Cannot parse message: {message}. Exception: {ex.Message}.)");
				return;
			}

			if (!msg?.IsValid ?? true)
				return;

			switch (msg.Type.ToLower())
			{
				case "notification":
					await ProcessBackendNotificationMessage(msg, ct);
					break;
				default:
					Console.WriteLine($"Couldn't find match action for message type '{msg.Type}'");
					break;
			}


			// Some logic, db storing...
			//await _wsBroker.BroadcastToAllAsync(message);
		}

		private async Task ProcessBackendNotificationMessage(BackendMessage msg, CancellationToken ct)
		{
			NotificationBackendMessage notificationMessage = msg.Data as NotificationBackendMessage;
			notificationMessage = JsonConvert.DeserializeObject<NotificationBackendMessage>(JsonConvert.SerializeObject(msg.Data));
			if (!notificationMessage?.IsValid ?? true)
				return;

			FrontendMessage messageToSend = new FrontendMessage
			{
				Type = "notification",
				Data = msg.Data
			};

			await _wsBroker.SendAsync(messageToSend.ToString(), notificationMessage.Recipient);
		}
	}
}
