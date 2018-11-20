using System;
using System.Threading.Tasks;
using Phantasma.Wallet.DTOs;
using Phantasma.Wallet.JsonRpc.Client;

namespace Phantasma.Wallet.RpcClient.API
{
    public class PhantasmaGetTxConfirmations : JsonRpc.Client.RpcRequestResponseHandler<TxConfirmation>
    {
        public PhantasmaGetTxConfirmations(IClient client) : base(client, APIMethods.getconfirmations.ToString()) { }

        public Task<TxConfirmation> SendRequestAsync(string hash, object id = null)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));
            return SendRequestAsync(id, hash);
        }

        public RpcRequest BuildRequest(string hash, object id = null)
        {
            if (hash == null) throw new ArgumentNullException(nameof(hash));
            return BuildRequest(id, hash);
        }
    }
}
