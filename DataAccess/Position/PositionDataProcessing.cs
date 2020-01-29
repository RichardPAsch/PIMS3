using PIMS3.Data;
using System.Linq;
using PIMS3.ViewModels;
using System.Collections.Generic;
using System;
using Serilog;


namespace PIMS3.DataAccess.Position
{
    public class PositionDataProcessing
    {
        private PIMS3Context _ctx;

        public PositionDataProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }
        

        public IQueryable<Data.Entities.Asset> GetPositionAssetByTickerAndAccount(string tickerSymbol, string assetAccount, string currentInvestorId)
        {
            IQueryable<Data.Entities.Asset> assetInfo = _ctx.Position
                                .Where(p => p.PositionAsset.InvestorId == currentInvestorId &&
                                            p.PositionAsset.Profile.TickerSymbol == tickerSymbol &&
                                            p.AccountType.AccountTypeDesc.ToUpper() == assetAccount.ToUpper())
                                .Select(p => p.PositionAsset);

            return assetInfo.AsQueryable();
        }


        public bool UpdatePositionPymtDueFlags(string[] positionIds, bool? isRecorded = null)
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
                    // isRecorded: null/false - income received, but not yet recorded.
                    // isRecorded: true - income received & recorded, & now eligible for next receivable.
                    // position.PymtDue = false;
                    position.PymtDue = isRecorded == null ? false : true;
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


        public IQueryable<PositionsForEditVm> GetPositions(string investorId, bool includeInactiveStatusRecs)
        {
            // Explicitly querying for status, as other statuses may be used in future versions.
            var positionAssetJoin = _ctx.Position.Where(p => (p.Status == "A" || p.Status == "I") && p.PositionAsset.InvestorId == investorId)
                                                  .Join(_ctx.Asset, a => a.AssetId, p => p.AssetId, (assetsData, positionData) =>
                                                              new
                                                              {
                                                                  posId = assetsData.PositionId,
                                                                  ticker = positionData.Profile.TickerSymbol,
                                                                  desc = positionData.Profile.TickerDescription,
                                                                  acct = assetsData.AccountType.AccountTypeDesc,
                                                                  pymt = assetsData.PymtDue,
                                                                  status = assetsData.Status,
                                                                  classId = positionData.AssetClassId
                                                              }
                                                        )
                                                  .AsQueryable();

            if (!includeInactiveStatusRecs)
            {
                positionAssetJoin = positionAssetJoin.Where(p => p.status == "A");
            }

            return positionAssetJoin.Join(_ctx.AssetClass, paJoin => paJoin.classId, ac => ac.AssetClassId, (paJoinInfo, acInfo) => 
                                                new PositionsForEditVm
                                                {
                                                    PositionId = paJoinInfo.posId,
                                                    TickerSymbol = paJoinInfo.ticker,
                                                    TickerDescription = paJoinInfo.desc,
                                                    Account = paJoinInfo.acct,
                                                    AssetClass = acInfo.Code,
                                                    PymtDue = (Convert.ToBoolean(paJoinInfo.pymt) == true || paJoinInfo.pymt == null) 
                                                                    ? true 
                                                                    : false,
                                                    Status = paJoinInfo.status

                                                })
                                    .AsQueryable()
                                    .OrderBy(p => p.Status)
                                    .ThenBy(p => p.TickerSymbol);
        }


        public int UpdatePositions(PositionsForEditVm[] editedPositionsAbridged)
        {
            // Persist 'PymtDue' and/or 'Status' edit(s).
            List<Data.Entities.Position> positionsToUpdateListing = new List<Data.Entities.Position>();
            int updateCount = 0;

            foreach (PositionsForEditVm pos in editedPositionsAbridged)
            {
                positionsToUpdateListing.Add(_ctx.Position.Where(p => p.PositionId == pos.PositionId).First());
            }

            positionsToUpdateListing.OrderBy(p => p.PositionId);
            editedPositionsAbridged.OrderBy(p => p.PositionId);

            // Update fetched corresponding positions.
            if (positionsToUpdateListing.Count() == editedPositionsAbridged.Length)
            {
                foreach (var position in positionsToUpdateListing)
                {
                    if(positionsToUpdateListing.First().PositionId == editedPositionsAbridged.First().PositionId)
                    {
                        position.PymtDue = editedPositionsAbridged.First().PymtDue;
                        position.Status = editedPositionsAbridged.First().Status;
                        position.LastUpdate = DateTime.Now;
                        position.AccountTypeId = editedPositionsAbridged.First().AccountTypeId;
                    }
                }

                try
                {
                    _ctx.UpdateRange(positionsToUpdateListing);
                    updateCount = _ctx.SaveChanges();
                }
                catch (Exception ex)
                {
                    Log.Error("Error updating Position(s) via PositionDataProcessing.UpdatePositions(), due to {0}", ex.Message);
                    return updateCount;
                }
                _ctx.UpdateRange(positionsToUpdateListing);
                updateCount = _ctx.SaveChanges();
            }

            return updateCount;
        }


        // Data used for lookup/dropdown functionality in 'Positions' grid ONLY, hence, including here for now.
        public IQueryable<AssetClassesVm> GetAssetClassDescriptions()
        {
            return _ctx.AssetClass.Select((ac) => new AssetClassesVm() {
                Code = ac.Code,
                Description = ac.Description
            })
            .OrderBy(ac => ac.Code)
            .AsQueryable();
        }


        public string FetchAssetId(string targetPositionId)
        {
            if (_ctx == null)
                return null;
            else
            {
                return _ctx.Position.Where(p => p.PositionId == targetPositionId)
                           .FirstOrDefault().AssetId
                           .ToString();
            }

        }

    }

}
