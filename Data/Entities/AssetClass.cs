using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    public class AssetClass 
    {
        [Key]
        public string AssetClassId { get; set; }
        

        // 1:1 cardinality by convention
        public Asset Asset { get; set; }


        // FK-dependent for 1:1 
        public string AssetId { get; set; } 
                

        // Example: "CS"
        [Required]
        public string Code { get; set; }


        // Example: "Common Stock"
        [Required]
        public string Description { get; set; }


        public string LastUpdate { get; set; }
    
    }
}
