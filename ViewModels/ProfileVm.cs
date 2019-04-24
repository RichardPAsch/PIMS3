using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class ProfileVm
    {
        public string ProfileId { get; set; }

        public string CreatedBy { get; set; }

        [Required]
        public string DividendFreq { get; set; }

        [Required]
        public string DividendMonths { get; set; }

        [Required]
        public int DividendPayDay { get; set; }

        [Required]
        public decimal DividendRate { get; set; }

        [Required]
        public decimal? DividendYield { get; set; }

        public decimal EarningsPerShare { get; set; }

        public DateTime? ExDividendDate { get; set; }

        [Required]
        public DateTime? LastUpdate { get; set; }

        // ReSharper disable once InconsistentNaming
        public decimal? PE_Ratio { get; set; }

        [Required]
        public string TickerDescription { get; set; }

        [Required]
        public string TickerSymbol { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }
        
    }
}
