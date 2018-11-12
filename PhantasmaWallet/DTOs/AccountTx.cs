using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class AccountTx
    {
        [JsonProperty("txid")]
        public string Txid { get; set; }

        [JsonProperty("chainAddress")]
        public string ChainAddress { get; set; }

        [JsonProperty("chainName")]
        public string ChainName { get; set; }

        [JsonProperty("timestamp")]
        public uint Timestamp { get; set; }

        [JsonProperty("blockHeight")]
        public uint BlockHeight { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("asset")]
        public string Asset { get; set; }

        [JsonProperty("addressTo")]
        public string AddressTo { get; set; }

        [JsonProperty("addressFrom")]
        public string AddressFrom { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("gasLimit")]
        public decimal GasLimit { get; set; }

        [JsonProperty("gasPrice")]
        public decimal GasPrice { get; set; }
    }
}
