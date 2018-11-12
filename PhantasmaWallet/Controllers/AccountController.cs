using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Phantasma.Core.Types;
using Phantasma.Wallet.Services;

namespace Phantasma.Wallet.Controllers
{
    public class AccountController
    {
        private readonly PhantasmaApiService _phantasmaApi;
        public AccountController()
        {
            _phantasmaApi = new PhantasmaApiService();
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
                    description = tx.ChainName,
                });
            }

            return txs.ToArray();
        }
    }
}
