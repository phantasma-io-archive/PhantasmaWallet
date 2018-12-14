using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Tokens;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.Interfaces;
using Phantasma.Numerics;
using Phantasma.Wallet.Helpers;
using Transaction = Phantasma.Wallet.DTOs.Transaction;
using Token = Phantasma.Wallet.DTOs.Token;

namespace Phantasma.Wallet.Controllers
{
    public class AccountController
    {
        private readonly IPhantasmaRestService _phantasmaApi;
        private readonly IPhantasmaRpcService _phantasmaRpcService;

        private List<Token> AccountHoldings { get; set; }

        public string AccountName { get; set; }

        public AccountController()
        {
            _phantasmaApi = (IPhantasmaRestService)Backend.AppServices.GetService(typeof(IPhantasmaRestService));
            _phantasmaRpcService = (IPhantasmaRpcService)Backend.AppServices.GetService(typeof(IPhantasmaRpcService));
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
                            Amount = TokenUtils.ToDecimal(BigInteger.Parse(balanceChain.Balance), holding.Decimals),
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
            var account = await _phantasmaRpcService.GetAccount.SendRequestAsync(address);
            AccountName = account.Name;
            var rateUsd = Utils.GetCoinRate(2827);
            foreach (var token in account.Tokens)
            {
                var holding = new Holding
                {
                    Symbol = token.Symbol,
                    Icon = "phantasma_logo",
                    Name = token.Name,
                    Rate = rateUsd
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

                holding.Amount = amount;
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
                    Date = new Timestamp(tx.Timestamp),
                    Hash = tx.Txid,
                    Description = Utils.GetTxDescription(tx, PhantasmaChains, PhantasmaTokens)
                });
            }

            return txs.ToArray();
        }

        public async Task<SendRawTx> SettleBlockTransfer(KeyPair keyPair, string sourceChainAddress, string blockHash,
            string destinationChainAddress)
        {
            try
            {
                var sourceChain = Address.FromText(sourceChainAddress);
                var destinationChainName =
                    PhantasmaChains.SingleOrDefault(c => c.Address == destinationChainAddress).Name;
                var nexusName = "simnet";

                var block = Hash.Parse(blockHash);

                var settleTxScript = ScriptUtils.BeginScript()
                    .CallContract("token", "SettleBlock", sourceChain, block)
                    .AllowGas(keyPair.Address, 1, 9999)
                    .SpendGas(keyPair.Address)
                    .EndScript();

                var settleTx = new Blockchain.Transaction(nexusName, destinationChainName, settleTxScript, DateTime.UtcNow + TimeSpan.FromHours(1), 0);
                settleTx.Sign(keyPair);

                var settleResult =
                    await _phantasmaRpcService.SendRawTx.SendRequestAsync(settleTx.ToByteArray(true).Encode());
                return settleResult;
            }
            catch (Exception)
            {
                return new SendRawTx { Error = "Something bad happened when trying to settle transaction." };
            }
        }

        public async Task<SendRawTx> CrossChainTransferToken(bool isFungible, KeyPair keyPair, string addressTo,
            string chainName, string destinationChain, string symbol, string amountId)
        {
            try
            {
                var toChain = PhantasmaChains.Find(p => p.Name == destinationChain);
                var destinationAddress = Address.FromText(addressTo);
                int decimals = AccountHoldings.SingleOrDefault(t => t.Symbol == symbol).Decimals;
                var bigIntAmount = TokenUtils.ToBigInteger(decimal.Parse(amountId), decimals);
                var fee = TokenUtils.ToBigInteger(0.0001m, 8);

                var script = isFungible
                    ? ScriptUtils.BeginScript()
                        .AllowGas(keyPair.Address, 1, 9999)
                        .CrossTransferToken(Address.FromText(toChain.Address), symbol, keyPair.Address,
                            keyPair.Address, fee)
                        .CrossTransferToken(Address.FromText(toChain.Address), symbol, keyPair.Address,
                            destinationAddress, bigIntAmount)
                        .SpendGas(keyPair.Address)
                        .EndScript()

                    : ScriptUtils.BeginScript()
                        .AllowGas(keyPair.Address, 1, 9999)
                        .CrossTransferNFT(Address.FromText(toChain.Address), symbol, keyPair.Address,
                            destinationAddress, bigIntAmount)
                        .SpendGas(keyPair.Address)
                        .EndScript();

                var nexusName = "simnet";

                var tx = new Blockchain.Transaction(nexusName, chainName, script, DateTime.UtcNow + TimeSpan.FromHours(1), 0);
                tx.Sign(keyPair);

                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync(tx.ToByteArray(true).Encode());
                return txResult;
            }
            catch (Exception)
            {
                return new SendRawTx { Error = "Something bad happened when trying to send tx." };
            }
        }

