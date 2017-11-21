using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Notifications.Middleware
{
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
