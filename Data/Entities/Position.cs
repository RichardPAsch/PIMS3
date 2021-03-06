﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.Data.Entities
{
    public class Position
    {
        [Required]
        public string PositionId { get; set; }


        // FK-dependent 
        [Required]
        public string AssetId { get; set; }

        // FK dependency for 1:1 setup.
        [Required]
        public string AccountTypeId { get; set; }


        // 1:M cardinality via convention
        public IList<Income> Incomes { get; set; }
        

        public Asset PositionAsset { get; set; }


        // 1:1 cardinality by convention   
        public AccountType AccountType { get; set; }

                      
        // (I)nactive or (A)ctive
        public string Status { get; set; }
        

        [Required]
        [Range(0,10000)]
        public int Quantity { get; set; }
        

        [Required]
        public DateTime LastUpdate { get; set; }


        // Design spec for 'Position' db field -> no nulls allowed.
        // Date Position was added to portfolio; used in Revenue editing (back-dating checks)
        [Required]
        public DateTime PositionDate { get; set; }


        [Range(0.00, 30000.00)]
        public decimal Fees { get; set; }


        [Required]
        [Range(0.00, 9000.00)]
        public decimal UnitCost { get; set; }

        public bool? PymtDue { get; set; }


    }
}
