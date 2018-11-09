using System.Collections.Generic;
using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class Token
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("chains")]
        public List<Chain> Chains { get; set; }
    }
}
