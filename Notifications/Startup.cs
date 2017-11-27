using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Notifications
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<IWsManager, WebSockets.WsManager>();
			services.AddTransient<IWsBroker, WebSockets.WsBroker>();
			services.AddTransient<IFrontendMessageHandler, Services.FrontendMessageHandler>();
			services.AddTransient<IBackendMessageHandler, Services.BackendMessageHandler>();
			services.AddTransient<IIdentityProvider, Services.IdentityProvider>();
			services.AddTransient<PubSub.PubSubProvider>(
				// TODO: make it via configuration
				sp => new PubSub.PubSubProvider(Environment.GetEnvironmentVariable("PUBSUB_PROJECT_ID"), 1000));
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IServiceProvider sp, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseWebSockets(new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
				ReceiveBufferSize = 4 * 1024
			});

			// Registering middleware that manages WebSocket connections and 
			// listens to frontend client messages
			app.UseMiddleware<Middleware.NotificationMiddleware>();

			app.UseFileServer();


			// PUB SUB
			// Working with Google PubSub requires an environment variable GOOGLE_APPLICATION_CREDENTIALS,
			// pointing to json file with keys, defining user rights. 
			// That file is granted by devops. Ask Aleksey B.
			// Also put into variable PUBSUB_PROJECT_ID projectDd - for security reasons.

			// Setting up and starting PubSub subscription -  internal loop



			Func<string, Task> pubSubCallback = (msg) => sp.GetService<IBackendMessageHandler>().ProcessMessage(msg);

			var subsctiptions = new List<(string topicId, string subscriptionId, Func<string, Task> messageHandler)>
			{
				//TODO: make it via configuration
				("bus_offer_commented", "mm_of_cmnt_1", pubSubCallback)
			};

			sp.GetService<PubSub.PubSubProvider>().Start(subsctiptions);
		}
	}
}
