using NeoModules.JsonRpc.Client;
using Phantasma.Wallet.Interfaces;
using Phantasma.Wallet.RpcClient.API;

namespace Phantasma.Wallet.RpcClient
{
    public class PhantasmaRpcService : RpcClientWrapper, IPhantasmaRpcService
    {
        public PhantasmaRpcService(IClient client) : base(client)
        {
            GetAccount = new PhantasmaGetAccount(client);
            GetAccountTransactions = new PhantasmaGetAccountTransactions(client);
        }

        public PhantasmaGetAccount GetAccount { get; }
        public PhantasmaGetAccountTransactions GetAccountTransactions { get; }
    }
}
