using System;

namespace PIMS3.ViewModels
{
    public class IncomeSavedVm
    {
        public string TickerSymbol { get; set; }
        public string AccountTypeDesc { get; set; }
        public string DividendFreq { get; set; }
        public DateTime DateRecvd { get; set; }
        public decimal AmountReceived { get; set; }
        public string IncomeId { get; set; }
       
    }
}
