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
        public ViewsRenderer(HTTPServer server, string viewsPath)
        {
            if (server == null) throw new ArgumentNullException(nameof(server));
            TemplateEngine = new TemplateEngine(server, viewsPath);
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

        void UpdateHistoryContext(Dictionary<string, object> context, HTTPRequest request)
        {
            if (request.session.Contains("confirmedHash"))
            {
                context["confirmedHash"] = request.session.GetString("confirmedHash");
            }
        }

        void UpdateSendContext(Dictionary<string, object> context, KeyPair keyPair, HTTPRequest request)
        {
            var cache = FindCache(keyPair.Address);

            var availableChains = new List<string>();
            foreach (var token in cache.Tokens)
            {
                foreach (var balanceChain in token.Chains)
                {
                    if (!availableChains.Contains(balanceChain.ChainName))
                    {
                        availableChains.Add(balanceChain.ChainName);
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

        #region Cache

        private static readonly Dictionary<Address, AccountCache> _accountCaches = new Dictionary<Address, AccountCache>();

        private void InvalidateCache(Address address)
        {
            if (_accountCaches.ContainsKey(address))
            {
                _accountCaches.Remove(address);
            }
        }

        private AccountCache FindCache(Address address)
        {
            AccountCache cache;

            var currentTime = DateTime.UtcNow;

            if (_accountCaches.ContainsKey(address))
            {
                cache = _accountCaches[address];
                var diff = currentTime - cache.LastUpdated;

                if (diff.TotalMinutes < 5)
                {
                    return cache;
                }
            }

            cache = new AccountCache()
            {
                LastUpdated = currentTime,
                Holdings = AccountController.GetAccountHoldings(address.Text).Result,
                Tokens = AccountController.GetAccountTokens(address.Text).Result.ToArray(),
                Transactions = AccountController.GetAccountTransactions(address.Text).Result //todo remove .Result,
            };

            _accountCaches[address] = cache;
            return cache;
        }

        #endregion


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
                entry.Count = cache.Transactions.Length;

                context["transactions"] = cache.Transactions;
                context["holdings"] = cache.Holdings;

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
            TemplateEngine.Server.Get("/", RouteHome);

            TemplateEngine.Server.Get("/login/{key}", RouteLoginWithParams);

            TemplateEngine.Server.Get("/login", RouteLogin);

            TemplateEngine.Server.Get("/create", RouteCreateAccount);

            TemplateEngine.Server.Post("/sendrawtx", RouteSendRawTx);

            TemplateEngine.Server.Get("/error", RouteError);

            TemplateEngine.Server.Get("/waiting/{txhash}", RouteWaitingTx);

            TemplateEngine.Server.Get("/confirmations/{txhash}", RouteConfirmations);

            TemplateEngine.Server.Post("/register", RouteRegisterName);

            foreach (var entry in MenuEntries)
            {
                var url = $"/{entry.Id}";

                if (entry.Id == "logout")
                {
                    TemplateEngine.Server.Get(url, RouteLogout);
                }
                else
                {
                    TemplateEngine.Server.Get(url, request => RouteMenuItems(request, url, entry.Id));
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
                PushError(request, "Error decoding key.");
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

            UpdateMenus(entry, url, request);

            var context = InitContext(request);

            context["active"] = entry;

            switch (entry)
            {
                case "history":
                    UpdateHistoryContext(context, request);
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
            var addressTo = request.GetVariable("dest");

            var chainName = request.GetVariable("chain");
            var destinationChain = request.GetVariable("destChain");

            var symbol = request.GetVariable("token");
            var amountOrId = request.GetVariable(isFungible ? "amount" : "id");

            var keyPair = GetLoginKey(request);
            SendRawTx result;

            if (chainName == destinationChain)
            {
                result = AccountController.TransferTokens(isFungible, keyPair, addressTo, chainName, symbol, amountOrId).Result;

                ResetSessionSendFields(request);
            }
            else //cross chain requires 2 txs
            {
                var pathList = AccountController.GetShortestPath(chainName, destinationChain).ToArray();

                request.session.SetInt("txNumber", pathList.Length);

                if (pathList.Length > 2)
                {
                    chainName = pathList[0].Name;
                    destinationChain = pathList[1].Name;

                    // save tx
                    request.session.SetStruct<TransferTx>("transferTx", new TransferTx
                    {
                        IsFungible = isFungible,
                        FromChain = chainName,
                        ToChain = destinationChain,
                        FinalChain = pathList[pathList.Length - 1].Name,
                        AddressTo = addressTo,
                        Symbol = symbol,
                        AmountOrId = amountOrId
                    });

                    result = AccountController.CrossChainTransferToken(isFungible, keyPair, keyPair.Address.Text, chainName, destinationChain, symbol, amountOrId).Result;
                }
                else
                {
                    result = AccountController.CrossChainTransferToken(isFungible, keyPair, addressTo, chainName, destinationChain, symbol, amountOrId).Result;
                }
                if (!result.HasError)
                {
                    request.session.SetBool("isCrossTransfer", true);
                    request.session.SetStruct<SettleTx>("settleTx",
                        new SettleTx
                        {
                            ChainName = chainName,
                            ChainAddress = AccountController.PhantasmaChains.Find(p => p.Name == chainName).Address,
                            DestinationChainAddress = AccountController.PhantasmaChains.Find(p => p.Name == destinationChain).Address,
                        });
                }
            }

            if (result.HasError)
            {
                PushError(request, result.Error);
                return "";
            }

            return result.Hash;
        }

        private object RouteWaitingTx(HTTPRequest request)
        {
            if (!HasLogin(request))
            {
                return HTTPResponse.Redirect("/login");
            }

            var context = InitContext(request);
            context["confirmingTxHash"] = request.GetVariable("txhash");
            context["transferTx"] = request.session.GetStruct<TransferTx>("transferTx");
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

            request.session.SetStruct<ErrorContext>("error", new ErrorContext { ErrorCode = "", ErrorDescription = $"{txHash} is still not confirmed." });
            var confirmationDto = AccountController.GetTxConfirmations(txHash).Result;

            if (confirmationDto.IsConfirmed)
            {
                request.session.SetString("confirmedHash", txHash);
                if (request.session.GetBool("isCrossTransfer"))
                {
                    var settle = request.session.GetStruct<SettleTx>("settleTx");

                    var settleTx = AccountController.SettleBlockTransfer(
                        GetLoginKey(request),
                        settle.ChainAddress,
                        confirmationDto.Hash, settle.DestinationChainAddress).Result;

                    // clear
                    request.session.SetBool("isCrossTransfer", false);

                    if (!settleTx.HasError)
                    {
                        context["confirmingTxHash"] = settleTx;
                        return "settling";
                    }
                    PushError(request, settleTx.Error);
                    return "unconfirmed";
                }
                else
                {
                    if (request.session.GetInt("txNumber") > 2)
                    {
                        return "continue";
                    }

                    //if it gets here, there are no more txs to process
                    var keyPair = GetLoginKey(request);
                    InvalidateCache(keyPair.Address);

                    ResetSessionSendFields(request);
                    return "confirmed";
                }
            }
            PushError(request, "Error sending tx.");
            return "unconfirmed";
        }

        private object RouteRegisterName(HTTPRequest request)
        {
            var name = request.GetVariable("name");
            var context = InitContext(request);
            if (AccountContract.ValidateAddressName(name))
            {
                if (context["holdings"] is Holding[] balance)
                {
                    var soulBalance = balance.SingleOrDefault(b => b.Symbol == "SOUL");
                    if (soulBalance.Amount > 0.1m) //RegistrationCost
                    {
                        var keyPair = GetLoginKey(request);
                        var registerTx = AccountController.RegisterName(keyPair, name).Result;
                        if (!registerTx.HasError)
                        {
                            return registerTx.Hash;
                        }

                        PushError(request, registerTx.Error);
                    }
                    else
                    {
                        PushError(request, "You need a small drop of SOUL (+0.1) to register a name.");
                    }
                }
            }
            else
            {
                PushError(request, "Error while registering name.");
            }
            return "";
        }

        #endregion

        private void ResetSessionSendFields(HTTPRequest request)
        {
            if (request.session.Contains("txNumber"))
            {
                request.session.Remove("txNumber");
            }

            if (request.session.Contains("transferTx"))
            {
                request.session.Remove("transferTx");
            }

            if (request.session.Contains("settleTx"))
            {
                request.session.Remove("settleTx");
            }

            if (request.session.Contains("isCrossTransfer"))
            {
                request.session.Remove("isCrossTransfer");
            }
        }

        private void UpdateMenus(string id, string url, HTTPRequest request)
        {
            request.session.SetString("active", url);
            foreach (var menuEntry in MenuEntries)
            {
                menuEntry.IsSelected = menuEntry.Id == id;
            }
            request.session.SetString("selectedMenu", MenuEntries.SingleOrDefault(m => m.IsSelected).Caption);
        }

        #region UI
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

        private static readonly Net[] Networks =
        {
            new Net{Name = "simnet", IsEnabled = true, Value = 1},
            new Net{Name = "testnet", IsEnabled = false, Value = 2},
            new Net{Name = "mainnet", IsEnabled = false, Value = 3},
        };
        #endregion
    }
}
