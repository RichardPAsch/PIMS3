using PIMS3.Data;
using PIMS3.Data.Repositories;
using System;
using System.Linq;


namespace PIMS3.Services
{
    public class CommonSvc : ICommonSvc
    {
        private readonly PIMS3Context _ctx;

        public CommonSvc(PIMS3Context ctx) 
        {
            _ctx = ctx;
        }


        public string GetInvestorIdFromInvestor(string investorEMail)
        {
            // investorEMail aka login name.
            return _ctx.Investor
                       .Where(investor => investor.EMailAddr.Trim() == investorEMail.Trim())
                       .FirstOrDefault().InvestorId
                       .ToString();
        }

        // From: C:\Development\PIMS_References\PIMS.WebApi\Utilities.cs
        public static string ParseAccountTypeFromDescription(string accountDesc)
        {
            // Any account description that includes superfluous data, e.g.,account number, will
            // be consolidated to the appropriate account type. Primarily used during XLS
            // revenue processing.

            if (accountDesc.ToUpper().IndexOf("IRA", StringComparison.Ordinal) >= 0 && accountDesc.ToUpper().IndexOf("ROTH", StringComparison.Ordinal) == -1)
                return "IRA";
            if (accountDesc.ToUpper().IndexOf("ROTH", StringComparison.Ordinal) >= 0 && accountDesc.ToUpper().IndexOf("IRA", StringComparison.Ordinal) >= 0)
                return "ROTH-IRA";

            return accountDesc.ToUpper().IndexOf("CMA", StringComparison.Ordinal) >= 0 ? "CMA" : null;
        }

        public static string GenerateGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }

}
