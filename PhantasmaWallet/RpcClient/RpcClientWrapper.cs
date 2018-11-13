using System;
using IClient = Phantasma.Wallet.JsonRpc.Client.IClient;

namespace Phantasma.Wallet.RpcClient
{
    public class RpcClientWrapper
    {
        public RpcClientWrapper(IClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected IClient Client { get; set; }

        public void SwitchClient(IClient client)
        {
            Client = client;
        }
    }
}
