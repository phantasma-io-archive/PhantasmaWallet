using System.Collections.Generic;
using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class Chains
    {
        [JsonProperty("chains")]
        public List<ChainElement> ChainList { get; set; }

        public static Chains FromJson(string json) => JsonConvert.DeserializeObject<Chains>(json, JsonUtils.Settings);

        public string ToJson() => JsonConvert.SerializeObject(this, JsonUtils.Settings);

    }
    public class ChainElement
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("parent")]
        public string ParentChain { get; set; }
    }
}
