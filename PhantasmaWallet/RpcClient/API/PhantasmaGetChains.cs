using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetChains : GenericRpcRequestResponseHandlerNoParam<string>
    {
        public PhantasmaGetChains(IClient client) : base(client, APIMethods.getchains.ToString())
        {
        }
    }
}
