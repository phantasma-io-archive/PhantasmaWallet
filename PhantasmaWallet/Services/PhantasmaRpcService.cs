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
            GetBlockByHeight = new PhantasmaGetBlockByHeight(client);
            GetBlockHeight = new PhantasmaGetBlockHeight(client);
            GetBlockTxCountByHash = new PhantasmaGetBlockTxCountByHash(client);
            GetChains = new PhantasmaGetChains(client);
            GetTokens = new PhantasmaGetTokens(client);
            GetTransactionByBlockHashAndIndex = new PhantasmaGetTransactionByBlockHashAndIndex(client);
            GetTransactionByHash = new PhantasmaGetTransactionByHash(client);
            GetTransactionConfirmations = new PhantasmaGetTransactionConfirmations(client);
            SendRawTx = new PhantasmaSendRawTx(client);
        }

        public PhantasmaGetAccount GetAccount { get; }
        public PhantasmaGetAccountTransactions GetAccountTransactions { get; }
        public PhantasmaGetApplications GetApplications { get; }
        public PhantasmaGetBlockByHash GetBlockByHash { get; }
        public PhantasmaGetBlockByHeight GetBlockByHeight { get; }
        public PhantasmaGetBlockHeight GetBlockHeight { get; }
        public PhantasmaGetBlockTxCountByHash GetBlockTxCountByHash { get; }
        public PhantasmaGetChains GetChains { get; }
        public PhantasmaGetTokens GetTokens { get; }
        public PhantasmaGetTransactionByBlockHashAndIndex GetTransactionByBlockHashAndIndex { get; }
        public PhantasmaGetTransactionByHash GetTransactionByHash { get; }
        public PhantasmaGetTransactionConfirmations GetTransactionConfirmations { get; set; }
        public PhantasmaSendRawTx SendRawTx { get; }
    }
}
