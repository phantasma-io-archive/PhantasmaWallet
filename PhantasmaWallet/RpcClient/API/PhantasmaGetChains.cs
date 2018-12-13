using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetChains : GenericRpcRequestResponseHandlerNoParam<Chains>
    {
        public PhantasmaGetChains(IClient client) : base(client, ApiMethods.getChains.ToString()) { }
    }
}
