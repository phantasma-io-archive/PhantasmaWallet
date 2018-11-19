using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        //todo move this
        public KeyPair SessionKeyPair;
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

        public async Task<Holding[]> GetAccountHoldings(string address = null)
        {
            var holdings = new List<Holding>();
            if (address == null) address = SessionKeyPair.Address.Text;
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

        public async Task<List<Token>> GetAccountTokens(string address = null)
        {
            if (address == null) address = SessionKeyPair.Address.Text;
            var account = await _phantasmaApi.GetAccount(address);
            return account.Tokens;
        }

        public async Task<Transaction[]> GetAccountTransactions(string address = null, int amount = 20)
        {
            var txs = new List<Transaction>();
            if (address == null) address = SessionKeyPair.Address.Text;
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


        public async Task<string> SendRawTx(string addressTo, string chainAddress, string symbol, string amount)
        {
            try
            {
                var chain = Address.FromText(chainAddress);
                var dest = Address.FromText(addressTo);
                var bigIntAmount = TokenUtils.ToBigInteger(decimal.Parse(amount), 8);

                var script = ScriptUtils.CallContractScript(chain, "TransferTokens", SessionKeyPair.Address, dest, symbol, bigIntAmount);//todo this should be TokenTransferScript

                // TODO this should be a dropdown in the wallet settings!!
                var nexusName = "simnet";

                var tx = new Blockchain.Transaction(nexusName, script, 0, 0, DateTime.UtcNow, 0);
                tx.Sign(SessionKeyPair);

                //todo main
                var txResult = await _phantasmaRpcService.SendRawTx.SendRequestAsync("main", tx.ToByteArray(true).Encode());
                var txHash = txResult?.GetValue("hash");
                return txHash?.ToString();
            }
            catch (Exception ex)
            {
                return "";
            }
        }
    }
}
