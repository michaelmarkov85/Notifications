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

			switch (msg.EventType.ToLower())
			{
				case "offer_status_approved":
				case "offer_status_rejected":
				case "offer_status_expired":
				case "offer_status_sold_out":
				case "offer_status_inactive":
					await ProcessOfferNotificationMessage(msg, ct);
					break;
				case "offer_purchased":
					await ProcessOfferPurchaseNotificationMessage(msg, ct);
					break;
				case "offer_commented":
					await ProcessOfferCommentNotificationMessage(msg, ct);
					break;
				default:
					Console.WriteLine($"Couldn't find match action for message type '{msg.EventType}'");
					break;
			}
		}

		private async Task ProcessOfferCommentNotificationMessage(BackendMessage msg, CancellationToken ct)
		{
			// just for separation of types
			await ProcessOfferNotificationMessage(msg, ct);
		}

		private async Task ProcessOfferPurchaseNotificationMessage(BackendMessage msg, CancellationToken ct)
		{
			// just for separation of types
			await ProcessOfferNotificationMessage(msg, ct);
		}

		private async Task ProcessOfferNotificationMessage(BackendMessage msg, CancellationToken ct)
		{
			OfferNotification baseMsg = msg.Data as OfferNotification;
			baseMsg = JsonConvert.DeserializeObject<OfferNotification>(JsonConvert.SerializeObject(msg.Data));
			if (!baseMsg?.IsValid ?? true)
				return;

			string Recipient = GetOwnerByMerchantId(baseMsg.MerchantId);

			FrontendMessage messageToSend = new FrontendMessage
			{
				Type = "notification",
				Data = msg.Data
			};

			await _wsBroker.SendAsync(messageToSend.ToString(), Recipient);
		}

		private string GetOwnerByMerchantId(string merchantId)
		{
			// TODO: get. Currently - first index.html user returned.
			return "a6241f60-bf59-4cfe-8dcb-a17c81d7abb5";
		}
	}
}
