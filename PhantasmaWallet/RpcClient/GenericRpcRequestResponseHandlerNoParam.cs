using System.Threading.Tasks;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;

namespace Phantasma.Wallet.RpcClient
{
    public class GenericRpcRequestResponseHandlerNoParam<TResponse> : JsonRpc.Client.RpcRequestResponseHandlerNoParam<TResponse>
    {
        public GenericRpcRequestResponseHandlerNoParam(IClient client, string methodName) : base(client, methodName)
        {
        }

        public new Task<TResponse> SendRequestAsync(object id = null)
        {
            return base.SendRequestAsync(id);
        }
    }
}
