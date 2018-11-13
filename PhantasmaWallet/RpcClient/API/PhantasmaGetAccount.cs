using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;
using RpcRequest = Phantasma.Wallet.JsonRpc.Client.RpcRequest;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetAccount : JsonRpc.Client.RpcRequestResponseHandler<Account>
    {
        public PhantasmaGetAccount(IClient client) : base(client, APIMethods.getaccount.ToString()) { }

        public Task<Account> SendRequestAsync(string address, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            return SendRequestAsync(id, address);
        }

        public RpcRequest BuildRequest(string address, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            return BuildRequest(id, address);
        }
    }
}
