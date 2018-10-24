using System;


namespace PIMS3.Data.Entities
{
    // Join entity for M:M relationship.
    public class AssetInvestor
    {
        public string InvestorId { get; set; }

        public string AssetId { get; set; }

        public Asset Asset { get; set; }

        public Investor Investor { get; set; }
 
    }
}
