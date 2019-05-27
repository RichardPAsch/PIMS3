using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class InvestorVm
    {
        public Guid InvestorId { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }
        
        [Required]
        public string LoginName { get; set; }  // aka Investor Name.

        [Required]
        public string Password { get; set; }
        

        // 2.29.16 - Keep for now, as used by MapVmToAccountType(). 
        public string Url { get; set; }
    }
}
