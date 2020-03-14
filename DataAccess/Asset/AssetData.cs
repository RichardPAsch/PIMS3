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

      
        public IQueryable<string> FetchPositionId(string investorId, string tickerSymbol, string account)
        {
            if (_ctx == null)
                return null;
            else
            {
                return _ctx.Asset.Where(a => a.InvestorId == investorId.Trim() && a.Profile.TickerSymbol == tickerSymbol.Trim())
                                 .SelectMany(a => a.Positions)
                                 .Where(p => p.AccountType.AccountTypeDesc == account.Trim() && p.Status == "A")
                                 .Select(p => p.PositionId)
                                 .AsQueryable();
            }
        }


       public bool UpdateAssetClass(string passedAssetId, string editedAssetClassCode)
        {
            int updateCount = 0;

            try
            {
                IQueryable<Data.Entities.Asset> assetToBeUpdated = _ctx.Asset.Where(a => a.AssetId == passedAssetId);
                string updatedAssetClassId = _ctx.AssetClass.Where(ac => ac.Code == editedAssetClassCode)
                                                            .FirstOrDefault()
                                                            .AssetClassId;

                assetToBeUpdated.First().AssetClassId = updatedAssetClassId;
                updateCount = _ctx.SaveChanges();
            }
            catch (System.Exception ex)
            {
                var debug = ex;
                return false;
            }

            return updateCount > 0 
                ? true 
                : false;
        }


       
    }
}

