using PIMS3.Data;
using System.Linq;

namespace PIMS3.DataAccess.Position
{
    public class PositionDataProcessing
    {
        private PIMS3Context _ctx;

        public PositionDataProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }



        public bool GetPositionByTickerAndAccount(string tickerSymbol, string assetAccount)
        {
            // Temporary until security implemented.
            var currentInvestorId = "CF256A53-6DCD-431D-BC0B-A810010F5B88"; 

            var positionInfo = _ctx.Position
                                   .Where(p => p.PositionAsset.InvestorId == currentInvestorId &&
                                               p.PositionAsset.Profile.TickerSymbol == tickerSymbol &&
                                               p.AccountType.AccountTypeDesc.ToUpper() == assetAccount.ToUpper())
                                   .Select(p => p.PositionAsset)
                                   .AsQueryable();

            return positionInfo.Any();
        }
    }
}
