using Phantasma.Wallet.Interfaces;
using Phantasma.Wallet.RpcClient;
using Phantasma.Wallet.RpcClient.API;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;

namespace Phantasma.Wallet.Services
{
    public class PhantasmaRpcService : RpcClientWrapper, IPhantasmaRpcService
    {
        public PhantasmaRpcService(IClient client) : base(client)
        {
            GetAccount = new PhantasmaGetAccount(client);
            GetAccountTransactions = new PhantasmaGetAccountTransactions(client);
            GetApplications = new PhantasmaGetApplications(client);
            GetBlockByHash = new PhantasmaGetBlockByHash(client);
            GetChains = new PhantasmaGetChains(client);
            GetTxConfirmations = new PhantasmaGetTxConfirmations(client);
            GetTokens = new PhantasmaGetTokens(client);
            SendRawTx = new PhantasmaSendRawTx(client);
        }

        public PhantasmaGetAccount GetAccount { get; }
        public PhantasmaGetAccountTransactions GetAccountTransactions { get; }
        public PhantasmaGetApplications GetApplications { get; }
        public PhantasmaGetBlockByHash GetBlockByHash { get; }
        public PhantasmaGetChains GetChains { get; }
        public PhantasmaGetTokens GetTokens { get; }
        public PhantasmaGetTxConfirmations GetTxConfirmations { get; set; }
        public PhantasmaSendRawTx SendRawTx { get; }
    }
}
