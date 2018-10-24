using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;



namespace PIMS3.Data.Entities
{
   // Seed as a static file, used for look ups?
    public class AccountType 
    {
        // NH PK Mapping: AccountTypeId 
        [Key]
        public string AccountTypeId { get; set; }


        // 1:1 cardinality by convention
        public Position Position { get; set; }

             
        [Required]
        public string AccountTypeDesc { get; set; }
       
    }
}
