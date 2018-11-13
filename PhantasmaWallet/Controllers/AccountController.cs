using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phantasma.Blockchain;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Numerics;
using Phantasma.VM.Utils;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.Interfaces;
using Chain = Phantasma.Blockchain.Chain;
using Token = Phantasma.Blockchain.Tokens.Token;

namespace Phantasma.Wallet.Controllers
{
    public class AccountController
    {
        private readonly IPhantasmaRestService _phantasmaApi;
        private readonly IPhantasmaRpcService _phantasmaRpcService;

        //todo move this
        public KeyPair SessionKeyPair;
        public AccountController()
        {
            _phantasmaApi = (IPhantasmaRestService)Backend.AppServices.GetService(typeof(IPhantasmaRestService));
            _phantasmaRpcService = (IPhantasmaRpcService)Backend.AppServices.GetService(typeof(IPhantasmaRpcService));
        }


        public async Task<Chains> GetChains()
        {
            var x = await _phantasmaRpcService.GetChains.SendRequestAsync();
            return Chains.FromJson(x);
        }

        public async Task<Holding[]> GetAccountInfo(string address)
        {
            var holdings = new List<Holding>();
            var account = await _phantasmaApi.GetAccount(address);

            foreach (var token in account.Tokens)
            {
                var holding = new Holding
                {
                    symbol = token.Symbol,
                    icon = "phantasma_logo",
                    name = token.Name,
                    rate = 0.1m
                };
                decimal amount = 0;
                foreach (var tokenChain in token.Chains)
                {
                    if (decimal.TryParse(tokenChain.Balance, out var chainAmount))
                    {
                        amount += chainAmount;
                    }
                }

                holding.amount = amount;
                holdings.Add(holding);
            }

            return holdings.ToArray();
        }

        public async Task<Transaction[]> GetAccountTransactions(string address, int amount)
        {
            var txs = new List<Transaction>();
            var accountTxs = await _phantasmaApi.GetAccountTxs(address, amount);
            foreach (var tx in accountTxs.Txs)
            {
                txs.Add(new Transaction
                {
                    date = new Timestamp(tx.Timestamp),
                    hash = tx.Txid,
                    description = ""//todo
                });
            }

            return txs.ToArray();
        }

        private string GetTxDescription(AccountTx accountTx)
        {
            string description = null;

            //Token senderToken = null;
            //Address senderChain = Address.Null;
            //Address senderAddress = Address.Null;

            //Token receiverToken = null;
            //Address receiverChain = Address.Null;
            //Address receiverAddress = Address.Null;

            //BigInteger amount = 0;


            foreach (var evt in accountTx.Events)//todo move this
            {
                switch (evt.EvtKind)
                {
                    case EvtKind.TokenSend:
                        {

                        }
                        break;

                    case EvtKind.TokenReceive:
                        {

                        }
                        break;

                    case EvtKind.AddressRegister:
                        {

                        }
                        break;

                    case EvtKind.FriendAdd:
                        {

                        }
                        break;

                    case EvtKind.FriendRemove:
                        {

                        }
                        break;
                }
            }
            return string.Empty;
        }

        private async Task<string> SendRawTx(string addressTo, string chainAddress, string symbol, decimal amount)
        {
            var chain = Address.FromText(chainAddress);
            var dest = Address.FromText(addressTo);
            var bigIntAmount = TokenUtils.ToBigInteger(amount, 8);
            var script = ScriptUtils.CallContractScript(chain, "TransferTokens", SessionKeyPair.Address, dest, symbol, bigIntAmount);

            var tx = new Blockchain.Transaction(script, 0, 0, DateTime.UtcNow, 0);
            tx.Sign(SessionKeyPair);

            //todo main
            return await _phantasmaRpcService.SendRawTx.SendRequestAsync("main", tx.ToByteArray(true).Base58CheckEncode());
        }
    }
}
