using System;

namespace Phantasma.Wallet.DTOs
{
    public class MenuEntry
    {
        public string Id { get; set; }
        public string Icon { get; set; }
        public string Caption { get; set; }
        public bool Enabled { get; set; }
        public int Count { get; set; }
        public bool IsSelected { get; set; }
    }

    public class AccountCache
    {
        public DateTime lastUpdated;
        public Transaction[] transactions;
        public Holding[] holdings;
        public Token[] tokens;
    }

    public struct Holding
    {
        public string name;
        public string symbol;
        public string icon;
        public decimal amount;
        public decimal rate;
        public decimal usd => (amount * rate);
        public string amountFormated => amount.ToString("0.####");
        public string usdFormated => usd.ToString("0.####");
    }

    public struct Transaction
    {
        public DateTime date;
        public string hash;
        public string description;
    }

    public class Net
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool IsEnabled { get; set; }
    }

    public struct ErrorContext
    {
        public string ErrorDescription { get; set; }
        public string ErrorCode { get; set; }
    }

    public struct SettleTx
    {
        public string chainName;
        public string chainAddress;
        public string destinationChainAddress;
    }
}
