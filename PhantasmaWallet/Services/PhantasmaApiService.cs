using System;
using System.Net.Http;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;

namespace Phantasma.Wallet.Services
{
    public class PhantasmaApiService //todo interface
    {
        private readonly HttpClient _restClient;

        public PhantasmaApiService()
        {
            _restClient = new HttpClient() { BaseAddress = new Uri("http://localhost:49153/api/") };
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
