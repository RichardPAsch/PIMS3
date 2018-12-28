using PIMS3.Data;
using System.Linq;

namespace PIMS3.DataAccess.Account
{
    public class AccountDataProcessing
    {
        private PIMS3Context _ctx;

        public AccountDataProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public IQueryable<string> GetAccountTypeId(string acctDesc)
        {
            return _ctx.AccountType.Where(a => a.AccountTypeDesc.Trim() == acctDesc.Trim())
                                   .Select(a => a.AccountTypeId)
                                   .AsQueryable();

        }


        



    }
}
