using PIMS3.Data;
using System.Linq;
using PIMS3.ViewModels;
using System.Collections.Generic;
using System;
using Serilog;
using PIMS3.Data.Entities;


namespace PIMS3.DataAccess.Position
{
    public class PositionDataProcessing
    {
        // TODO: REFACTOR !!

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


        public bool UpdatePositionPymtDueFlags(List<PositionsForPaymentDueVm> sourcePositionsInfo, bool isRecorded = false)
        {
            // -----------------------------------------
            // This should be refactored into BL layer!!
            // -----------------------------------------

            // Received sourcePositionsInfo may contain either:
            //  1. data-imported month-end XLSX revenue processed Position Ids, OR
            //  2. selected 'Income Due' Positions marked for pending payment processing; selection(s) may also include * delinquent positions *.
            bool updatesAreOk = false;
            List<Data.Entities.Position> targetPositionsToUpdate = new List<Data.Entities.Position>();
            IList<DelinquentIncome> delinquentPositions = new List<DelinquentIncome>();
            int positionsUpdated = 0;

            // Context of processing.
            // isRecorded: false - income received, but not yet recorded, e.g., processing payment(s) via 'Income Due'.
            // isRecorded: true  - income received & recorded/saved, and is now eligible for next receivable cycle, e.g., XLSX via 'Data Import'.
            if (!isRecorded)
            {
                // Any delinquent records will appear first.
                var sourcePositionsInfoSorted = sourcePositionsInfo.OrderBy(p => p.TickerSymbol).ThenBy(p => p.MonthDue);
                foreach (var selectedPosition in sourcePositionsInfoSorted)
                {
                    // If selected Position is overdue, then -> delete from 'DelinquentIncome' table.
                    if (int.Parse(selectedPosition.MonthDue) < DateTime.Now.Month)
                    {
                        DelinquentIncome delinquentIncomeToDelete = new DelinquentIncome
                        {
                            PositionId = selectedPosition.PositionId,
                            TickerSymbol = selectedPosition.TickerSymbol,
                            MonthDue = selectedPosition.MonthDue,
                            InvestorId = FetchInvestorId(selectedPosition.PositionId)
                        };
                        // If we have *duplicate* selected PositionIds, e.g., 1 current & 1 delinquent, then delete the delinquent record 
                        // first, so that 'DelinquentIncome' table is updated real-time. 
                        int duplicatePositionIdCount = sourcePositionsInfoSorted.Where(di => di.PositionId == selectedPosition.PositionId).Count();
                        if(duplicatePositionIdCount == 1)
                            delinquentPositions.Add(delinquentIncomeToDelete);
                        else
                        {
                            IList<DelinquentIncome> tempDelinquentPositions = new List<DelinquentIncome>
                            {
                                delinquentIncomeToDelete
                            };
                            RemoveDelinquency(tempDelinquentPositions);
                        }
                    }
                    else
                    {
                        // Mark Position(s) as paid, if no outstanding delinquencies.
                        IList<DelinquentIncome> delinquentPositionsFound = _ctx.DelinquentIncome.Where(d => d.PositionId == selectedPosition.PositionId).ToList();
                        if (!delinquentPositionsFound.Any())
                        {
                            Data.Entities.Position targetPositionToUpdate = _ctx.Position.Where(p => p.PositionId == selectedPosition.PositionId).First();
                            targetPositionToUpdate.PymtDue = false;
                            targetPositionToUpdate.LastUpdate = DateTime.Now;

                            targetPositionsToUpdate.Add(targetPositionToUpdate);
                        }
                    }
                }

                if (delinquentPositions.Any())
                    updatesAreOk = RemoveDelinquency(delinquentPositions);

                if (targetPositionsToUpdate.Any())
                    updatesAreOk = UpdateTargetPositions(targetPositionsToUpdate);
            }
            else
            {
                string currentInvestorId = FetchInvestorId(sourcePositionsInfo.First().PositionId);
                delinquentPositions = GetSavedDelinquentRecords(currentInvestorId, "");

                // Loop thru each XLSX position within the month-end collection & determine if it's eligible for updating its' 'PymtDue' flag.
                foreach (var xlsxPosition in sourcePositionsInfo)
                {
                    IList<DelinquentIncome> foundDelinquentPosition = delinquentPositions.Where(p => p.PositionId == xlsxPosition.PositionId).ToList();
                    if (foundDelinquentPosition != null)
                    {
                        continue;
                    }
                    else
                    {
                        Data.Entities.Position positionToUpdate = _ctx.Position.Where(p => p.PositionId == xlsxPosition.PositionId).First();
                        positionToUpdate.PymtDue = true;
                        positionToUpdate.LastUpdate = DateTime.Now;

                        targetPositionsToUpdate.Add(positionToUpdate);
                    }
                }

                if(targetPositionsToUpdate.Count() >= 1)
                {
                    _ctx.AddRange(targetPositionsToUpdate);
                    positionsUpdated = _ctx.SaveChanges();

                    if (positionsUpdated == sourcePositionsInfo.Count())
                        updatesAreOk = true;
                }
            }

            return updatesAreOk;



            /* ======= orig code =============
            // Get matching Positions.
            //foreach (var position in sourcePositionsInfo)
            //{
            //    JObject positionIdInfo = JObject.Parse(position.ToString());
            //    string currentPosId = positionIdInfo["positionId"].ToString().Trim();
            //    targetPositionsToUpdate.Add(_ctx.Position.Where(p => p.PositionId == currentPosId).FirstOrDefault());
            //}

            // If months' end, capture any unpaid Positions ('PymtDue : true') for populating 'DelinquentIncomes' table.
            // Any delinquencies still found for a Position, will mark that Position as 'PymtDue: true'.
            PositionProcessing positionProcessingBusLogic = new PositionProcessing(_ctx);
            string currInvestorId = FetchInvestorId(targetPositionsToUpdate.First().PositionId);
            delinquentPositions = GetSavedDelinquentRecords(currInvestorId, null);
            
            // Update Positions, where appropriate.
            if (targetPositionsToUpdate.Count() == sourcePositionsInfo.Count)
            {
                //bool delinquencyRemoved;
                foreach(Data.Entities.Position position in targetPositionsToUpdate)
                {
                    // Omit position update if any outstanding overdue payments found. 
                    IQueryable<DelinquentIncome> delinquentPositionsFound = delinquentPositions.Where(p => p.PositionId == position.PositionId);
                    if(delinquentPositionsFound.Count() >= 1)
                    {
                       // delinquencyRemoved = RemoveDelinquency(delinquentPositionsFound.First());
                        //position.PymtDue = (delinquentPositionsFound.Count() == 1 && delinquencyRemoved) ? false : true;
                    }
                    else
                    {
                        // position.PymtDue = false;
                        //position.PymtDue = isRecorded == null ? false : true;
                        position.PymtDue = isRecorded;
                    }
                    position.LastUpdate = DateTime.Now;
                }
                
                //_ctx.UpdateRange(targetPositionsToUpdate);
                //positionsUpdated = _ctx.SaveChanges();

                //if(positionsUpdated == targetPositionsToUpdate.Count())
                //    updatesAreOk = true;
            }
            return updatesAreOk;
            === end orig code ==== */

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

        
        public IList<DelinquentIncome> GetSavedDelinquentRecords(string investorId, string monthToCheck = "")
        {
            // Positions with delinquent payments.
            if (string.IsNullOrEmpty(monthToCheck))
            {
                return _ctx.DelinquentIncome.Where(p => p.InvestorId == investorId)
                                        .OrderByDescending(p => p.MonthDue)
                                        .ThenBy(p => p.TickerSymbol)
                                        .ToList();
                                        //.AsQueryable();
            }
            else
            {
                return _ctx.DelinquentIncome.Where(p => p.InvestorId == investorId && p.MonthDue == monthToCheck)
                                       .OrderByDescending(p => p.MonthDue)
                                       .ThenBy(p => p.TickerSymbol)
                                       .ToList();
                                       //.AsQueryable();
            }
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


        private string FetchInvestorId(string recvdPositionId)
        {
            IQueryable<Data.Entities.Position> positionFound = _ctx.Position.Where(p => p.PositionId == recvdPositionId);
            string assetIdFound = FetchAssetId(positionFound.First().PositionId);
            IQueryable <Data.Entities.Asset> asset = _ctx.Asset.Where(a => a.AssetId == assetIdFound);

            return asset.First().InvestorId;
        }


        private bool RemoveDelinquency(IList<DelinquentIncome> delinquenciesToRemove)
        {
            int recordsRemoved = 0;
            _ctx.RemoveRange(delinquenciesToRemove);
            recordsRemoved = _ctx.SaveChanges();
            return recordsRemoved == 1 ? true : false;
        }


        private bool UpdateTargetPositions(List<Data.Entities.Position> positionsToUpdate)
        {
            _ctx.UpdateRange(positionsToUpdate);
            int positionsUpdated = _ctx.SaveChanges();
            return positionsUpdated >= 1 ? true : false;
        }

    }

}
