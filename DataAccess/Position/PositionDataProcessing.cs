using PIMS3.Data;
using System.Linq;
using PIMS3.ViewModels;
using System.Collections.Generic;
using System;
using Serilog;
using PIMS3.Data.Entities;
using PIMS3.BusinessLogic.PositionData;


namespace PIMS3.DataAccess.Position
{
    public class PositionDataProcessing
    {
        private PIMS3Context _ctx;
        private int recordsSaved = 0;

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
            // Received positionIds may reflect eligible positions to update, as a result of data-import XLSX revenue processing.
            var updatesAreOk = false;
            List<Data.Entities.Position> positionsToUpdate = new List<Data.Entities.Position>();
            List<IncomeReceivablesVm> delinquentPositions = new List<IncomeReceivablesVm>();
            var positionsUpdated = 0;

            // Get matching Positions.
            foreach (var id in positionIds)
            {
                positionsToUpdate.Add(_ctx.Position.Where(p => p.PositionId == id).FirstOrDefault());
            }

            // If months' end, capture any unpaid Positions ('PymtDue : true') for populating 'DelinquentIncomes' table.
            // Any delinquencies found for a Position, will render that Position still as 'PymtDue: true'.
            PositionProcessing positionProcessingBusLogic = new PositionProcessing(_ctx);
            positionProcessingBusLogic.GetPositionsWithIncomeDue(FetchInvestorId(positionIds));
            
            // Update Positions.
            if (positionsToUpdate.Count() == positionIds.Length)
            {
                foreach(var position in positionsToUpdate)
                {
                    // isRecorded: null/false - income received, but not yet recorded.
                    // isRecorded: true - income received & recorded, & now eligible for next receivable cycle.
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
            IQueryable<Data.Entities.Position> positions = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId
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
                // MonthDue = assetData.
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

        
        public IQueryable<DelinquentIncome> GetSavedDelinquentRecords(string investorId, string monthToCheck)
        {
            // Positions with delinquent payments.
            return _ctx.DelinquentIncome.Where(p => p.InvestorId == investorId && p.MonthDue == monthToCheck)
                                        .OrderByDescending(p => p.MonthDue)
                                        .ThenBy(p => p.TickerSymbol)
                                        .AsQueryable();
        }


        public bool SavePositionsWithOverdueIncome(List<DelinquentIncome> delinquentPositions)
        {
            _ctx.AddRange(delinquentPositions);
            recordsSaved = _ctx.SaveChanges();

            return recordsSaved > 0 ? true : false;
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


        private string FetchInvestorId(string[] recvdPositionIds)
        {
            IQueryable<Data.Entities.Position> positionFound = _ctx.Position.Where(p => p.PositionId == recvdPositionIds.First());
            string assetIdFound = FetchAssetId(positionFound.First().PositionId);
            IQueryable <Data.Entities.Asset> asset = _ctx.Asset.Where(a => a.AssetId == assetIdFound);

            return asset.First().InvestorId;
        }

    }

}
