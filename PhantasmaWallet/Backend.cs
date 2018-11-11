namespace Phantasma.Wallet
{
    class Backend
    {
        static void Main(string[] args)
        {
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
}
