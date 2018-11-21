using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LunarLabs.Parser.JSON;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.Interfaces;
using Phantasma.Numerics;
using Event = Phantasma.Wallet.DTOs.Event;

namespace Phantasma.Wallet.Controllers
{
    public class AccountController
    {
        private readonly IPhantasmaRestService _phantasmaApi;
        private readonly IPhantasmaRpcService _phantasmaRpcService;

        public List<Token> AccountHoldings { get; set; }

        public AccountController()
        {
            _phantasmaApi = (IPhantasmaRestService)Backend.AppServices.GetService(typeof(IPhantasmaRestService));
            _phantasmaRpcService = (IPhantasmaRpcService)Backend.AppServices.GetService(typeof(IPhantasmaRpcService));
        }

        public async Task<Chains> GetChains()
        {
            try
            {
                return await _phantasmaRpcService.GetChains.SendRequestAsync();
            }
            catch (Exception ex)
            {
                return new Chains();
            }
        }

        public List<SendHolding> PrepareSendHoldings()
        {
            var holdingList = new List<SendHolding>();
            if (AccountHoldings.Count == 0) return holdingList;

            foreach (var holding in AccountHoldings)
            {
                foreach (var balanceChain in holding.Chains)
                {
                    if (decimal.Parse(balanceChain.Balance) > 0)
                    {
                        holdingList.Add(new SendHolding
                        {
                            Amount = decimal.Parse(balanceChain.Balance),
                            ChainName = balanceChain.ChainName,
                            Name = holding.Name,
                            Symbol = holding.Symbol,
                            Icon = "phantasma_logo",
                            Fungible = holding.Fungible,
                            Ids = balanceChain.Ids
                        });
                    }
                }
            }
            return holdingList;
        }

        public async Task<Holding[]> GetAccountHoldings(string address)
        {
            var holdings = new List<Holding>();
            var account = await _phantasmaApi.GetAccount(address);
            var rateUsd = GetCoinRate(2827);
            foreach (var token in account.Tokens)
            {
                var holding = new Holding
                {
                    symbol = token.Symbol,
                    icon = "phantasma_logo",
                    name = token.Name,
                    rate = rateUsd
                };
                decimal amount = 0;
                foreach (var tokenChain in token.Chains)
                {
                    if (BigInteger.TryParse(tokenChain.Balance, out var balance))
                    {
                        decimal chainAmount = TokenUtils.ToDecimal(balance, token.Decimals); // TODO fix this later, should use token.Decimals
                        amount += chainAmount;
                    }
                }

                holding.amount = amount;
                holdings.Add(holding);
            }

            AccountHoldings = account.Tokens;
            return holdings.ToArray();
        }

        public async Task<List<Token>> GetAccountTokens(string address)
        {
            var account = await _phantasmaApi.GetAccount(address);
            return account.Tokens;
        }

        public async Task<Transaction[]> GetAccountTransactions(string address, int amount = 20)
        {
            var txs = new List<Transaction>();
            var accountTxs = await _phantasmaApi.GetAccountTxs(address, amount);
            foreach (var tx in accountTxs.Txs)
            {
                txs.Add(new Transaction
                {
                    date = new Timestamp(tx.Timestamp),
                    hash = tx.Txid,
                    description = GetTxDescription(tx.Events)
                });
            }

            return txs.ToArray();
        }

        private string GetTxDescription(List<Event> events)
        {
            string description = null;

            string senderToken = null;
            Address senderChain = Address.Null;
            Address senderAddress = Address.Null;

            string receiverToken = null;
            Address receiverChain = Address.Null;
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            foreach (var evt in events)//todo move this
            {
                Blockchain.Contracts.Event nativeEvent = null;
                if (evt.Data != null)
                {
                    nativeEvent = new Blockchain.Contracts.Event((EventKind)evt.EvtKind, Address.FromText(evt.EventAddress), Base16.Decode(evt.Data));
                }
                else
                {
                    nativeEvent = new Blockchain.Contracts.Event((EventKind)evt.EvtKind, Address.FromText(evt.EventAddress));
                }

                switch (evt.EvtKind)
                {
                    case EvtKind.TokenSend:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            senderAddress = nativeEvent.Address;
                            senderChain = data.chainAddress;
                            senderToken = (data.symbol);
                        }
                        break;

                    case EvtKind.TokenReceive:
                        {
                            var data = nativeEvent.GetContent<TokenEventData>();
                            amount = data.value;
                            receiverAddress = nativeEvent.Address;
                            receiverChain = data.chainAddress;
                            receiverToken = data.symbol;
                        }
                        break;

                    case EvtKind.AddressRegister:
                        {
                            var name = nativeEvent.GetContent<string>();
                            description = $"{nativeEvent.Address} registered the name '{name}'";
                        }
                        break;

                    case EvtKind.FriendAdd:
                        {
                            var address = nativeEvent.GetContent<Address>();
                            description = $"{nativeEvent.Address} added '{address} to friends.'";
                        }
                        break;

                    case EvtKind.FriendRemove:
                        {
                            var address = nativeEvent.GetContent<Address>();
                            description = $"{nativeEvent.Address} removed '{address} from friends.'";
                        }
                        break;
                }
            }

            if (description == null)
            {
                if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null && senderToken != null && senderToken == receiverToken)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount, 8);
                    description = $"{amountDecimal} {senderToken} sent from {senderAddress.Text} to {receiverAddress.Text}";
                }
                else
                {
                    description = "Custom transaction";
                }
            }
            return description;
        }


        public async Task<string> SendRawTx(KeyPair keyPair, string addressTo, string chainName, string chainAddress, string symbol, string amount)
        {
            try
            {
                var chain = Address.FromText(chainAddress);
                var dest = Address.FromText(addressTo);
                var bigIntAmount = TokenUtils.ToBigInteger(decimal.Parse(amount), 8);

                var script = ScriptUtils.CallContractScript(chain, "TransferTokens", keyPair.Address, dest, symbol, bigIntAmount);//todo this should be TokenTransferScript

                // TODO this should be a dropdown in the wallet settings!!
                var nexusName = "simnet";

                var tx = new Blockchain.Transaction(nexusName, script, 0, 0, DateTime.UtcNow, 0);
                tx.Sign(keyPair);

                //todo main
                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync(chainName.ToLowerInvariant(), tx.ToByteArray(true).Encode());
                var txHash = txResult?.GetValue("hash");
                return txHash?.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public async Task<TxConfirmation> GetTxConfirmations(string txHash)
        {
            try
            {
                var txConfirmation = await _phantasmaRpcService.GetTxConfirmations.SendRequestAsync(txHash);
                return txConfirmation;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public static decimal GetCoinRate(uint ticker, string symbol = "USD")
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert={symbol}";

            string json;

            try
            {
                using (var wc = new WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];
                var quotes = root["quotes"][symbol];

                var price = quotes.GetDecimal("price");

                return price;
            }
            catch
            {
                return 0;
            }
        }
    }
}
