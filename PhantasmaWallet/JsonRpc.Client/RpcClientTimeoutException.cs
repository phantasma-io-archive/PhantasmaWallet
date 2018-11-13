using System;

namespace Phantasma.Wallet.JsonRpc.Client
{
    public class RpcClientTimeoutException : Exception
    {
        public RpcClientTimeoutException(string message) : base(message)
        {
        }

        public RpcClientTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}