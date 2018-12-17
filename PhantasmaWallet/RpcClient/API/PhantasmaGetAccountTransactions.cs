using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetAccountTransactions : RpcRequestResponseHandler<AccountTransactions>
    {
        public PhantasmaGetAccountTransactions(IClient client) : base(client, ApiMethods.getAddressTransactions.ToString()) { }

        public Task<AccountTransactions> SendRequestAsync(string address, int amount, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return SendRequestAsync(id, address, amount);
        }

        public RpcRequest BuildRequest(string address, int amount, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

            return BuildRequest(id, address, 10);
        }
    }
}
