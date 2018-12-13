using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetApplications : GenericRpcRequestResponseHandlerNoParam<AppList>
    {
        public PhantasmaGetApplications(IClient client) : base(client, ApiMethods.getApps.ToString()) { }
    }
}
