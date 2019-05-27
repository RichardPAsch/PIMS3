using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
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


        //[Required]
        public byte[] PasswordHash { get; set; }


        //[Required]
        public byte[] PasswordSalt { get; set; }


    }
}
