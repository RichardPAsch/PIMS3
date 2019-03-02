using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    public class Profile
    {
        [Required]
        public string ProfileId { get; set; }


        // Profile can only belong 1 unique 'Asset'
        // 1:1 cardinality via convention
        public Asset Asset { get; set; }


        //public string AssetId { get; set; }
       

        [Required]
        public string TickerSymbol { get; set; }


        // Non-null indicates user who created custom Profile 
        [Required]
        public string CreatedBy { get; set; }


        [Required]
        public string TickerDescription { get; set; }


        [Required]
        [Range(1.00, 40.00)]
        public decimal DividendRate { get; set; }


        [Required]
        [Range(0.01, 50.00)]
        public decimal? DividendYield { get; set; }

        [Required]
        public string DividendFreq { get; set; }

        [Required]
        public string DividendMonths { get; set; }

        [Required]
        public int DividendPayDay { get; set; }


        public DateTime? ExDividendDate { get; set; }


        public decimal? PERatio { get; set; }


        public decimal EarningsPerShare { get; set; }


        // aka 'Ask Price' or todays' market price.
        public decimal UnitPrice { get; set; }


        [Required]
        public DateTime LastUpdate { get; set; }

        
    }
}
