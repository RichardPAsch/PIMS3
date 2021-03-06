﻿using System;
using System.ComponentModel.DataAnnotations;


namespace PIMS3.ViewModels
{
    public class PositionVm
    {
        public string PositionId { get; set; }

        // 12.24.18 - deferred for possible future use.
        //[Required]
        //public string PreEditPositionAccount { get; set; }

        //public string PostEditPositionAccount { get; set; }

        [Required]
        public decimal Qty { get; set; }

        [Required]
        public decimal UnitCost { get; set; }

        // Date Position added.
        [Required]
        public DateTime? DateOfPurchase { get; set; }

        [Required]
        public DateTime? LastUpdate { get; set; }

        
        public string Status { get; set; }

        public decimal Fees { get; set; }

    }


    public class PositionsForEditVm
    {
        public string PositionId { get; set; }

        public string TickerSymbol { get; set; }

        public string TickerDescription { get; set; }

        public string Account { get; set; }

        //public string LastUpdate { get; set; }
        public string AssetClass { get; set; }

        public string Status { get; set; }

        public bool PymtDue { get; set; }

        public string AccountTypeId { get; set; }

    }

    public class PositionsForPaymentDueVm
    {
        public string PositionId { get; set; }

        public string MonthDue { get; set; }

        public string TickerSymbol { get; set; }

        public string InvestorId { get; set; }
    }
}
