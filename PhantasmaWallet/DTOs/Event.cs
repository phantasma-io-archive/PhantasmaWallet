using Newtonsoft.Json;

namespace Phantasma.Wallet.DTOs
{
    public class Event
    {
        [JsonProperty("eventAddress")]
        public string EventAddress { get; set; }

        [JsonProperty("eventData")]
        public string Data { get; set; }

        [JsonProperty("eventKind")]
        public EvtKind EvtKind { get; set; }
    }

    public enum EvtKind
    {
        ChainCreate,
        TokenCreate,
        TokenInfo,
        TokenSend,
        TokenReceive,
        TokenMint,
        TokenBurn,
        AddressRegister,
        FriendAdd,
        FriendRemove,
    }
}
