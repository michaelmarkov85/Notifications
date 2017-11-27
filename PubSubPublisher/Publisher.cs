using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Cloud.PubSub.V1;
using Messages;
using Microsoft.Extensions.Configuration;

namespace PubSubPublisher
{
	class Publisher
	{
		private IConfigurationRoot _configuration;


		private readonly string _host;
		private readonly int _port;
		private readonly string _projectId;
		private readonly string[] _topics;
		private PublisherClient _pub;

		private TopicName[] TopicNames { get; set; }




		public Publisher(IConfigurationRoot configuration)
		{
			_configuration = configuration;

			IConfigurationSection pubSubConfig = configuration.GetSection("Notifications:PubSub");

			_host = pubSubConfig["Host"];
			_port = Convert.ToInt32(pubSubConfig["Port"]);
			_projectId = pubSubConfig["ProjectId"];
			_topics = pubSubConfig.GetSection("Topics").GetChildren()
				.Select(x => x.Value).ToArray();
		}
		public void Init()
		{
			TopicNames = _topics.Select(t => new TopicName(_projectId, t)).ToArray();
			Grpc.Core.Channel chanel = new Grpc.Core.Channel(_host, _port, Grpc.Core.ChannelCredentials.Insecure);
			_pub = PublisherClient.Create(chanel);

			foreach (var topic in TopicNames)
			{
				try
				{
					_pub.CreateTopic(topic);
				}
				catch (Grpc.Core.RpcException e)
				when (e.Status.StatusCode == Grpc.Core.StatusCode.AlreadyExists)
				{
					// The topic already exists.  OK.
				}
			}
		}

		public void SendRandomMessages(string[] recipients, int frequancy, string messageType)
		{
			while (true)
			{
				TopicName topic = GetRandom<TopicName>(TopicNames);
				string recipient = GetRandom<string>(recipients);
				string message = CreateMessage(topic.TopicId, recipient, messageType);

				Google.Protobuf.ByteString psMessage = Google.Protobuf.ByteString.CopyFromUtf8(message);

				_pub.Publish(topic, new[]
				{ new PubsubMessage()
					{
						Data = psMessage
					}
				});
				Console.WriteLine($"-- Ticked {topic.TopicId}: {message}");
				System.Threading.Thread.Sleep(2000); 
			}
		}

		private string CreateMessage(string topicId, string recipient, string messageType)
		{
			BackendMessage b = new BackendMessage()
			{
				EventType = messageType,
				Data = new { Recipient = recipient, Topic = topicId, Timestamp = DateTime.Now.ToLongTimeString() }
			};
			return b.ToString();
		}

		private T GetRandom<T>(T[] array)
		{
			Random gen = new Random();
			int prob = gen.Next(array.Length);
			return array[prob];
		}
	}
}
