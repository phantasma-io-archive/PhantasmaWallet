using NeoModules.JsonRpc.Client;
using Phantasma.Wallet.Interfaces;
using Phantasma.Wallet.RpcClient;
using Phantasma.Wallet.RpcClient.API;

namespace Phantasma.Wallet.Services
{
    public class PhantasmaRpcService : RpcClientWrapper, IPhantasmaRpcService
    {
        public PhantasmaRpcService(IClient client) : base(client)
        {
            GetAccount = new PhantasmaGetAccount(client);
            GetAccountTransactions = new PhantasmaGetAccountTransactions(client);
            GetChains = new PhantasmaGetChains(client);
            SendRawTx = new PhantasmaSendRawTx(client);
        }

        public PhantasmaGetAccount GetAccount { get; }
        public PhantasmaGetAccountTransactions GetAccountTransactions { get; }
        public PhantasmaGetChains GetChains { get; }
        public PhantasmaSendRawTx SendRawTx { get; }
    }
}
