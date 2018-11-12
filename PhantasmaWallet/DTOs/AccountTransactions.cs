using System.Collections.Generic;
using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class AccountTransactions
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("amount")]
        public long Amount { get; set; }

        [JsonProperty("txs")]
        public List<AccountTx> Txs { get; set; }

        public static AccountTransactions FromJson(string json) => JsonConvert.DeserializeObject<AccountTransactions>(json, JsonUtils.Settings);
        public string ToJson() => JsonConvert.SerializeObject(this, JsonUtils.Settings);
    }
}
