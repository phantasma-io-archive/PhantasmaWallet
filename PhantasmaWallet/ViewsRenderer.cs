using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Cryptography;
using Phantasma.Wallet.Controllers;

namespace Phantasma.Wallet
{
    public struct MenuEntry
    {
        public string id;
        public string icon;
        public string caption;
        public bool enabled;
        public int count;
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

    public class ViewsRenderer
    {
        public ViewsRenderer(LunarLabs.WebServer.Core.Site site, string viewsPath)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            TemplateEngine = new TemplateEngine(site, viewsPath);
        }

        public void Init()
        {
            foreach (var menuEntry in menuEntries)
            {

                UpdateContext(menuEntry.id, menuEntry);
            }
        }

        public void SetupControllers()
        {
            AccountController = new AccountController();
        }

        private AccountController AccountController { get; set; }

        public TemplateEngine TemplateEngine { get; set; }

        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        public string RendererView(params string[] templateList)
        {
            return TemplateEngine.Render(Context, templateList);
        }

        public void UpdateContext(string key, object value)
        {
            Context[key] = value;
        }

        static bool HasLogin(HTTPRequest request)
        {
            return request.session.Contains("login");
        }

        static void PushError(HTTPRequest request, string msg)
        {
            request.session.Set("error", msg);
        }

        async Task CreateContext(HTTPRequest request)
        {
            Context["menu"] = menuEntries;

            if (HasLogin(request))
            {
                Context["login"] = true;

                var keyPair = request.session.Get<KeyPair>("login");
                Context["name"] = "Anonymous";
                Context["address"] = keyPair.Address;

                Context["transactions"] = transactions;
                Context["holdings"] = AccountController.GetAccountInfo(keyPair.Address.Text).Result; //todo remove .Result
            }

            Context["active"] = request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio";

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                Context["error"] = error;
                request.session.Remove("error");
            }
        }

        public void SetupHandlers() //todo separate each call
        {
            TemplateEngine.Site.Get("/", (request) =>
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

            TemplateEngine.Site.Get("/login/{key}", (request) =>
            {
                CreateContext(request);

                var key = request.GetVariable("key");
                KeyPair keyPair;

                try
                {
                    keyPair = KeyPair.FromWIF(key);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    PushError(request, "Error decoding key...");
                    return HTTPResponse.Redirect("/login");
                }

                request.session.Set("login", keyPair);
                return HTTPResponse.Redirect("/portfolio");
            });

            TemplateEngine.Site.Get("/login", (request) =>
            {
                CreateContext(request);
                return RendererView("layout", "login");
            });

            TemplateEngine.Site.Get("/create", (request) =>
            {
                CreateContext(request);

                var keyPair = KeyPair.Generate();
                Context["WIF"] = keyPair.ToWIF();
                Context["address"] = keyPair.Address;
                return RendererView("layout", "login");
            });

            foreach (var entry in menuEntries)
            {
                var url = $"/{entry.id}";

                if (entry.id == "logout")
                {
                    TemplateEngine.Site.Get(url, (request) =>
                    {
                        request.session.Remove("login");
                        return HTTPResponse.Redirect("/login");
                    });
                }
                else
                {
                    TemplateEngine.Site.Get(url, (request) =>
                    {
                        if (!HasLogin(request))
                        {
                            return HTTPResponse.Redirect("/login");
                        }

                        request.session.Set("active", url);
                        CreateContext(request);
                        return RendererView("layout", entry.id);
                    });
                }
            }
        }

        static MenuEntry[] menuEntries = new MenuEntry[]
        {
            new MenuEntry(){ id = "portfolio", icon = "fa-wallet", caption = "Portfolio", enabled = true},
            new MenuEntry(){ id = "send", icon = "fa-paper-plane", caption = "Send", enabled = true},
            new MenuEntry(){ id = "receive", icon = "fa-qrcode", caption = "Receive", enabled = true},
            new MenuEntry(){ id = "history", icon = "fa-receipt", caption = "Transactions", enabled = true, count = 1},
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

    }
}
