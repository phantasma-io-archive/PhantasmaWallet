using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetTokens : GenericRpcRequestResponseHandlerNoParam<TokenList>
    {
        public PhantasmaGetTokens(IClient client) : base(client, ApiMethods.getTokens.ToString())
        {
        }
    }
}
