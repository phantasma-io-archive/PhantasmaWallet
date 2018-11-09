using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Phantasma.Wallet
{
    public static class JsonUtils
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Error = (sender, args) =>
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            },
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
