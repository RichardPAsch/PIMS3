﻿using PIMS3.Data;
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
        // TODO: consider REFACTOR for UpdatePositionPymtDueFlags() into BL ?

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


        public bool UpdatePositionPymtDueFlags(List<PositionsForPaymentDueVm> sourcePositionsInfo, bool isPersisted = false)
        {
             // Received sourcePositionsInfo may contain either:
             //  1. data-imported month-end XLSX revenue processed Position Ids, OR
             //  2. selected 'Income Due' Positions marked for pending payment processing; selection(s) may also include * delinquent positions *.
            bool updatesAreOk = false;
            List<Data.Entities.Position> targetPositionsToUpdate = new List<Data.Entities.Position>();
            IList<DelinquentIncome> delinquentPositions = new List<DelinquentIncome>();

            int positionsUpdatedCount = 0; 

            // Parameter context:
            // isPersisted: false - income received, but not yet recorded/saved, e.g., processing payment(s) via 'Income Due'.
            // isPersisted: true  - income received & recorded/saved, and is now eligible for next receivable cycle, e.g., XLSX via 'Data Import'.
            if (!isPersisted)
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
                int positionsNotUpdatedCount = 0;

                // Loop thru each XLSX position within the month-end collection & determine if it's eligible for updating its' 'PymtDue' flag.
                // debug -> existing PosId delinquency: 0481727F-737E-4775-870A-A8F20118F977 for PICB
                foreach (PositionsForPaymentDueVm xlsxPosition in sourcePositionsInfo)
                {
                    IList<DelinquentIncome> existingDelinquentPositions = delinquentPositions.Where(p => p.PositionId == xlsxPosition.PositionId).ToList();
                    if (existingDelinquentPositions.Any())
                    {
                        positionsNotUpdatedCount++;
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

                if (targetPositionsToUpdate.Any())
                {
                    _ctx.UpdateRange(targetPositionsToUpdate);
                    positionsUpdatedCount = _ctx.SaveChanges();

                    if (positionsUpdatedCount + positionsNotUpdatedCount == sourcePositionsInfo.Count())
                        updatesAreOk = true;
                }
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

            // p1: join table, p2&p3: join PKs, p4: projection form.
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

        
        public IList<DelinquentIncome> GetSavedDelinquentRecords(string investorId, string monthToCheck = "")
        {
            // Positions with delinquent payments.
            if (string.IsNullOrEmpty(monthToCheck))
            {
                return _ctx.DelinquentIncome.Where(p => p.InvestorId == investorId)
                           .Join(_ctx.Profile, d => d.TickerSymbol, pr => pr.TickerSymbol, (di, pr) => new DelinquentIncome
                           {
                                TickerSymbol = di.TickerSymbol,
                                MonthDue = di.MonthDue,
                                DividendFreq = pr.DividendFreq,
                                PositionId = di.PositionId
                           })
                           .ToList();
            }
            else
            {
                return _ctx.DelinquentIncome.Where(p => p.InvestorId == investorId && p.MonthDue == monthToCheck)
                           .Join(_ctx.Profile, d => d.TickerSymbol, pr => pr.TickerSymbol, (di, pr) => new DelinquentIncome
                           {
                                TickerSymbol = di.TickerSymbol,
                                MonthDue = di.MonthDue,
                                DividendFreq = pr.DividendFreq,
                                PositionId = di.PositionId
                           })
                           .ToList();
            }
        }


        public bool SavePositionsWithOverdueIncome(List<DelinquentIncome> delinquentPositions)
        {
            _ctx.AddRange(delinquentPositions);
            recordsSaved = _ctx.SaveChanges();

            return recordsSaved > 0 ? true : false;
        }


        public bool SaveDelinquencies(List<DelinquentIncome> pastDuePositionsToSave)
        {
            _ctx.AddRange(pastDuePositionsToSave);
            recordsSaved = _ctx.SaveChanges();

            return recordsSaved > 0 ? true : false;
        }

               
        public IQueryable<PositionsForEditVm> GetPositions(string investorId, bool includeInactiveStatusRecs)
        {
            // Explicitly querying for specific statuses, as other statuses may be used in future versions.
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


        public IQueryable<dynamic> GetCandidateDelinquentPositions(string currentInvestorId)
        {
            IQueryable<dynamic> candidatePositions = _ctx.Position.Where(p => p.Status == "A" && 
                                                               p.PositionAsset.InvestorId == currentInvestorId &&
                                                               p.PymtDue == true)
                                                    .Join(_ctx.Asset, p => p.AssetId, a => a.AssetId, (position,asset) => new {
                                                                      position.PositionAsset.Profile.DividendFreq,
                                                                      asset.Profile.TickerSymbol,
                                                                      position.PositionAsset.Profile.DividendMonths,
                                                                      position.PositionId,
                                                                      asset.InvestorId
                                                     })
                                                    .AsQueryable();
            
            return candidatePositions;
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


        // Data used for lookup/dropdown functionality in 'Positions' grid ONLY.
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
