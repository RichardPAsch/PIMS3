using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    public class Income 
    {
        [Required]
        public string IncomeId { get; set; }

        //[Required]
        //public Position Position { get; set; }

       [Required]
       public string PositionId { get; set; }
        

        [Required]
        public DateTime DateRecvd { get; set; }


        [Required]
        //[Range(0.01, 5000.00)]
        public decimal AmountRecvd { get; set; }
        
        
        public DateTime LastUpdate { get; set; }

       
    }
}
