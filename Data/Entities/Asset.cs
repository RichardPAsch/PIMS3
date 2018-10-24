using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    public class Asset 
    {
        /* ------------------------------
         *  Aggregate ROOT master object. 
         * ------------------------------
        */

        [Required]
        public string AssetId { get; set; }

        // 1:1 cardinality by convention
        public AssetClass AssetClass { get; set; }    // e.g., common stock
        

        // FK-dependent
        [Required]
        public string ProfileId { get; set; }


        [Required]
        public string InvestorId { get; set; }

        [Required]
        public string AssetClassId { get; set; }
        

        // M:M relationship
        public IList<AssetInvestor> AssetInvestors { get; set; }


        // Unique per Investor/Asset/AccountType e.g., Roth-IRA
        // 1:M cardinality via convention
        public IList<Position> Positions { get; set; }
                       

        // 1:1 cardinality via convention
        public Profile Profile { get; set; }

        
        public DateTime? LastUpdate { get; set; }

        
    }
}
