using System;
using System.Threading.Tasks;
using NeoModules.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaSendRawTx : RpcRequestResponseHandler<string>
    {
        public PhantasmaSendRawTx(IClient client) : base(client, APIMethods.sendrawtransaction.ToString()) { }

        public Task<string> SendRequestAsync(string chain, string signedTx, object id = null)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));
            if (signedTx == null) throw new ArgumentNullException(nameof(signedTx));
            return SendRequestAsync(id, chain, signedTx);
        }

        public RpcRequest BuildRequest(string address, object id = null)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            return BuildRequest(id, address);
        }
    }
}
