using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
   public class PhantasmaGetBlockByHash : RpcRequestResponseHandler<Block>
    {
        public PhantasmaGetBlockByHash(IClient client) : base(client, ApiMethods.getBlockByHash.ToString()) { }

        public Task<Block> SendRequestAsync(string hash, object id = null)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));
            return SendRequestAsync(id, hash);
        }

        public RpcRequest BuildRequest(string hash, object id = null)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));
            return BuildRequest(id, hash);
        }
    }
}
