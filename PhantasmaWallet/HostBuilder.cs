using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using System;

namespace Phantasma.Wallet
{
    public static class HostBuilder
    {
        public static LunarLabs.WebServer.Core.Site CreateSite(string[] args, string filePath)
        {
            var log = new ConsoleLogger();

            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);

            var sessionStorage = new FileSessionStorage("session");
            sessionStorage.CookieExpiration = TimeSpan.FromHours(6);

            var server = new HTTPServer(settings, log, sessionStorage) { AutoCompress = false };

            // instantiate a new site, the second argument is the relative file path where the public site contents will be found
            return new LunarLabs.WebServer.Core.Site(server, filePath);
        }
    }
}
