using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class AssetCreationVm
    {
        [Required]
        public string AssetId { get; set; }                     // Map -> Asset

        [Required]
        public string ProfileId { get; set; }

        [Required]
        public string AssetClassId { get; set; }               // Map -> Asset; persist to Db

        [Required]
        public string InvestorId { get; set; }
        
        public DateTime LastUpdate { get; set; }
    }
}
