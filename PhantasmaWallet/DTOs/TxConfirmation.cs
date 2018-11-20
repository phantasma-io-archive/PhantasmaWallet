using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class TxConfirmation
    {
        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("confirmations")]
        public int Confirmations { get; set; }

        public bool IsConfirmed => Confirmations >= 5;
    }
}
