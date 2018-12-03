using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Wallet.Interfaces;
using Phantasma.Wallet.Services;

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

            var site = HostBuilder.CreateSite(args, "public");
            var viewsRenderer = new ViewsRenderer(site, "views");

            viewsRenderer.SetupHandlers();
            viewsRenderer.SetupControllers();

            site.Server.Run();
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
            serviceCollection.AddScoped<IPhantasmaRestService, PhantasmaRestService>();
            serviceCollection.AddScoped<IPhantasmaRpcService>(provider => new PhantasmaRpcService(new JsonRpc.Client.RpcClient(new Uri("http://localhost:7077/rpc"), httpClientHandler: new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })));
        }
    }
}
