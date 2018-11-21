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

        [JsonProperty("decimals")]
        public int Decimals { get; set; }

        [JsonProperty("fungible")]
        public bool Fungible { get; set; }

        [JsonProperty("chains")]
        public List<BalanceChain> Chains { get; set; }
    }
}
