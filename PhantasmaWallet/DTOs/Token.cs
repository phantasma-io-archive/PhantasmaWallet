using System.Collections.Generic;
using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class Token
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("chains")]
        public List<BalanceChain> Chains { get; set; }
    }
}
