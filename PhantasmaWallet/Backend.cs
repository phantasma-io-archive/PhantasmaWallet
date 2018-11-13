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

            viewsRenderer.Init();
            viewsRenderer.SetupHandlers();
            viewsRenderer.SetupControllers();

            //todo remove testing
            //var testService = new PhantasmaApiService();
            //Task.Run(async () =>
            //{
            //    var account = await testService.GetAccount("P16m9XNDHxUex9hsGRytzhSj58k6W7BT5Xsvs3tHjJUkX");
            //    var block = await testService.GetBlock("0x14D1857735940BA96BF3A6F6B8F91F2DA4CFC732C6AD77038171EE8BB74B5182");
            //    var x = 1;
            //}).Wait();

            site.server.Run(site);
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
            serviceCollection.AddScoped<IPhantasmaRpcService>(provider => new PhantasmaRpcService(new JsonRpc.Client.RpcClient(new Uri("http://localhost:7077/rpc"),httpClientHandler:new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })));
        }
    }
}
