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
using Phantasma.RpcClient.Client;
using Phantasma.RpcClient.DTOs;
using Phantasma.RpcClient.Interfaces;
using Phantasma.Wallet.Helpers;
using Transaction = Phantasma.Wallet.DTOs.Transaction;

namespace Phantasma.Wallet.Controllers
{
    public class AccountController
    {
        private readonly IPhantasmaRestService _phantasmaApi;
        private readonly IPhantasmaRpcService _phantasmaRpcService;

        private List<BalanceSheetDto> AccountHoldings { get; set; }

        public string AccountName { get; set; }

        public AccountController()
        {
            _phantasmaApi = (IPhantasmaRestService)Backend.AppServices.GetService(typeof(IPhantasmaRestService));
            _phantasmaRpcService = (IPhantasmaRpcService)Backend.AppServices.GetService(typeof(IPhantasmaRpcService));
        }

        public List<SendHolding> PrepareSendHoldings()
        {
            var holdingList = new List<SendHolding>();
            if (AccountHoldings == null || AccountHoldings.Count == 0) return holdingList;

            foreach (var holding in AccountHoldings)
            {
                if (decimal.Parse(holding.Amount) > 0)
                {
                    var token = GetToken(holding.Symbol);
                    holdingList.Add(new SendHolding
                    {
                        Amount = TokenUtils.ToDecimal(BigInteger.Parse(holding.Amount), token.Decimals),
                        ChainName = holding.ChainName,
                        Name = token.Name,
                        Symbol = holding.Symbol,
                        Icon = "phantasma_logo",
                        Fungible = token.Fungible,
                        Ids = holding.Ids
                    });
                }
            }

            return holdingList;
        }

        public async Task<Holding[]> GetAccountHoldings(string address)
        {
            try
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
                        Chain = token.ChainName,
                        Name = GetToken(token.Symbol).Name,
                        Rate = rateUsd
                    };

                    decimal amount = 0;
                    if (BigInteger.TryParse(token.Amount, out var balance))
                    {
                        var decimals = GetToken(token.Symbol).Decimals;
                        decimal chainAmount = TokenUtils.ToDecimal(balance, decimals);
                        amount += chainAmount;
                    }
                    holding.Amount = amount;
                    holdings.Add(holding);
                }

                AccountHoldings = account.Tokens;
                return holdings.ToArray();
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
            }

            return new Holding[0];
        }

        public async Task<List<BalanceSheetDto>> GetAccountTokens(string address)
        {
            try
            {
                var account = await _phantasmaRpcService.GetAccount.SendRequestAsync(address);
                return account.Tokens;
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
            }

            return new List<BalanceSheetDto>();
        }

        public async Task<Transaction[]> GetAccountTransactions(string address, int amount = 20)
        {
            try
            {
                var txs = new List<Transaction>();
                var accountTxs = await _phantasmaRpcService.GetAddressTxs.SendRequestAsync(address, amount);
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
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
            }

            return new Transaction[0];
        }

        public async Task<SendRawTxDto> SettleBlockTransfer(KeyPair keyPair, string sourceChainAddress, string blockHash,
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
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
                return new SendRawTxDto { Error = rpcEx.RpcError.Message };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
                return new SendRawTxDto { Error = "Something bad happened when trying to settle transaction." };
            }
        }

        public async Task<SendRawTxDto> CrossChainTransferToken(bool isFungible, KeyPair keyPair, string addressTo,
            string chainName, string destinationChain, string symbol, string amountId)
        {
            try
            {
                var toChain = PhantasmaChains.Find(p => p.Name == destinationChain);
                var destinationAddress = Address.FromText(addressTo);
                int decimals = GetToken(symbol).Decimals;
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
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
                return new SendRawTxDto { Error = rpcEx.RpcError.Message };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
                return new SendRawTxDto { Error = "Something bad happened when trying to send tx." };
            }
        }

        public async Task<SendRawTxDto> TransferTokens(bool isFungible, KeyPair keyPair, string addressTo, string chainName, string symbol, string amountId)
        {
            try
            {
                var destinationAddress = Address.FromText(addressTo);
                int decimals = GetToken(symbol).Decimals;
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

                var tx = new Blockchain.Transaction(nexusName, chainName, script,
                    DateTime.UtcNow + TimeSpan.FromHours(1), 0);
                tx.Sign(keyPair);

                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync(tx.ToByteArray(true).Encode());
                return txResult;
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
                return new SendRawTxDto { Error = rpcEx.RpcError.Message };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
                return new SendRawTxDto { Error = "Something bad happened when trying to send tx." };
            }
        }

        public async Task<TxConfirmationDto> GetTxConfirmations(string txHash)
        {
            try
            {
                var txConfirmation = await _phantasmaRpcService.GetTxConfirmations.SendRequestAsync(txHash);
                return txConfirmation;
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
            }

            return new TxConfirmationDto { Confirmations = 0 };
        }

        public async Task<SendRawTxDto> RegisterName(KeyPair keyPair, string name)
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
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
                return new SendRawTxDto { Error = rpcEx.RpcError.Message };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
                return new SendRawTxDto { Error = "Something bad happened when trying to send tx." };
            }
        }

        public List<ChainDto> GetShortestPath(string chainName, string destinationChain)
        {
            return SendUtils.GetShortestPath(chainName, destinationChain, PhantasmaChains);
        }


        private TokenDto GetToken(string symbol)
        {
            return PhantasmaTokens.SingleOrDefault(p => p.Symbol == symbol);
        }

        //private void CreateAccountHoldings(List<BalanceSheetDto> balances)
        //{
        //    List<TokenDto> tokens = new List<TokenDto>();
        //    foreach (var balanceSheetDto in balances)
        //    {
        //        var token = GetToken(balanceSheetDto.Symbol);
        //        token.Chains = balances;
        //        tokens.Add();
        //    }
        //}

        #region Public Lists
        public List<ChainDto> PhantasmaChains
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

        private List<ChainDto> _phantasmaChains;

        public List<TokenDto> PhantasmaTokens
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

        private List<TokenDto> _phantasmaTokens;

        private List<ChainDto> GetPhantasmaChains()
        {
            List<ChainDto> chains = null;
            try
            {
                chains = _phantasmaRpcService.GetChains.SendRequestAsync().Result.ToList();
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
            }
            return chains;
        }

        private List<TokenDto> GetPhantasmaTokens()
        {
            List<TokenDto> tokens = null;
            try
            {
                tokens = _phantasmaRpcService.GetTokens.SendRequestAsync().Result.Tokens;
            }
            catch (RpcResponseException rpcEx)
            {
                Debug.WriteLine($"RPC Exception occurred: {rpcEx.RpcError.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception occurred: {ex.Message}");
            }

            return tokens;
        }
        #endregion
    }
}
