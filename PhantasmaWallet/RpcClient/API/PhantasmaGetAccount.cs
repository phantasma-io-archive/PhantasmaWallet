using System;
using System.Threading.Tasks;
using NeoModules.JsonRpc.Client;
using Phantasma.Wallet.DTOs;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetAccount : RpcRequestResponseHandler<Account>
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
