using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    public class Investor
    {
        [Required]
        public string InvestorId { get; set; }


        // M:M relationship
        public IList<AssetInvestor> AssetInvestors { get; set; }


        // ** NOTE TODO ** 
        // 5.20.19 - Commented '[Required]' annotated fields should be considered for elimination, as they may no  
        //           longer be necessary for new security implementation! Check for current usage.
        //           Exception: InvestorId.

        [Required]
        public  string LastName { get; set; }


        [Required]
        public  string FirstName { get; set; }


        public string MiddleInitial { get; set; }


        public string BirthDay { get; set; }


        //[Required]
        public string Address1 { get; set; }


        public string Address2 { get; set; }

        //[Required]
        public  string City { get; set; }


        //[Required]
        public string State { get; set; }


        //[Required]
        public string ZipCode { get; set; }


        public string Phone { get; set; }


        public string Mobile { get; set; }
        
        [Required]
        public  string LoginName { get; set; }


        //[Required]
        public string Password { get; set; }


        public string Token { get; set; }


    }
}
