using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.RpcClient;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Wallet
{
    class Backend
    {
        public static IServiceProvider AppServices => _app.Services;
        private static Application _app;

        static void Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            _app = new Application(serviceCollection);

            var server = HostBuilder.CreateServer(args);
            var viewsRenderer = new ViewsRenderer(server, "views");

            viewsRenderer.SetupHandlers();
            viewsRenderer.SetupControllers();

            server.Run();
        }
    }

    public class Application
    {
        public IServiceProvider Services { get; set; }

        public Application(IServiceCollection serviceCollection)
        {
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IPhantasmaRpcService>(provider => new PhantasmaRpcService(new RpcClient.Client.RpcClient(new Uri("http://localhost:7077/rpc"), httpClientHandler: new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })));
        }
    }
}
