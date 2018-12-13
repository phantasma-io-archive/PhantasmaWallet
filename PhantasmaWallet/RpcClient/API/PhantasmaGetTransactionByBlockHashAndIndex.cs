using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetTransactionByBlockHashAndIndex : RpcRequestResponseHandler<AccountTx>
    {
        public PhantasmaGetTransactionByBlockHashAndIndex(IClient client) : base(client, ApiMethods.getTransactionByBlockHashAndIndex.ToString()) { }

        public Task<AccountTx> SendRequestAsync(string blockHash, int index, object id = null)
        {
            if (blockHash == null) throw new ArgumentNullException(nameof(blockHash));
            return SendRequestAsync(id, blockHash, index);
        }

        public RpcRequest BuildRequest(string blockHash, int index, object id = null)
        {
            if (blockHash == null) throw new ArgumentNullException(nameof(blockHash));
            return BuildRequest(id, blockHash, index);
        }
    }
}
