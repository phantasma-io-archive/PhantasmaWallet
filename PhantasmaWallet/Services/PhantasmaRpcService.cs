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
            GetChains = new PhantasmaGetChains(client);
            GetTxConfirmations = new PhantasmaGetTxConfirmations(client);
            SendRawTx = new PhantasmaSendRawTx(client);
        }

        public PhantasmaGetAccount GetAccount { get; }
        public PhantasmaGetAccountTransactions GetAccountTransactions { get; }
        public PhantasmaGetChains GetChains { get; }
        public PhantasmaGetTxConfirmations GetTxConfirmations { get; set; }
        public PhantasmaSendRawTx SendRawTx { get; }
    }
}
