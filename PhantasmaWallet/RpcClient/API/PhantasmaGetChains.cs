using System.Collections.Generic;
using NeoModules.JsonRpc.Client;
using Phantasma.Wallet.DTOs;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetChains : GenericRpcRequestResponseHandlerNoParam<Chains>
    {
        public PhantasmaGetChains(IClient client) : base(client, APIMethods.getchains.ToString())
        {
        }
    }
}
