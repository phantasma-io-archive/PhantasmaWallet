using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetTransactionByHash : RpcRequestResponseHandler<AccountTx>
    {
        public PhantasmaGetTransactionByHash(IClient client) : base(client, ApiMethods.getTransactionByHash.ToString()) { }

        public Task<AccountTx> SendRequestAsync(string txHash, object id = null)
        {
            if (txHash == null) throw new ArgumentNullException(nameof(txHash));
            return SendRequestAsync(id, txHash);
        }

        public RpcRequest BuildRequest(string txHash, object id = null)
        {
            if (txHash == null) throw new ArgumentNullException(nameof(txHash));
            return BuildRequest(id, txHash);
        }
    }
}
