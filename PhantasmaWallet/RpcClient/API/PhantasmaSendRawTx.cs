using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;
using RpcRequest = Phantasma.Wallet.JsonRpc.Client.RpcRequest;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaSendRawTx : JsonRpc.Client.RpcRequestResponseHandler<SendRawTx>
    {
        public PhantasmaSendRawTx(IClient client) : base(client, ApiMethods.sendRawTransaction.ToString()) { }

        public Task<SendRawTx> SendRequestAsync(string signedTx, object id = null)
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
