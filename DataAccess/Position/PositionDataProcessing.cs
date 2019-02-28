using PIMS3.Data;
using System.Linq;
using PIMS3.ViewModels;
using System.Collections.Generic;
using System;


namespace PIMS3.DataAccess.Position
{
    public class PositionDataProcessing
    {
        private PIMS3Context _ctx;

        public PositionDataProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }
        

        public IQueryable<Data.Entities.Asset> GetPositionAssetByTickerAndAccount(string tickerSymbol, string assetAccount)
        {
            // Temporary until security implemented.
            var currentInvestorId = "CF256A53-6DCD-431D-BC0B-A810010F5B88";

            var assetInfo = _ctx.Position
                                .Where(p => p.PositionAsset.InvestorId == currentInvestorId &&
                                            p.PositionAsset.Profile.TickerSymbol == tickerSymbol &&
                                            p.AccountType.AccountTypeDesc.ToUpper() == assetAccount.ToUpper())
                                .Select(p => p.PositionAsset);

            return assetInfo.AsQueryable();
        }


        public bool UpdatePositionPymtDueFlags(string[] positionIds)
        {
            var updatesAreOk = false;
            var positionsToUpdate = new List<Data.Entities.Position>();
            var positionsUpdated = 0;

            // Get matching Positions.
            foreach (var id in positionIds)
            {
                positionsToUpdate.Add(_ctx.Position.Where(p => p.PositionId == id).FirstOrDefault());
            }

            // Update Positions.
            if(positionsToUpdate.Count() == positionIds.Length)
            {
                foreach(var position in positionsToUpdate)
                {
                    position.PymtDue = false;
                    position.LastUpdate = DateTime.Now;
                }

                _ctx.UpdateRange(positionsToUpdate);
                positionsUpdated = _ctx.SaveChanges();

                if(positionsUpdated == positionsToUpdate.Count())
                    updatesAreOk = true;
            }

            return updatesAreOk;
        }


        public IQueryable<IncomeReceivablesVm> GetPositionsForIncomeReceivables(string investorId)
        {
            var positions = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId
                                                 &&  p.Status == "A" 
                                                 && (p.PymtDue == null || p.PymtDue == true))
                                         .AsQueryable();

            var assets = _ctx.Asset.Select(a => a);

            // p1: join table; p2&p3: join PKs; p4: projection form.
            var dataJoin = positions.Join(assets, p => p.AssetId, a => a.AssetId, (positionData, assetData) => new IncomeReceivablesVm
            {
                PositionId = positionData.PositionId,
                TickerSymbol = assetData.Profile.TickerSymbol,
                AccountTypeDesc = positionData.AccountType.AccountTypeDesc,
                DividendPayDay = assetData.Profile.DividendPayDay,
                DividendFreq = assetData.Profile.DividendFreq,
                DividendMonths = assetData.Profile.DividendMonths
            });

            return dataJoin.Where(data => data.DividendFreq == "A" || data.DividendFreq == "S" ||
                                          data.DividendFreq == "Q" || data.DividendFreq == "M")
                           .OrderBy(data => data.DividendFreq)
                           .ThenBy(data => data.TickerSymbol)
                           .AsQueryable();
        }


        public IQueryable<PositionsForEditVm> GetPositions(string investorId)
        {
            // Skipping filter for (I)nactive recs, allowing investor to view all.
            var positions = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId).AsQueryable();
            var assets = _ctx.Asset.Select(asset => asset).AsQueryable();

            return positions.Join(assets, p => p.AssetId, a => a.AssetId, (positionsInfo, assetsInfo) => new PositionsForEditVm
            {
                PositionId = positionsInfo.PositionId,
                TickerSymbol = assetsInfo.Profile.TickerSymbol,
                Account = positionsInfo.AccountType.AccountTypeDesc,
                LastUpdate = positionsInfo.LastUpdate.ToShortDateString(),
                Status = positionsInfo.Status,
                PymtDue = positionsInfo.PymtDue?? true,
                Revenue = positionsInfo.Incomes
            })
            .OrderBy(p => p.Status)
            .ThenBy(p => p.TickerSymbol)
            .AsQueryable();
        }

    }
}
