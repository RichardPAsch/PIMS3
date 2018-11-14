using PIMS3.Data;
using PIMS3.Data.Repositories;
using System;

namespace PIMS3.Services
{
    public class CommonSvc : ICommonSvc
    {
        private readonly PIMS3Context _ctx;
        //private readonly InvestorRepository _repo;

        public CommonSvc(PIMS3Context ctx) //, InvestorRepository repo)
        {
            _ctx = ctx;
           // _repo = repo;
        }


        public string GetInvestorIdFromInvestor(string currentInvestor)
        {
            //return _repo.RetreiveIdByName(currentInvestor);
            return "";
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
    }

}
