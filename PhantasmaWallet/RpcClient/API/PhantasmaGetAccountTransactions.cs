using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;
using RpcRequest = Phantasma.Wallet.JsonRpc.Client.RpcRequest;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetAccountTransactions : JsonRpc.Client.RpcRequestResponseHandler<AccountTransactions>
    {
        public PhantasmaGetAccountTransactions(IClient client) : base(client, APIMethods.getaddresstransactions.ToString()) { }

        public Task<AccountTransactions> SendRequestAsync(string address, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            return SendRequestAsync(id, address, 10);//todo
        }

        public RpcRequest BuildRequest(string address, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            return BuildRequest(id, address, 10);
        }
    }
}
