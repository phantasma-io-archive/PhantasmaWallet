using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetAccountTransactions : RpcRequestResponseHandler<AccountTransactions>
    {
        public PhantasmaGetAccountTransactions(IClient client) : base(client, ApiMethods.getAddressTransactions.ToString()) { }

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
