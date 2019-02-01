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
            _restClient = new HttpClient() { BaseAddress = new Uri("http://localhost:7072/api/") }; //todo disabled atm
        }

        public async Task<AccountDto> GetAccount(string address)
        {
            try
            {
                HttpResponseMessage responseMessage = await _restClient.GetAsync($"get_account/{address}");
                responseMessage.EnsureSuccessStatusCode();
                var data = await responseMessage.Content.ReadAsStringAsync();
                return AccountDto.FromJson(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new AccountDto(); //todo
            }
        }

        public async Task<BlockDto> GetBlock(string blockHash)
        {
            try
            {
                HttpResponseMessage responseMessage = await _restClient.GetAsync($"get_block/{blockHash}");
                responseMessage.EnsureSuccessStatusCode();
                var data = await responseMessage.Content.ReadAsStringAsync();
                return BlockDto.FromJson(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new BlockDto(); //todo
            }
        }

        public async Task<AccountTransactionsDto> GetAccountTxs(string address, int amount)
        {
            try
            {
                HttpResponseMessage responseMessage = await _restClient.GetAsync($"get_account_txs/{address}/{amount}");
                responseMessage.EnsureSuccessStatusCode();
                var data = await responseMessage.Content.ReadAsStringAsync();
                return AccountTransactionsDto.FromJson(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return new AccountTransactionsDto(); //todo
            }
        }
    }
}
