using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Cryptography;
using Phantasma.Wallet.Controllers;
using Phantasma.Wallet.DTOs;

namespace Phantasma.Wallet
{
    public class MenuEntry
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Caption { get; set; }
        public bool Enabled { get; set; }
        public int Count { get; set; }
        public bool IsSelected { get; set; }
    }

    public struct Holding
    {
        public string name;
        public string symbol;
        public string icon;
        public decimal amount;
        public decimal rate;
        public decimal usd => (amount * rate);
        public string amountFormated => amount.ToString("0.####");
        public string usdFormated => usd.ToString("0.####");
    }

    public struct Transaction
    {
        public DateTime date;
        public string hash;
        public string description;
    }

    public class Net
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class ErrorContext
    {
        public string ErrorDescription { get; set; }
        public string ErrorCode { get; set; }
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
            //foreach (var menuEntry in MenuEntries)
            //{
            //    UpdateContext(menuEntry.Id, menuEntry);
            //}
        }

        public void SetupControllers()
        {
            AccountController = new AccountController();
        }

        private AccountController AccountController { get; set; }

        public TemplateEngine TemplateEngine { get; set; }

        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        public string RendererView(HTTPRequest request, params string[] templateList)
        {
            var context = request.session.data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return TemplateEngine.Render(context, templateList);
        }

        public void UpdateContext(HTTPRequest request, string key, object value)
        {
            request.session.Set(key, value);
            Context = request.session.data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        static KeyPair GetLoginKey(HTTPRequest request)
        {
            return request.session.Get<KeyPair>("keypair");
        }

        static bool HasLogin(HTTPRequest request)
        {
            return request.session.Contains("login");
        }

        static void PushError(HTTPRequest request, string msg)
        {
            request.session.Set("error", msg);
        }

        void UpdateHistoryContext(KeyPair keyPair, HTTPRequest request)
        {
            var txs = AccountController.GetAccountTransactions(keyPair.Address.Text).Result; //todo remove .Result
            var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
            entry.Count = txs.Length;

            UpdateContext(request, "transactions", txs);
            UpdateContext(request, "active", request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio");

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<ErrorContext>("error");
                UpdateContext(request, "error", error);
                request.session.Remove("error");
            }
        }

        void UpdatePortfolioContext(KeyPair keyPair, HTTPRequest request)
        {
            UpdateContext(request, "holdings", AccountController.GetAccountHoldings(keyPair.Address.Text).Result);//todo remove .Result
            UpdateContext(request, "active", request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio");

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<ErrorContext>("error");
                UpdateContext(request, "error", error);
                request.session.Remove("error");
            }
        }

        void UpdateSendContext(KeyPair keyPair, HTTPRequest request)
        {
            var tokens = AccountController.GetAccountTokens(keyPair.Address.Text).Result.ToArray();
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

            UpdateContext(request, "chainTokens", AccountController.PrepareSendHoldings());
            UpdateContext(request, "availableChains", availableChains);
            if (request.session.Contains("error"))
            {
                var error = request.session.Get<ErrorContext>("error");
                UpdateContext(request, "error", error);
                request.session.Remove("error");
            }
        }

        void InitContext(HTTPRequest request)
        {
            UpdateContext(request, "menu", MenuEntries);
            UpdateContext(request, "networks", Networks);

            if (HasLogin(request))
            {
                var keyPair = GetLoginKey(request);

                UpdateContext(request, "login", true);

                UpdateContext(request, "name", "Anonymous");
                UpdateContext(request, "address", keyPair.Address);

                var txs = AccountController.GetAccountTransactions(keyPair.Address.Text).Result; //todo remove .Result
                var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
                entry.Count = txs.Length;

                UpdateContext(request, "chains", AccountController.GetChains().Result);

                UpdateContext(request, "transactions", txs);
                UpdateContext(request, "holdings", AccountController.GetAccountHoldings(keyPair.Address.Text).Result);
            }

            UpdateContext(request, "active", request.session.Contains("active") ? request.session.Get<string>("active") : "portfolio");

            if (request.session.Contains("error"))
            {
                var error = request.session.Get<string>("error");
                UpdateContext(request, "error", error);
                request.session.Remove("error");
            }
        }

        public void SetupHandlers()
        {
            TemplateEngine.Site.Get("/", RouteHome);

            TemplateEngine.Site.Get("/login/{key}", RouteLoginWithParams);

            TemplateEngine.Site.Get("/login", RouteLogin);

            TemplateEngine.Site.Get("/create", RouteCreateAccount);

            TemplateEngine.Site.Post("/sendrawtx", RouteSendRawTx);

            TemplateEngine.Site.Get("/error", RouteError);

            TemplateEngine.Site.Get("/wait/{txhash}", RouteWaitingTx);

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

        private object RouteError(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }
            return RendererView(request, "layout", "error");
        }

        private HTTPResponse RouteLoginWithParams(HTTPRequest request)
        {
            var key = request.GetVariable("key");
            KeyPair keyPair;

            try
            {
                keyPair = KeyPair.FromWIF(key);
                request.session.Set("keypair", keyPair);
                request.session.Set("login", true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PushError(request, "Error decoding key...");
                return HTTPResponse.Redirect("/login");
            }

            InitContext(request);
            return HTTPResponse.Redirect("/portfolio");
        }

        private string RouteLogin(HTTPRequest request)
        {
            return RendererView(request, "login");
        }

        private string RouteCreateAccount(HTTPRequest request)
        {
            var keyPair = KeyPair.Generate();
            UpdateContext(request, "WIF", keyPair.ToWIF());
            UpdateContext(request, "address", keyPair.Address);

            return RendererView(request, "login");
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

            var keyPair = GetLoginKey(request);

            request.session.Set("active", url);
            UpdateMenus(entry);
            switch (entry)
            {
                case "portfolio":
                    UpdatePortfolioContext(keyPair, request);
                    break;
                case "history":
                    UpdateHistoryContext(keyPair, request);
                    break;
                case "send":
                    UpdateSendContext(keyPair, request);
                    break;

                default: break;
            }

            return RendererView(request, "layout", entry);
        }

        private object RouteSendRawTx(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var isFungible = bool.Parse(request.GetVariable("fungible"));
            string amountOrId = null;
            var addressTo = request.GetVariable("dest");

            var chainName = request.GetVariable("chain");
            var destinationChain = request.GetVariable("destChain");

            // get chain addresses
            var chains = (Chains)Context["chains"];
            var chainAddress =
                chains.ChainList.SingleOrDefault(a => a.Name.ToLowerInvariant() == chainName.ToLowerInvariant())?.Address;
            var destinationChainAddress = chains.ChainList.SingleOrDefault(a => a.Name.ToLowerInvariant() == destinationChain.ToLowerInvariant())?.Address;

            var symbol = request.GetVariable("token");
            amountOrId = request.GetVariable(isFungible ? "amount" : "id");

            var keyPair = GetLoginKey(request);
            var result = AccountController.TransferTokens(isFungible, keyPair, addressTo, chainName, chainAddress, destinationChainAddress, symbol, amountOrId).Result;

            if (string.IsNullOrEmpty(result))
            {
                UpdateContext(request, "error", new ErrorContext { ErrorCode = "", ErrorDescription = "Error sending tx." });
                return HTTPResponse.Redirect("/error");
            }
            //return HTTPResponse.Redirect($"/wait/{result}");
            return HTTPResponse.Redirect($"/history");
        }

        private object RouteWaitingTx(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }
            var txHash = request.GetVariable("txhash");
            RendererView(request, "layout", "waiting");

            while (!AccountController.GetTxConfirmations(txHash).Result.IsConfirmed)
            {
                Thread.Sleep(2000);
            }
            return RendererView(request, "layout", "history");
        }
        #endregion


        private static readonly MenuEntry[] MenuEntries = new MenuEntry[]
        {
            new MenuEntry(){ Id = "portfolio", Icon = "fa-wallet", Caption = "Portfolio", Enabled = true, IsSelected = true},
            new MenuEntry(){ Id = "send", Icon = "fa-paper-plane", Caption = "Send", Enabled = true, IsSelected = false},
            new MenuEntry(){ Id = "receive", Icon = "fa-qrcode", Caption = "Receive", Enabled = true, IsSelected = false},
            new MenuEntry(){ Id = "history", Icon = "fa-receipt", Caption = "Transactions", Enabled = true, IsSelected = false},
            new MenuEntry(){ Id = "storage", Icon = "fa-hdd", Caption = "Storage", Enabled = false, IsSelected = false},
            new MenuEntry(){ Id = "exchange", Icon = "fa-chart-bar", Caption = "Exchange", Enabled = false, IsSelected = false},
            new MenuEntry(){ Id = "sales", Icon = "fa-certificate", Caption = "Crowdsales", Enabled = false, IsSelected = false},
            new MenuEntry(){ Id = "offline", Icon = "fa-file-export", Caption = "Offline Operation", Enabled = false, IsSelected = false},
            //new MenuEntry(){ Id = "settings", Icon = "fa-cog", Caption = "Settings", Enabled = true, IsSelected = false},
            new MenuEntry(){ Id = "logout", Icon = "fa-sign-out-alt", Caption = "Log Out", Enabled = true, IsSelected = false},
        };

        private static Chains Chains { get; set; }

        private static readonly Net[] Networks = new Net[]
        {
            new Net{Name = "simnet", IsEnabled = true, Value = 1},
            new Net{Name = "testnet", IsEnabled = false, Value = 2},
            new Net{Name = "mainnet", IsEnabled = false, Value = 3},
        };

        private void UpdateMenus(string id)
        {
            foreach (var menuEntry in MenuEntries)
            {
                menuEntry.IsSelected = menuEntry.Id == id;
            }
        }
    }
}
