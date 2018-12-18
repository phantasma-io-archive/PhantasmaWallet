using System;
using System.Net.Http;
using System.Threading.Tasks;
using Phantasma.RpcClient.DTOs;
using Phantasma.Wallet.Interfaces;

namespace Phantasma.Wallet.Services
{
    public class PhantasmaRestService : IPhantasmaRestService
    {
        private readonly HttpClient _restClient;

        public PhantasmaRestService()
        {
            _restClient = new HttpClient() { BaseAddress = new Uri("http://localhost:7074/api/") };
        }

        public async Task<Account> GetAccount(string address)
        {
            try
            {
                HttpResponseMessage responseMessage = await _restClient.GetAsync($"get_account/{address}");
                responseMessage.EnsureSuccessStatusCode();
                var data = await responseMessage.Content.ReadAsStringAsync();
                return Account.FromJson(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new Account(); //todo
            }
        }

        public async Task<Block> GetBlock(string blockHash)
        {
            try
            {
                HttpResponseMessage responseMessage = await _restClient.GetAsync($"get_block/{blockHash}");
                responseMessage.EnsureSuccessStatusCode();
                var data = await responseMessage.Content.ReadAsStringAsync();
                return Block.FromJson(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new Block(); //todo
            }
        }

        public async Task<AccountTransactions> GetAccountTxs(string address, int amount)
        {
            try
            {
                HttpResponseMessage responseMessage = await _restClient.GetAsync($"get_account_txs/{address}/{amount}");
                responseMessage.EnsureSuccessStatusCode();
                var data = await responseMessage.Content.ReadAsStringAsync();
                return AccountTransactions.FromJson(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new AccountTransactions(); //todo
            }
        }
    }
}
