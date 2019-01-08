using System;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Wallet.Models
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
        public DateTime LastUpdated;
        public Transaction[] Transactions;
        public Holding[] Holdings;
        public BalanceSheetDto[] Tokens;
    }

    public struct Holding
    {
        public string Name;
        public string Symbol;
        public string Icon;
        public decimal Amount;
        public decimal Rate;
        public decimal Usd => (Amount * Rate);
        public string AmountFormated => Amount.ToString("0.####");
        public string UsdFormated => Usd.ToString("0.####");
    }

    public struct Transaction
    {
        public DateTime Date;
        public string Hash;
        public string Description;
    }

    public class Net
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public bool IsEnabled { get; set; }
    }

    public struct ErrorContext
    {
        public string ErrorDescription;
        public string ErrorCode;
    }

    public struct SettleTx
    {
        public string ChainName;
        public string ChainAddress;
        public string DestinationChainAddress;
    }

    public struct TransferTx
    {
        public bool IsFungible;
        public string AddressTo;
        public string FromChain;
        public string ToChain;
        public string FinalChain;
        public string Symbol;
        public string AmountOrId;
    }
}
