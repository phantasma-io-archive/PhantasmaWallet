using System;
using System.Collections.Generic;
using System.Linq;
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

        private List<Token> AccountHoldings { get; set; }

        public List<ChainElement> PhantasmaChains { get; set; }

        public List<Token> PhantasmaTokens { get; set; }

        public string AccountName { get; set; }

        public AccountController()
        {
            _phantasmaApi = (IPhantasmaRestService)Backend.AppServices.GetService(typeof(IPhantasmaRestService));
            _phantasmaRpcService = (IPhantasmaRpcService)Backend.AppServices.GetService(typeof(IPhantasmaRpcService));
        }

        public void InitController()
        {
            try
            {
                PhantasmaChains = _phantasmaRpcService.GetChains.SendRequestAsync().Result.ChainList;
                PhantasmaTokens = _phantasmaRpcService.GetTokens.SendRequestAsync().Result.Tokens;
            }
            catch (Exception ex)
            {
                //todo
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
            AccountName = account.Name;
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
                        decimal chainAmount = TokenUtils.ToDecimal(balance, token.Decimals);
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
                    description = GetTxDescription(tx)
                });
            }

            return txs.ToArray();
        }

        private string GetTxDescription(AccountTx tx)
        {
            string description = null;

            string senderToken = null;
            Address senderChain = Address.FromText(tx.ChainAddress);
            Address senderAddress = Address.Null;

            string receiverToken = null;
            Address receiverChain = Address.Null;
            Address receiverAddress = Address.Null;

            BigInteger amount = 0;

            foreach (var evt in tx.Events)//todo move this
            {
                Blockchain.Contracts.Event nativeEvent = null;
                if (evt.Data != null)
                {
                    nativeEvent = new Blockchain.Contracts.Event((EventKind)evt.EvtKind, Address.FromText(evt.EventAddress), evt.Data.Decode());
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
                    var amountDecimal = TokenUtils.ToDecimal(amount, PhantasmaTokens.SingleOrDefault(p => p.Symbol == senderToken).Decimals);
                    description = $"{amountDecimal} {senderToken} sent from {senderAddress.Text} to {receiverAddress.Text}";
                }
                else if (amount > 0 && senderAddress != Address.Null && receiverAddress != Address.Null && senderToken != null && receiverToken != null)
                {
                    var amountDecimal = TokenUtils.ToDecimal(amount, PhantasmaTokens.SingleOrDefault(p => p.Symbol == receiverToken).Decimals);
                    description = $"{amountDecimal} {receiverToken} sent from {senderAddress.Text} to {receiverAddress.Text}";
                }
                else
                {
                    description = "Custom transaction";
                }

                if (receiverChain != Address.Null && senderChain != Address.Null && receiverChain != senderChain)
                {
                    description += $" from {GetChainName(senderChain.Text)} chain to {GetChainName(receiverChain.Text)} chain";
                }
            }
            return description;
        }

        public async Task<string> TransferTokens(bool isFungible, KeyPair keyPair, string addressTo, string chainName, string chainAddress, string destinationChainAddress, string symbol, string amountId)
        {
            try
            {
                var chain = Address.FromText(chainAddress);
                var destinationChain = Address.FromText(destinationChainAddress);
                var destinationAddress = Address.FromText(addressTo);
                byte[] script;
                int decimals = AccountHoldings.SingleOrDefault(t => t.Symbol == symbol).Decimals;
                var bigIntAmount = TokenUtils.ToBigInteger(decimal.Parse(amountId), decimals);

                if (chain.Equals(destinationChain)) //same chain transfer
                {
                    script = isFungible ? ScriptUtils.TokenTransferScript(chain, symbol, keyPair.Address, destinationAddress, bigIntAmount) : ScriptUtils.NfTokenTransferScript(chain, symbol, keyPair.Address, destinationAddress, bigIntAmount);
                }
                else // cross-chain transfer
                {
                    script = isFungible
                        ? ScriptUtils.CrossTokenTransferScript(chain, destinationChain, symbol, keyPair.Address,
                            destinationAddress, bigIntAmount)
                        : ScriptUtils.CrossNfTokenTransferScript(chain, destinationChain, symbol, keyPair.Address,
                            destinationAddress, bigIntAmount);
                }

                // TODO this should be a dropdown in the wallet settings!!
                var nexusName = "simnet";

                var tx = new Blockchain.Transaction(nexusName, chainName, script, 0, 0, DateTime.UtcNow + TimeSpan.FromHours(1), 0);
                tx.Sign(keyPair);

                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync(tx.ToByteArray(true).Encode());
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

        private string GetChainName(string address)
        {
            foreach (var element in PhantasmaChains)
            {
                if (element.Address == address) return element.Name;
            }
            return string.Empty;
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
