using PIMS3.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class AssetCreationVm
    {
        // All are direct mappings to table.
        [Required]
        public string AssetId { get; set; }                     

        [Required]
        public string ProfileId { get; set; }

        [Required]
        public string AssetClassId { get; set; }               

        [Required]
        public string InvestorId { get; set; }
        
        public DateTime LastUpdate { get; set; }

        public IList<Position> Positions { get; set; }

        public Profile Profile { get; set; }

        public string ExceptionTickers { get; set; }
    }
}

/* Original code:
 
 * public string AssetInvestorId { get; set; }                     // Map -> Asset
      
        [Required]
        public string AssetTicker { get; set; }                         // Map -> Profile


        [Required]
        public string AssetDescription { get; set; }                    // Map -> Profile

        [Required]
        public string AssetClassification { get; set; }                 // Map -> AssetClass; recvd from client
        

        public string AssetClassificationId { get; set; }               // Map -> Asset; persist to Db
        

        public ProfileVm ProfileToCreate { get; set; }
        
        
        public PositionVm PositionToCreate { get; set; }                // includes AccountType & Transaction


        [Required]                                                      // added 5.10.16 for model state validation check
        public List<PositionVm> PositionsCreated { get; set; }


        public string AssetIdentification { get; set; }                 // Map -> Position Asset ref.
 

        public IncomeVm IncomeToCreate { get; set; }


        public List<IncomeVm> RevenueCreated { get; set; }              // Optional
 
 */
