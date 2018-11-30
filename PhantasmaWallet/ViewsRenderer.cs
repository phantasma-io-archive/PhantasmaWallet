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
            AccountController.InitController();
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
            if (request.session.Contains("ConfirmedHash"))
            {
                context["ConfirmedHash"] = request.session.GetString("ConfirmedHash");
            }
        }

        void UpdateSendContext(Dictionary<string, object> context, KeyPair keyPair, HTTPRequest request)
        {
            var cache = FindCache(keyPair.Address);

            var availableChains = new List<string>();
            foreach (var token in cache.tokens)
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

        private static Dictionary<Address, AccountCache> _accountCaches = new Dictionary<Address, AccountCache>();

        private AccountCache FindCache(Address address)
        {
            AccountCache cache;

            var currentTime = DateTime.UtcNow;

            if (_accountCaches.ContainsKey(address))
            {
                cache = _accountCaches[address];
                var diff = currentTime - cache.lastUpdated;

                if (diff.TotalMinutes < 5)
                {
                    return cache;
                }
            }

            cache = new AccountCache()
            {
                lastUpdated = currentTime,
                holdings = AccountController.GetAccountHoldings(address.Text).Result,
                tokens = AccountController.GetAccountTokens(address.Text).Result.ToArray(),
                transactions = AccountController.GetAccountTransactions(address.Text).Result //todo remove .Result,
            };

            _accountCaches[address] = cache;
            return cache;
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

                context["chains"] = AccountController.PhantasmaChains;
                context["tokens"] = AccountController.PhantasmaTokens;

                var cache = FindCache(keyPair.Address);

                var entry = MenuEntries.FirstOrDefault(e => e.Id == "history");
                entry.Count = cache.transactions.Length;

                context["transactions"] = cache.transactions;
                context["holdings"] = cache.holdings;

                if (string.IsNullOrEmpty(AccountController.AccountName))
                {
                    context["name"] = "Anonymous";
                }
                else
                {
                    context["name"] = AccountController.AccountName;
                }
            }

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

            TemplateEngine.Site.Post("/register", RouteRegisterName);

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
            var error = request.session.GetStruct<ErrorContext>("error");
            context["error"] = error;
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

            context["active"] = entry;

            switch (entry)
            {
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
            string result;
            if (chainAddress == destinationChainAddress)
            {
                result = AccountController.TransferTokens(isFungible, keyPair, addressTo, chainName, chainAddress, symbol, amountOrId).Result;
            }
            else //cross chain requires 2 txs
            {
                result = AccountController.CrossChainTransferToken(isFungible, keyPair, addressTo, chainName, chainAddress, destinationChainAddress, symbol, amountOrId).Result;
                if (!string.IsNullOrEmpty(result))
                {
                    request.session.SetBool("IsCrossTransfer", true);

                    request.session.SetStruct<SettleTx>("settleTx",
                        new SettleTx
                        {
                            chainName = chainName,
                            chainAddress = chainAddress,
                            destinationChainAddress = destinationChainAddress,
                        });
                }
            }
            if (string.IsNullOrEmpty(result))  //todo refactor this 
            {
                PushError(request, "Error sending tx.");
                return ""; // TODO why is this empty?? because send.html checks callback for "" or txHash
            }

            return result;
        }

        private object RouteWaitingTx(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var context = InitContext(request);
            context["ConfirmingTxHash"] = request.GetVariable("txhash");

            return RendererView(context, "layout", "waiting");
        }

        private object RouteConfirmations(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var context = InitContext(request);
            var txHash = request.GetVariable("txhash");

            request.session.SetStruct<ErrorContext>("error", new ErrorContext { ErrorCode = "", ErrorDescription = $"{txHash} is still not confirmed" });
            var confirmationDto = AccountController.GetTxConfirmations(txHash).Result;

            if (confirmationDto.IsConfirmed)
            {
                request.session.SetString("ConfirmedHash", txHash);
                if (request.session.GetBool("IsCrossTransfer"))
                {
                    //temp workaround, todo remove
                    var data = request.session.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    var settleTx = AccountController.SettleBlockTransfer(
                        GetLoginKey(request),
                        data["settleTx.chainAddress"].ToString(),
                        confirmationDto.Hash, data["settleTx.destinationChainAddress"].ToString()).Result;

                    // clear
                    request.session.SetBool("IsCrossTransfer", false);

                    if (!string.IsNullOrEmpty(settleTx))
                    {
                        context["ConfirmingTxHash"] = settleTx;
                        return "settling";
                    }
                    return "";
                }
            }

            return confirmationDto.IsConfirmed ? "confirmed" : "unconfirmed";
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
                    if (soulBalance.amount > 0.1m) //RegistrationCost
                    {
                        var keyPair = GetLoginKey(request);
                        var registerTx = AccountController.RegisterName(keyPair, name).Result;
                        return registerTx;
                    }
                }
            }
            // todo fix error, page does not show anything
            PushError(request, "Error while registering name");
            return "";
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
