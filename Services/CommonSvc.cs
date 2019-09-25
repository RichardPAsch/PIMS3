using PIMS3.Data;
using System;
using System.Linq;

namespace PIMS3.Services
{
    public class CommonSvc : ICommonSvc
    {
        private readonly PIMS3Context _ctx;
        private static string logFile = string.Empty;

        public CommonSvc(PIMS3Context ctx) 
        {
            _ctx = ctx;
        }


        public static string LogFile
        {
            // Includes full file path.
            get => logFile;
            set
            {
                if (value != string.Empty && value != null)
                {
                    logFile = value.Trim();
                }
            }
        }
        
        public string GetInvestorIdFromInvestor(string login)
        {
            return _ctx.Investor
                       .Where(investor => investor.LoginName.Trim() == login.Trim())
                       .FirstOrDefault().InvestorId
                       .ToString();
        }
        
        public static string ParseAccountTypeFromDescription(string accountDesc)
        {
            // Any received account description that includes superfluous data, e.g.,account number,
            // will be truncated to the appropriate account type. Primarily used during XLSX data import
            // revenue processing.

            if (accountDesc.ToUpper().IndexOf("IRA", StringComparison.Ordinal) >= 0 && accountDesc.ToUpper().IndexOf("ROTH", StringComparison.Ordinal) == -1)
                return "IRA";
            if (accountDesc.ToUpper().IndexOf("ROTH", StringComparison.Ordinal) >= 0 && accountDesc.ToUpper().IndexOf("IRA", StringComparison.Ordinal) >= 0)
                return "ROTH-IRA";
            if (accountDesc.ToUpper().IndexOf("401", StringComparison.Ordinal) >= 0)
                return "401(k)";
            if (accountDesc.ToUpper().IndexOf("KEOGH", StringComparison.Ordinal) >= 0)
                return "KEOGH";

            return accountDesc.ToUpper().IndexOf("CMA", StringComparison.Ordinal) >= 0 ? "CMA" : null;
        }

        public static string GenerateGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }

}
