using PIMS3.Data;
using System.Linq;


namespace PIMS3.DataAccess.Asset
{
    public class AssetData
    {
        private readonly PIMS3Context _ctx;


        public AssetData(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public AssetData()
        {
        }


        public IQueryable<string> FetchPositionId(string investorId, string tickerSymbol, string account)
        {
            return _ctx.Asset.Where(a => a.InvestorId == investorId.Trim() && a.Profile.TickerSymbol == tickerSymbol.Trim())
                             .SelectMany(a => a.Positions)
                             .Where(p => p.AccountType.AccountTypeDesc == tickerSymbol.Trim())
                             .Select(p => p.PositionId)
                             .AsQueryable();

        }

        
    }
}

