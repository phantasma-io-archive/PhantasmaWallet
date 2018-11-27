using System;
using System.Collections.Generic;
using System.Linq;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Blockchain.Contracts.Native;
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

    public struct ErrorContext
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

        public void SetupControllers()
        {
            AccountController = new AccountController();
        }

        private AccountController AccountController { get; set; }

        public TemplateEngine TemplateEngine { get; set; }

        public string RendererView(Dictionary<string, object> context, params string[] templateList)
        {
            return TemplateEngine.Render(context, templateList);
        }

        static KeyPair GetLoginKey(HTTPRequest request)
        {
            var wif = request.session.GetString("wif");
            var keyPair = KeyPair.FromWIF(wif);
            return keyPair;
        }

        static bool HasLogin(HTTPRequest request)
        {
            return request.session.Contains("login");
        }

        static void PushError(HTTPRequest request, string msg, string code = "0")
        {
            var temp = new ErrorContext() { ErrorDescription = msg, ErrorCode = code };
            request.session.SetStruct<ErrorContext>("error", temp);
        }

        void UpdateHistoryContext(Dictionary<string, object> context, KeyPair keyPair, HTTPRequest request)
        {
            var txs = AccountController.GetAccountTransactions(keyPair.Address.Text).Result; //todo remove .Result
            var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
            entry.Count = txs.Length;

            context["transactions"] = txs;
            context["active"] = request.session.Contains("active") ? request.session.GetString("active") : "portfolio";
        }

        void UpdatePortfolioContext(Dictionary<string, object> context, KeyPair keyPair, HTTPRequest request)
        {
            context["holdings"] = AccountController.GetAccountHoldings(keyPair.Address.Text).Result;//todo remove .Result
            context["active"] = request.session.Contains("active") ? request.session.GetString("active") : "portfolio";
        }

        void UpdateSendContext(Dictionary<string, object> context, KeyPair keyPair, HTTPRequest request)
        {
            var tokens = AccountController.GetAccountTokens(keyPair.Address.Text).Result.ToArray();
            var availableChains = new List<string>();
            foreach (var token in tokens)
            {
                foreach (var balanceChain in token.Chains)
                {
                    if (!availableChains.Contains(balanceChain.ChainName))
                    {
                        availableChains.Add(balanceChain.ChainName); //todo add address too | why?
                    }
                }
            }

            context["chainTokens"] = AccountController.PrepareSendHoldings();
            context["availableChains"] = availableChains;
            if (request.session.Contains("error"))
            {
                var error = request.session.GetStruct<ErrorContext>("error");
                context["error"] = error;
                request.session.Remove("error");
            }
        }

        private Dictionary<string, object> InitContext(HTTPRequest request)
        {
            var context = request.session.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (request.session.Contains("error.ErrorDescription")) // TODO this is stupid
            {
                var error = request.session.GetStruct<ErrorContext>("error");
                context["error"] = error;
                request.session.Remove("error");
            }

            context["menu"] = MenuEntries;
            context["networks"] = Networks;

            if (HasLogin(request))
            {
                var keyPair = GetLoginKey(request);

                context["login"] = true;

                context["address"] = keyPair.Address;

                AccountController.InitController();
                context["chains"] = AccountController.PhantasmaChains;
                context["tokens"] = AccountController.PhantasmaTokens;

                var txs = AccountController.GetAccountTransactions(keyPair.Address.Text).Result; //todo remove .Result
                var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
                entry.Count = txs.Length;

                context["transactions"] = txs;
                context["holdings"] = AccountController.GetAccountHoldings(keyPair.Address.Text).Result;

                if (string.IsNullOrEmpty(AccountController.AccountName))
                {
                    context["name"] = "Anonymous";
                }
                else
                {
                    context["name"] = AccountController.AccountName;
                }
            }

            context["active"] = request.session.Contains("active") ? request.session.GetString("active") : "portfolio";

            return context;
        }

        public void SetupHandlers()
        {
            TemplateEngine.Site.Get("/", RouteHome);

            TemplateEngine.Site.Get("/login/{key}", RouteLoginWithParams);

            TemplateEngine.Site.Get("/login", RouteLogin);

            TemplateEngine.Site.Get("/create", RouteCreateAccount);

            TemplateEngine.Site.Post("/sendrawtx", RouteSendRawTx);

            TemplateEngine.Site.Get("/error", RouteError);

            TemplateEngine.Site.Get("/waiting/{txhash}", RouteWaitingTx);

            TemplateEngine.Site.Get("/confirmations/{txhash}", RouteConfirmations);

            TemplateEngine.Site.Get("/register/{name}", RouteRegisterName);

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

            var context = InitContext(request);
            return RendererView(context, "layout", "error");
        }

        private HTTPResponse RouteLoginWithParams(HTTPRequest request)
        {
            var key = request.GetVariable("key");

            try
            {
                request.session.SetString("wif", key);
                request.session.SetBool("login", true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                PushError(request, "Error decoding key...");
                return HTTPResponse.Redirect("/login");
            }

            return HTTPResponse.Redirect("/portfolio");
        }

        private string RouteLogin(HTTPRequest request)
        {
            var context = InitContext(request);
            return RendererView(context, "login");
        }

        private string RouteCreateAccount(HTTPRequest request)
        {
            var keyPair = KeyPair.Generate();

            var context = InitContext(request);
            context["WIF"] = keyPair.ToWIF();
            context["address"] = keyPair.Address;

            return RendererView(context, "login");
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

            request.session.SetString("active", url);
            UpdateMenus(entry);

            var context = InitContext(request);

            switch (entry)
            {
                case "portfolio":
                    UpdatePortfolioContext(context, keyPair, request);
                    break;

                case "history":
                    UpdateHistoryContext(context, keyPair, request);
                    break;

                case "send":
                    UpdateSendContext(context, keyPair, request);
                    break;

                default: break;
            }

            return RendererView(context, "layout", entry);
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

            var context = InitContext(request);

            // get chain addresses
            var chains = (List<ChainElement>)context["chains"];
            var chainAddress =
                chains.SingleOrDefault(a => a.Name.ToLowerInvariant() == chainName.ToLowerInvariant())?.Address;
            var destinationChainAddress = chains.SingleOrDefault(a => a.Name.ToLowerInvariant() == destinationChain.ToLowerInvariant())?.Address;

            var symbol = request.GetVariable("token");
            amountOrId = request.GetVariable(isFungible ? "amount" : "id");

            var keyPair = GetLoginKey(request);
            var result = AccountController.TransferTokens(isFungible, keyPair, addressTo, chainName, chainAddress, destinationChainAddress, symbol, amountOrId).Result;

            if (string.IsNullOrEmpty(result))
            {
                context["error"] = new ErrorContext { ErrorCode = "", ErrorDescription = "Error sending tx." };
                return ""; // TODO why is this empty?? because send.html checks callback for "" or txHash
            }

            context["ConfirmingTxHash"] = result;
            return result;
        }

        private object RouteWaitingTx(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var context = InitContext(request);
            return RendererView(context, "layout", "waiting");
        }

        private object RouteConfirmations(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var txHash = request.GetVariable("txhash");

            request.session.SetStruct<ErrorContext>("error", new ErrorContext { ErrorCode = "", ErrorDescription = $"{txHash} is still not confirmed" });
            var confirmations = AccountController.GetTxConfirmations(txHash).Result.IsConfirmed;
            return confirmations.ToString();
        }

        private object RouteRegisterName(HTTPRequest request)
        {
            var name = request.GetVariable("name");
            var context = InitContext(request);
            if (AccountContract.ValidateAddressName(name))
            {
                if (context["holdings"] is Holding[] balance)
                {
                    var soulBalance = balance.SingleOrDefault(b => b.symbol == "SOUL");
                    if (soulBalance.amount < 0.1m) //RegistrationCost
                    {
                        return false.ToString();
                    }
                }
                else
                {
                    return false.ToString();
                }
            }

            return true.ToString();
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
