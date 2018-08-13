using System.Collections.Generic;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.Templates;
using LunarLabs.WebServer.HTTP;
using Phantasma.Cryptography;
using System;

namespace PhantasmaWallet
{
    public struct MenuEntry
    {
        public string id;
        public string icon;
        public string caption;
        public bool enabled;
    }

    public struct Holding
    {
        public string name;
        public string symbol;
        public string icon;
        public decimal amount;
        public decimal rate;
        public decimal usd => amount * rate;
    }

    public struct Transaction
    {
        public DateTime date;
        public string hash;
        public string description;
    }

    class Backend
    {
        static MenuEntry[] menuEntries = new MenuEntry[]
        {
            new MenuEntry(){ id = "portfolio", icon = "fa-wallet", caption = "Portfolio", enabled = true},
            new MenuEntry(){ id = "send", icon = "fa-paper-plane", caption = "Send", enabled = true},
            new MenuEntry(){ id = "receive", icon = "fa-qrcode", caption = "Receive", enabled = true},
            new MenuEntry(){ id = "history", icon = "fa-receipt", caption = "Transaction History", enabled = true},
            new MenuEntry(){ id = "storage", icon = "fa-hdd", caption = "Storage", enabled = false},
            new MenuEntry(){ id = "exchange", icon = "fa-chart-bar", caption = "Exchange", enabled = false},
            new MenuEntry(){ id = "sales", icon = "fa-certificate", caption = "Crowdsales", enabled = false},
            new MenuEntry(){ id = "offline", icon = "fa-file-export", caption = "Offline Operation", enabled = true},
            new MenuEntry(){ id = "settings", icon = "fa-cog", caption = "Settings", enabled = true},
            new MenuEntry(){ id = "logout", icon = "fa-sign-out-alt", caption = "Log Out", enabled = true},
        };

        static Transaction[] transactions = new Transaction[]
        {
            new Transaction(){ date =DateTime.UtcNow, hash = "982f14ce20a47b35d864c41bac016ed0d6b970d532102b72bb064866f3e36852", description = "Dummy transaction"}
        };

        static Holding[] holdings = new Holding[]
        {
            new Holding(){ name = "Phantasma", symbol = "SOUL", icon = "phantasma_logo", amount = 250, rate = 0.1m },
            new Holding(){ name = "Funny token", symbol = "LOL", icon = "dummy_logo", amount = 1000, rate = 0.002m }
        };

        static bool HasLogin(HTTPRequest request)
        {
            return request.session.Contains("login");
        }

        static Dictionary<string, object> CreateContext(HTTPRequest request)
        {
            var context = new Dictionary<string, object>();

            context["menu"] = menuEntries;

            if (HasLogin(request))
            {
                context["login"] = true;

                var keyPair = request.session.Get<KeyPair>("login");
                context["name"] = "Anonymous";
                context["address"] = keyPair.Address;

                context["transactions"] = transactions;
                context["holdings"] = holdings;
            }

            context["active"] = request.session.Contains("active")?request.session.Get<string>("active"):"portfolio";

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                context["error"] = error;
                request.session.Remove("error");
            }

            return context;
        }

        static void PushError(HTTPRequest request, string msg)
        {
            request.session.Set("error", msg);
        }

        static void Main(string[] args)
        {
            // initialize a logger
            var log = new LunarLabs.WebServer.Core.Logger();

            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);

            var server = new HTTPServer(log, settings);

            // instantiate a new site, the second argument is the file path where the public site contents will be found
            var site = new Site(server, "public");

            var templates = new TemplateEngine(site, "views");

            site.Get("/", (request) =>
            {
                if (HasLogin(request))
                {
                    return HTTPResponse.Redirect("/portfolio");
                }
                else
                {
                    return HTTPResponse.Redirect("/login");
                }
            });

            site.Get("/login/{key}", (request) =>
            {
                var context = CreateContext(request);

                var key = request.GetVariable("key");
                KeyPair keyPair;

                try
                {
                    keyPair = KeyPair.FromWIF(key);
                }
                catch  (Exception e)
                {
                    Console.WriteLine(e);
                    PushError(request, "Error decoding key...");
                    return HTTPResponse.Redirect("/login");
                }

                request.session.Set("login", keyPair);
                return HTTPResponse.Redirect("/portfolio");
            });

            site.Get("/login", (request) =>
            {
                var context = CreateContext(request);
                return templates.Render(site, context, new string[] { "login" });
            });

            site.Get("/create", (request) =>
            {
                var context = CreateContext(request);

                var keyPair = KeyPair.Generate();
                context["WIF"] = keyPair.ToWIF();
                context["address"] = keyPair.Address;

                return templates.Render(site, context, new string[] { "login" });
            });

            foreach (var entry in menuEntries)
            {
                var url = $"/{entry.id}";

                if (entry.id == "logout")
                {
                    site.Get(url, (request) =>
                    {
                        request.session.Remove("login");
                        return HTTPResponse.Redirect("/login");
                    });
                }
                else
                {
                    site.Get(url, (request) =>
                    {
                        if (!HasLogin(request))
                        {
                            return HTTPResponse.Redirect("/login");
                        }

                        request.session.Set("active", url);
                        var context = CreateContext(request);
                        return templates.Render(site, context, new string[] { "layout", entry.id });
                    });
                }
            }

            server.Run();
        }
    }
}
