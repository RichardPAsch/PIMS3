using System;
using System.Collections.Generic;
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



    public class ProfileForUpdateVm
    {
        // Model for batch updating Profiles, based on income history.
        public string ProfileId { get; set; }

        public string TickerSymbol { get; set; }

        public string DividendFreq { get; set; }

        public string DividendMonths { get; set; }

        public int DividendPayDay { get; set; }

        public DateTime? LastUpdate { get; set; }

        public List<Data.Entities.Profile> BatchProfilesList { get; set; }

        public string ExceptionTickerSymbols { get; set; }

        public bool UpdateHasErrors { get; set; }

        

    }


}
