using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetBlockTxCountByHash : GenericRpcRequestResponseHandlerNoParam<int>
    {
        public PhantasmaGetBlockTxCountByHash(IClient client) : base(client, ApiMethods.getBlockTransactionCountByHash.ToString()) { }
    }
}
