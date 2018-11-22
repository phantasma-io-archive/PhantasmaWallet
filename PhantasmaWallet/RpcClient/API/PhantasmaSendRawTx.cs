using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;
using RpcRequest = Phantasma.Wallet.JsonRpc.Client.RpcRequest;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaSendRawTx : JsonRpc.Client.RpcRequestResponseHandler<JObject>
    {
        public PhantasmaSendRawTx(IClient client) : base(client, APIMethods.sendrawtransaction.ToString()) { }

        public Task<JObject> SendRequestAsync(string signedTx, object id = null)
        {
            if (signedTx == null) throw new ArgumentNullException(nameof(signedTx));
            return SendRequestAsync(id, signedTx);
        }

        public RpcRequest BuildRequest(string signedTx, object id = null)
        {
            if (signedTx == null) throw new ArgumentNullException(nameof(signedTx));
            return BuildRequest(id, signedTx);
        }
    }
}
