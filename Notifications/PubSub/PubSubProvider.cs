using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Google.Cloud.PubSub.V1;

namespace Notifications.PubSub
{
	public class PubSubProvider
	{
		private readonly IBackendMessageHandler _beMessageHandler;
		private static SubscriberClient _sub;

		private const string HOST = "localhost";
		private const int PORT = 8085;

		private const string PROJECT_ID = "MyProjId";
		readonly string[] TOPICS = new string[] { "Topic_1", "Topic_2" };

		public PubSubProvider(IBackendMessageHandler backendMessageHandler)
		{
			_beMessageHandler = backendMessageHandler;
		}



		public void Start(Func<string, Task> messageHandler)
		{
			Grpc.Core.Channel chanel = new Grpc.Core.Channel(HOST, PORT, Grpc.Core.ChannelCredentials.Insecure);
			_sub = SubscriberClient.Create(chanel);

			foreach (string topic in TOPICS)
			{
				TopicName topicName = new TopicName(PROJECT_ID, topic);
				SubscriptionName subscriptionName = new SubscriptionName(PROJECT_ID, "s" + DateTime.Now.Ticks.ToString());
				try
				{
					Subscription s = _sub.CreateSubscription(subscriptionName, topicName, null, 0);
				}
				catch (Grpc.Core.RpcException e)
				when (e.Status.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
				{
					// The subscription already exists. OK.
				}
				Task pullTask = Task.Factory.StartNew(() => PullLoop(subscriptionName,
					(string msg) =>
					{
						messageHandler(msg);
						//_beMessageHandler.ProcessMessage(msg);
					}, new CancellationTokenSource().Token));

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
			var u = _sub.ListSubscriptions(new ProjectName(PROJECT_ID));
			Console.WriteLine($"Pulled {response.ReceivedMessages.Count} messages.");
			foreach (ReceivedMessage message in response.ReceivedMessages)
			{
				try
				{
					string msg = message.Message.Data.ToStringUtf8();
					_beMessageHandler.ProcessMessage(msg);
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
