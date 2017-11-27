using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;

namespace Notifications.PubSub
{
	public class PubSubProvider
	{
		private readonly int _requestFrequancy;
		private readonly string _projectId;

		private readonly SubscriberClient _sub;

		public PubSubProvider(string projectId, int requestFrequancy)
		{
			_projectId = projectId;
			_requestFrequancy = requestFrequancy;
			_sub = SubscriberClient.Create();
		}

		public void Start(List<(string topicId, string subscriptionId, Func<string, Task> messageHandler)> subsctiptions)
		{
			foreach (var s in subsctiptions)
			{
				SubscriptionName subscriptionName = new SubscriptionName(_projectId, s.subscriptionId);
				TopicName topic = new TopicName(_projectId, s.topicId);

				// TODO: some handling
				bool subscriptionExists = GetOrCreateSubscription(subscriptionName, topic);

				Task pullTask = Task.Factory.StartNew(() => PullLoop(subscriptionName,
					msg => s.messageHandler(msg), new CancellationTokenSource().Token));
			}
		}

		private bool GetOrCreateSubscription(SubscriptionName subscriptionName, TopicName topic)
		{
			Subscription sub;
			try
			{
				sub = _sub.CreateSubscription(subscriptionName, topic, null, 0);
				return true;
			}
			catch (Grpc.Core.RpcException e)
			when (e.Status.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
			{
				return true;
				// The subscription already exists. OK.
			}
			catch (Grpc.Core.RpcException e)
			when (e.Status.StatusCode == Grpc.Core.StatusCode.PermissionDenied)
			{
				// Bad. Shouldn't happen.
				// TODO: get permissions, get this logged. 
				return false;
			}
		}

		private void PullLoop(SubscriptionName subscriptionName, Action<string> callback, CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					Thread.Sleep(1000);
					PullOnce(subscriptionName, callback, cancellationToken);
				}
				catch (Exception e)
				{
					Console.WriteLine("PullOnce() failed." + e.Message);
				}
			}
		}

		private void PullOnce(SubscriptionName subscriptionName, Action<string> callback, CancellationToken cancellationToken)
		{
			// Pull some messages from the subscription.
			var response = _sub.Pull(subscriptionName, false, 10,
				CallSettings.FromCallTiming(
					CallTiming.FromExpiration(
						Expiration.FromTimeout(
							TimeSpan.FromSeconds(90)))));
			if (response.ReceivedMessages == null)
			{
				// HTTP Request expired because the queue was empty.  OK.
				Console.WriteLine("Pulled no messages.");
				return;
			}
			Console.WriteLine($"Pulled {response.ReceivedMessages.Count} messages.");
			foreach (ReceivedMessage message in response.ReceivedMessages)
			{
				try
				{
					string msg = message.Message.Data.ToStringUtf8();
					callback(msg);
				}
				catch (Exception e)
				{
					Console.WriteLine("Error processing message." + e.Message);
				}
			}
			// Acknowledge the message so we don't see it again.
			var ackIds = new string[response.ReceivedMessages.Count];
			for (int i = 0; i < response.ReceivedMessages.Count; ++i)
				ackIds[i] = response.ReceivedMessages[i].AckId;
			_sub.Acknowledge(subscriptionName, ackIds);
		}
	}
}
