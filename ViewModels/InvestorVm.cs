using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class InvestorVm
    {
        public Guid KeyId { get; set; }

        [Required]
        public string FName { get; set; }

        [Required]
        public string LName { get; set; }


        public string MInitial { get; set; }

        [Required]
        public string EMail { get; set; }

        [Required]
        public string LoginName { get; set; }  // aka Investor Name.

        [Required]
        public string Password { get; set; }



        // 2.29.16 - Keep for now, as used by MapVmToAccountType(). 
        public string Url { get; set; }
    }
}
