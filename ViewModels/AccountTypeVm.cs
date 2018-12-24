using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class AccountTypeVm
    {
        public Guid KeyId { get; set; }
        
        [Required]
        public string AccountTypeDesc { get; set; }

        // 2.29.16 - Keep for now, as used by MapVmToAccountType(). 
        public string Url { get; set; }
    }
}
