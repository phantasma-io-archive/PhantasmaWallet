using System.Net;
using LunarLabs.Parser.JSON;

namespace Phantasma.Wallet.Helpers
{
    public static class Utils
    {
        public static decimal GetCoinRate(uint ticker, string symbol = "USD")
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert={symbol}";

            string json;

            try
            {
                using (var wc = new WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];
                var quotes = root["quotes"][symbol];

                var price = quotes.GetDecimal("price");

                return price;
            }
            catch
            {
                return 0;
            }
        }
    }
}
