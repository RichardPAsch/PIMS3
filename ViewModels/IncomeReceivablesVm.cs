
namespace PIMS3.ViewModels
{
    public class IncomeReceivablesVm
    {
        public string PositionId { get; set; }
        public string TickerSymbol { get; set; }
        public string AccountTypeDesc { get; set; }
        public int DividendPayDay { get; set; }
        public string DividendFreq { get; set; }
        public string DividendMonths { get; set; }
        // Enable delinquent income receivables processing.
        public int MonthDue { get; set; }
        public string InvestorId { get; set; }
    }
}
