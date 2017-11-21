using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace PubSubPublisher
{
	class Program
	{
		static void Main(string[] args)
		{
			string path = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).FullName, "appsettings.json");
			IConfigurationRoot configuration = new ConfigurationBuilder()
				//.AddEnvironmentVariables()
				.AddJsonFile(path, optional: false, reloadOnChange: true)
				.Build();

			string[] recipients = configuration.GetSection("Notifications:Recipients")
				.GetChildren().Select(x => x.Value).ToArray();
			string messageType = 
				configuration.GetSection("Notifications:MessageTypes")["Notification"];


			Publisher p = new Publisher(configuration);
			p.Init();
			p.SendRandomMessages(recipients, 1000, messageType);
		}
	}
}
