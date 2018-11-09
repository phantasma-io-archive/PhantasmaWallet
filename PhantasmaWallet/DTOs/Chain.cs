using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class Chain
    {
        [JsonProperty("chain")]
        public string ChainChain { get; set; }

        [JsonProperty("balance")]
        public string Balance { get; set; }
    }
}
