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

        [Required]
        public List<Position> Positions { get; set; }

        public Profile Profile { get; set; }

        public string ExceptionTickers { get; set; }

        [Required]
        public Asset Asset { get; set; }
    }
}


