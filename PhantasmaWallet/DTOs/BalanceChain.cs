using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class BalanceChain
    {
        [JsonProperty("chain")]
        public string ChainName { get; set; }

        [JsonProperty("balance")]
        public string Balance { get; set; }
    }
}
