using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace Notifications.WebSockets
{
	/// <summary>
	/// Class for storing data about recipients and their sockets. Must be a singleton.
	/// </summary>
	public class WsManager : IWsManager
	{
		#region STORAGE
		private ConcurrentDictionary<string, List<string>> _owners = new ConcurrentDictionary<string, List<string>>();

		// key: [SocketId] -> value:[Owner] auxiliary dictionary for safe removal purpose only
		private ConcurrentDictionary<string, string> _ownersRemove = new ConcurrentDictionary<string, string>();

		// key: [SocketId] -> value:[WebSocket] for adding and iteration
		private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

		// key: [WebSocket] -> value:[SocketId] auxiliary dictionary for safe removal purpose only
		private ConcurrentDictionary<WebSocket, string> _socketsRemove = new ConcurrentDictionary<WebSocket, string>();
		#endregion


		public bool AddSocket(WebSocket socket, string recipient)
		{
			// Check if somehow socket is already in collection
			if (_socketsRemove.TryGetValue(socket, out string existingSocketId))
			{
				Console.WriteLine($"Warning! [AddSocket] Trying to add existing socket to a collection. Owner: {recipient}, socketId: {existingSocketId}.");
				// if _ownersRemove contains record of that socket has an owner
				if (_ownersRemove.TryGetValue(existingSocketId, out string existingOwner))
				{
					// if right socket owner - just return existingSocketId 
					if (string.Equals(recipient, existingOwner, StringComparison.InvariantCultureIgnoreCase))
						return true;
					else
						throw new Exception($"Error! [AddSocket] One socket belongs to different owners. " +
							$"SocketId: {existingSocketId}, existing Owner: {existingOwner}, new Owner: {recipient}.");
				}
				else
					throw new Exception($"Error! [AddSocket] One socket record exists, but doesn't belongs to any owner. " +
							$"SocketId: {existingSocketId}, new Owner: {recipient}.");
			}

			// Adding to socket collection
			string socketId = Guid.NewGuid().ToString();
			if (!_sockets.TryAdd(socketId, socket) || !_socketsRemove.TryAdd(socket, socketId))
				throw new Exception($"Error! [AddSocket] Cannot add new socket to a collection. " +
							$"SocketId: {socketId}, new Owner: {recipient}.");

			// Adding owner's collection - to existing or a new one
			if (_owners.TryGetValue(recipient, out List<string> socketIds))
			{
				try
				{
					socketIds.Add(socketId);
				}
				catch (Exception ex)
				{
					throw new Exception($"Error! [AddSocket] Cannot add new socketId to an existing owner. " +
							$"SocketId: {socketId}, new Owner: {recipient}. Exception: {ex.Message}.");
				}
				if (!_ownersRemove.TryAdd(socketId, recipient))
				{
					throw new Exception($"Error! [AddSocket] Cannot add new record to _ownersRemove collection. " +
							$"SocketId: {socketId}, new Owner: {recipient}.");
				}
			}
			else
			{
				if (!_owners.TryAdd(recipient, new List<string> { socketId }) || !_ownersRemove.TryAdd(socketId, recipient))
					throw new Exception($"Error! [AddSocket] Cannot add new record to _owners or _ownersRemove collection. " +
							$"SocketId: {socketId}, new Owner: {recipient}.");
			}

			return true;
		}
		public bool RemoveSocket(WebSocket socket)
		{
			if (_socketsRemove.TryRemove(socket, out string socketId))
				if (_sockets.TryRemove(socketId, out WebSocket existingSocket))
					if (_ownersRemove.TryRemove(socketId, out string owner))
						if (_owners.TryGetValue(owner, out var bag))
							if (bag.Remove(socketId))
								return true;
			return false;
		}

		public IEnumerable<string> GetAllRecipients()
		{
			List<string> recipients = _owners.Keys.ToList();
			return recipients;
		}
		public string GetRecipient(WebSocket socket)
		{
			string recipient = null;
			string socketId = GetSocketId(socket);
			if (!string.IsNullOrEmpty(socketId))
				_ownersRemove.TryGetValue(socketId, out recipient);
			return recipient;
		}
		public IEnumerable<WebSocket> GetSockets(string recipient)
		{
			if (_owners.TryGetValue(recipient, out List<string> socketIds))
				if (socketIds != null && socketIds.Count > 0)
					foreach (string sId in socketIds)
						if (_sockets.TryGetValue(sId, out WebSocket socket))
							if (socket != null && socket.State == WebSocketState.Open)
								yield return socket;
		}
		public IEnumerable<WebSocket> GetSockets(List<string> recipients)
		{
			List<WebSocket> result = new List<WebSocket>();
			if (recipients == null || recipients.Count < 1)
				return result;

			foreach (string r in recipients)
			{
				var portion = GetSockets(r);
				if (portion != null && portion.Count() > 0)
					result.AddRange(portion.ToList());
			}

			return result;
		}
		// Private
		private string GetSocketId(WebSocket socket)
		{
			bool result = _socketsRemove.TryGetValue(socket, out string socketId);
			return result ? socketId : null;
		}

	}
}
