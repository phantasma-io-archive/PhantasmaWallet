using System;
using System.Collections.Generic;
using System.Linq;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Core;
using Phantasma.Cryptography;
using Phantasma.Wallet.Controllers;

namespace Phantasma.Wallet
{
    public class MenuEntry
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Caption { get; set; }
        public bool Enabled { get; set; }
        public int Count { get; set; }
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
            foreach (var menuEntry in MenuEntries)
            {
                UpdateContext(menuEntry.Id, menuEntry);
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

        void UpdateHistoryContext(HTTPRequest request)
        {
            var txs = AccountController.GetAccountTransactions().Result; //todo remove .Result
            var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
            entry.Count = txs.Length;

            UpdateContext("transactions", txs);
            UpdateContext("active", request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio");

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                UpdateContext("error", error);
                request.session.Remove("error");
            }
        }

        void UpdatePortfolioContext(HTTPRequest request)
        {
            UpdateContext("holdings", AccountController.GetAccountHoldings().Result);//todo remove .Result
            UpdateContext("active", request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio");

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                UpdateContext("error", error);
                request.session.Remove("error");
            }
        }

        void UpdateSendContext(HTTPRequest request)
        {
            var tokens = AccountController.GetAccountTokens().Result.ToArray();
            var availableChains = new List<string>();
            foreach (var token in tokens)
            {
                foreach (var balanceChain in token.Chains)
                {
                    if (!availableChains.Contains(balanceChain.ChainName))
                    {
                        availableChains.Add(balanceChain.ChainName); //todo add address too
                    }
                }
            }
            UpdateContext("chainTokens", tokens);
            UpdateContext("availableChains", availableChains);
            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                UpdateContext("error", error);
                request.session.Remove("error");
            }
        }

        void InitContext(HTTPRequest request)
        {
            UpdateContext("menu", MenuEntries);
            if (HasLogin(request))
            {
                UpdateContext("login", true);

                var keyPair = request.session.Get<KeyPair>("login");
                UpdateContext("name", "Anonymous");
                UpdateContext("address", keyPair.Address);

                var txs = AccountController.GetAccountTransactions().Result; //todo remove .Result
                var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
                entry.Count = txs.Length;

                UpdateContext("chains", AccountController.GetChains().Result);
                UpdateContext("transactions", txs);
                UpdateContext("holdings", AccountController.GetAccountHoldings().Result);
            }

            UpdateContext("active", request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio");

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                UpdateContext("error", error);
                request.session.Remove("error");
            }
        }

        public void SetupHandlers()
        {
            TemplateEngine.Site.Get("/", RouteHome);

            TemplateEngine.Site.Get("/login/{key}", RouteLoginWithParams);

            TemplateEngine.Site.Get("/login", RouteLogin);

            TemplateEngine.Site.Get("/create", RouteCreateAccount);

            TemplateEngine.Site.Get("/sendrawtx", RouteSendRawTx);

            foreach (var entry in MenuEntries)
            {
                var url = $"/{entry.Id}";

                if (entry.Id == "logout")
                {
                    TemplateEngine.Site.Get(url, RouteLogout);
                }
                else
                {
                    TemplateEngine.Site.Get(url, request => RouteMenuItems(request, url, entry.Id));
                }
            }
        }

        #region Routes
        private HTTPResponse RouteHome(HTTPRequest request)
        {
            return HTTPResponse.Redirect(HasLogin(request) ? "/portfolio" : "/login");
        }

        private HTTPResponse RouteLoginWithParams(HTTPRequest request)
        {
            var key = request.GetVariable("key");
            KeyPair keyPair;

            try
            {
                keyPair = KeyPair.FromWIF(key);
                AccountController.SessionKeyPair = keyPair;//todo move
                InitContext(request);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PushError(request, "Error decoding key...");
                return HTTPResponse.Redirect("/login");
            }

            request.session.Set("login", keyPair);
            return HTTPResponse.Redirect("/portfolio");
        }

        private string RouteLogin(HTTPRequest request)
        {
            return RendererView("login");
        }

        private string RouteCreateAccount(HTTPRequest request)
        {
            var keyPair = KeyPair.Generate();
            UpdateContext("WIF", keyPair.ToWIF());
            UpdateContext("address", keyPair.Address);

            return RendererView("login");
        }

        private HTTPResponse RouteLogout(HTTPRequest request)
        {
            request.session.Remove("login");
            return HTTPResponse.Redirect("/login");
        }

        private object RouteMenuItems(HTTPRequest request, string url, string entry)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }
            request.session.Set("active", url);

            switch (entry)
            {
                case "portfolio":
                    UpdatePortfolioContext(request);
                    break;
                case "history":
                    UpdateHistoryContext(request);
                    break;
                case "send":
                    UpdateSendContext(request);
                    break;

                default: break;
            }

            return RendererView("layout", entry);
        }

        private object RouteSendRawTx(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var addressTo = request.GetVariable("dest");
            var chainAddress = "NztsEZP7dtrzRBagogUYVp6mgEFbhjZfvHMVkd2bYWJfE";//request.GetVariable("chainAddress");
            var symbol = request.GetVariable("token");
            var amount = request.GetVariable("amount");

            var result = AccountController.SendRawTx(addressTo, chainAddress, symbol, amount).Result;

            return HTTPResponse.Redirect("/portfolio");
        }
        #endregion


        private static readonly MenuEntry[] MenuEntries = new MenuEntry[]
        {
            new MenuEntry(){ Id = "portfolio", Icon = "fa-wallet", Caption = "Portfolio", Enabled = true},
            new MenuEntry(){ Id = "send", Icon = "fa-paper-plane", Caption = "Send", Enabled = true},
            new MenuEntry(){ Id = "receive", Icon = "fa-qrcode", Caption = "Receive", Enabled = true},
            new MenuEntry(){ Id = "history", Icon = "fa-receipt", Caption = "Transactions", Enabled = true},
            new MenuEntry(){ Id = "storage", Icon = "fa-hdd", Caption = "Storage", Enabled = false},
            new MenuEntry(){ Id = "exchange", Icon = "fa-chart-bar", Caption = "Exchange", Enabled = false},
            new MenuEntry(){ Id = "sales", Icon = "fa-certificate", Caption = "Crowdsales", Enabled = false},
            new MenuEntry(){ Id = "offline", Icon = "fa-file-export", Caption = "Offline Operation", Enabled = true},
            new MenuEntry(){ Id = "settings", Icon = "fa-cog", Caption = "Settings", Enabled = true},
            new MenuEntry(){ Id = "logout", Icon = "fa-sign-out-alt", Caption = "Log Out", Enabled = true},
        };
    }
}
