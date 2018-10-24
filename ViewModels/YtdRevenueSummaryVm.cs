namespace PIMS3.ViewModels

{
    public class YtdRevenueSummaryVm
    {
        public int MonthRecvd { get; set; }
        public decimal AmountRecvd { get; set; }
        public decimal YtdAverage { get; set; }
        public decimal Rolling3MonthAverage { get; set; }
    }

}
