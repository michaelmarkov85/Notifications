using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Notifications.Middleware
{
	/// <summary>
	/// Middleware for processing WS HTTP initial handshakes and establish WS connection.
	/// Connections are stored and accessed by IWsManager.
	/// Identity of who is establishing connection from frontEnd is supplied by IIdentityProvider based on
	/// token received in handshake's query string. Identity is stored as OwnerId in IWsManager and associated with WS.
	/// One identity can have multiple WSs.
	/// TODO: think of token revocation procedure.
	/// Every WS connection gets subscription to a message by IWsBroker and callback from IFrontendMessageHandler.
	/// </summary>
	public class NotificationMiddleware
	{
		const string OWNER_QUERY_STRING_TOKEN_KEY = "owner";

		private readonly RequestDelegate _next;
		private readonly IWsManager _wsManager;
		private readonly IWsBroker _wsBroker;
		private readonly IFrontendMessageHandler _feMessageHandler;
		private readonly IIdentityProvider _identityProvider;

		public NotificationMiddleware(RequestDelegate next, IWsManager wsManager,
			IWsBroker wsBroker, IFrontendMessageHandler frontendMessageHandler, IIdentityProvider identityProvider)
		{
			_next = next;
			_wsManager = wsManager;
			_wsBroker = wsBroker;
			_feMessageHandler = frontendMessageHandler;
			_identityProvider = identityProvider;
		}


		public async Task Invoke(HttpContext context)
		{
			// If not WebSockets request - ignore this and go to next middleware
			if (!context.WebSockets.IsWebSocketRequest)
			{
				await _next.Invoke(context);
				return;
			}

			// Establishing WebSocket connection
			CancellationToken ct = context.RequestAborted;
			WebSocket currentSocket = await context.WebSockets.AcceptWebSocketAsync();

			if (currentSocket == null || currentSocket.State != WebSocketState.Open)
				return;

			if (!AreSubProtocolsSupported(currentSocket.SubProtocol))
				return;

			// Getting token from which determine a user/owner
			string token = context.Request.Query[OWNER_QUERY_STRING_TOKEN_KEY].ToString();
			string owner = _identityProvider.GetRecipient(token);
			if (string.IsNullOrWhiteSpace(owner) || !Guid.TryParse(owner, out Guid g))
				return;

			// Adding socket to Manager and subscribing for new messages.
			try
			{
				_wsManager.AddSocket(currentSocket, owner);
				await _wsBroker.Listen(
					currentSocket,
					(msg, ws) => { return _feMessageHandler.ProcessMessage(msg, ws, ct); },
					ct);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private bool AreSubProtocolsSupported(string subProtocol)
		{
			// TODO: Implement sub protocols
			return true;
		}
	}
}
