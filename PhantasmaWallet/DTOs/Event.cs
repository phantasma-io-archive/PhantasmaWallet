using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Phantasma.Cryptography;

namespace Phantasma.Wallet.DTOs
{
    public class Event
    {
        [JsonProperty("eventAddress")]
        public Address EventAddress { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("evtKind")]
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
