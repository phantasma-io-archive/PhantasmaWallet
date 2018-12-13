using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetBlockHeight : RpcRequestResponseHandler<BlockHeight>
    {
        public PhantasmaGetBlockHeight(IClient client) : base(client, ApiMethods.getBlockHeight.ToString()) { }

        public Task<BlockHeight> SendRequestAsync(string chain, object id = null)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));
            return SendRequestAsync(id, chain);
        }

        public RpcRequest BuildRequest(string chain, object id = null)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));
            return BuildRequest(id, chain);
        }
    }
}
