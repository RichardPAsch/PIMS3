using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    /* 
       'Investor' entity represents data for an investor within PIMS, and can be used to pass data between 
       different parts of the application (e.g. between services and controllers), as well as returning 
       http response data from controller action methods.
    */

    public class Investor
    {
        // Required M:M relationship
        public IList<AssetInvestor> AssetInvestors { get; set; }


        [Required]
        public string InvestorId { get; set; }


        [Required]
        public  string LastName { get; set; }


        [Required]
        public  string FirstName { get; set; }


        [Required]
        public  string LoginName { get; set; }


        public byte[] PasswordHash { get; set; }


        public byte[] PasswordSalt { get; set; }


        public string Role { get; set; }


    }
}