        public async Task<SendRawTx> TransferTokens(bool isFungible, KeyPair keyPair, string addressTo, string chainName, string symbol, string amountId)
        {
            try
            {
                var destinationAddress = Address.FromText(addressTo);
                int decimals = AccountHoldings.SingleOrDefault(t => t.Symbol == symbol).Decimals;
                var bigIntAmount = TokenUtils.ToBigInteger(decimal.Parse(amountId), decimals);

                var script = isFungible
                    ? ScriptUtils.BeginScript()
                        .AllowGas(keyPair.Address, 1, 9999)
                        .TransferTokens(symbol, keyPair.Address, destinationAddress, bigIntAmount)
                        .SpendGas(keyPair.Address)
                        .EndScript()
                    : ScriptUtils.BeginScript()
                        .AllowGas(keyPair.Address, 1, 9999)
                        .TransferNFT(symbol, keyPair.Address, destinationAddress, bigIntAmount)
                        .SpendGas(keyPair.Address)
                        .EndScript();

                // TODO this should be a dropdown in the wallet settings!!
                var nexusName = "simnet";

                var tx = new Blockchain.Transaction(nexusName, chainName, script, DateTime.UtcNow + TimeSpan.FromHours(1), 0);
                tx.Sign(keyPair);

                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync(tx.ToByteArray(true).Encode());
                return txResult;
            }
            catch (Exception)
            {
                return new SendRawTx { Error = "Something bad happened when trying to send tx." };
            }
        }

        public async Task<TxConfirmation> GetTxConfirmations(string txHash)
        {
            try
            {
                var txConfirmation = await _phantasmaRpcService.GetTransactionConfirmations.SendRequestAsync(txHash);
                return txConfirmation;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<SendRawTx> RegisterName(KeyPair keyPair, string name)
        {
            try
            {
                var script = ScriptUtils.BeginScript()
                       .AllowGas(keyPair.Address, 1, 9999)
                       .CallContract("account", "Register", keyPair.Address, name)
                       .SpendGas(keyPair.Address)
                       .EndScript();

                // TODO this should be a dropdown in the wallet settings!!
                var nexusName = "simnet";
                var tx = new Blockchain.Transaction(nexusName, "main", script, DateTime.UtcNow + TimeSpan.FromHours(1), 0);

                tx.Sign(keyPair);

                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync(tx.ToByteArray(true).Encode());
                return txResult;
            }
            catch (Exception)
            {
                return new SendRawTx { Error = "Something bad happened when trying to send tx." };
            }
        }

        public List<ChainElement> GetShortestPath(string chainName, string destinationChain)
        {
            return SendUtils.GetShortestPath(chainName, destinationChain, PhantasmaChains);
        }

        #region Public Lists
        public List<ChainElement> PhantasmaChains
        {
            get
            {
                if (_phantasmaChains != null && _phantasmaChains.Any())
                {
                    return _phantasmaChains;
                }

                _phantasmaChains = GetPhantasmaChains();
                return _phantasmaChains;
            }
        }

        private List<ChainElement> _phantasmaChains;

        public List<Token> PhantasmaTokens
        {
            get
            {
                if (_phantasmaTokens != null && _phantasmaTokens.Any())
                {
                    return _phantasmaTokens;
                }

                _phantasmaTokens = GetPhantasmaTokens();
                return _phantasmaTokens;
            }
        }

        private List<Token> _phantasmaTokens;

        private List<ChainElement> GetPhantasmaChains()
        {
            List<ChainElement> chains = null;
            try
            {
                chains = _phantasmaRpcService.GetChains.SendRequestAsync().Result.ChainList;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return chains;
        }

        private List<Token> GetPhantasmaTokens()
        {
            List<Token> tokens = null;
            try
            {
                tokens = _phantasmaRpcService.GetTokens.SendRequestAsync().Result.Tokens;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return tokens;
        }
        #endregion
    }
}
