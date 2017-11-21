using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
			services.AddTransient<PubSub.PubSubProvider>();
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

			sp.GetService<PubSub.PubSubProvider>().Start((msg) =>
				sp.GetService<IBackendMessageHandler>().ProcessMessage(msg)
			);



			app.Run(async (context) =>
			{
				await context.Response.WriteAsync("Hello World!");
			});
		}
	}
}
