using PIMS3.ViewModels;
using PIMS3.Data;
using PIMS3.BusinessLogic.ImportData;
using System.Collections.Generic;
using System;
using System.Linq;
using PIMS3.DataAccess.Profile;
using PIMS3.DataAccess.Position;
using Serilog;
using PIMS3.Data.Entities;


namespace PIMS3.DataAccess.ImportData
{
    // Mimics/replaces Repository/UoW pattern, as EF Core already implements a Repository/UoW pattern internally.
    // All CRUD operations are functionality-specific (e.g., revenue import file) and utilize direct use of EF Core commands.
    // No need to segregate Read-Only operations (via QueryObjects) at this time.

    public class ImportFileDataProcessing
    {
        public string _exceptionTickers = string.Empty;
        private decimal totalAmtSaved = 0M;
        private int recordsSaved = 0;
        private string tickersProcessed = string.Empty;
        IEnumerable<AssetCreationVm> assetListingToSave = null;
        private ProfileDataProcessing profileDataAccessComponent;

        public ImportFileDataProcessing()
        {
        }


        public DataImportVm SaveRevenue(DataImportVm importVmToUpdate, PIMS3Context _ctx, string investorId)
        {
            // Processing includes:
            //  1. persisting revenue,
            //  2. persisting revenue delinquencies,  and
            //  3. updating Positions.

            ImportFileProcessing busLayerComponent = new ImportFileProcessing(importVmToUpdate, _ctx, null);
            IEnumerable<Income> revenueListingToSave;

            if (busLayerComponent.ValidateVm())
            {
                revenueListingToSave = busLayerComponent.ParseRevenueSpreadsheetForIncomeRecords(importVmToUpdate.ImportFilePath.Trim(), this, investorId);

                if (revenueListingToSave == null || revenueListingToSave.Count() == 0)
                {
                    if (!string.IsNullOrEmpty(importVmToUpdate.ExceptionTickers))
                        importVmToUpdate.MiscMessage = "Error saving revenue for " + importVmToUpdate.ExceptionTickers + ". Check position(s).";
                    else
                        importVmToUpdate.MiscMessage = BuildLogMessage("revenue");

                    return importVmToUpdate;
                }
                else
                {
                    // Persist to 'Income'. Deferring use of using{}: ctx scope needed.
                    _ctx.AddRange(revenueListingToSave); 
                    recordsSaved = _ctx.SaveChanges();
                }

                if (recordsSaved > 0)
                {
                    totalAmtSaved = 0M;
                    foreach (var record in revenueListingToSave)
                    {
                        totalAmtSaved += record.AmountRecvd;
                    }

                    // Returned values to caller.
                    importVmToUpdate.AmountSaved = totalAmtSaved;
                    importVmToUpdate.RecordsSaved = recordsSaved;


                    /* === Revenue delinquency processing === */

                    // If at month end/beginning, we'll query Positions for any now delinquent income receivables via "PymtDue" flag.
                    // Any unpaid income receivables, those not marked as received via 'Income due', will STILL have their 'PymtDue' flag set as
                    // True, and hence will now be considered a delinquent Position, resulting in persistence to 'DelinquentIncome' table. 
                    // Delinquent position searches may ONLY be performed during month-end xLSX processing, as income receipts are still being
                    // accepted during the month via 'Income due'. During 'Income due' payments, flags are set accordingly: [PymtDue : True -> False].
                    // Any delinquent Positions will automatically be added to the 'Income due' collection that is broadcast via the UI schedule, 
                    // for necessary action.
                    if (DateTime.Now.Day <= 3 && DateTime.Now.DayOfWeek.ToString() != "Saturday" && DateTime.Now.DayOfWeek.ToString() != "Sunday")
                    {
                        PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_ctx); 
                        IQueryable<dynamic> filteredPositionAssetJoinData = positionDataAccessComponent.GetCandidateDelinquentPositions(investorId);
                        IList<DelinquentIncome> pastDueListing = new List<DelinquentIncome>();
                        string delinquentMonth = DateTime.Now.AddMonths(-1).Month.ToString().Trim();
                        int matchingPositionDelinquencyCount = 0;
                        IList<DelinquentIncome> existingDelinqencies = positionDataAccessComponent.GetSavedDelinquentRecords(investorId, "");
                        int duplicateDelinquencyCount = 0;

                        foreach (dynamic joinData in filteredPositionAssetJoinData)
                        {
                            // 'DelinquentIncome' PKs: InvestorId, MonthDue, PositionId
                            // Do we have a match based on PositionId & InvestorId ? If so, Position is eligible for omitting setting 'PymtDue' to True.
                            matchingPositionDelinquencyCount = existingDelinqencies.Where(d => d.PositionId == joinData.PositionId
                                                                                            && d.InvestorId == joinData.InvestorId).Count();

                            if(matchingPositionDelinquencyCount >= 1)
                            {
                                // Do we have a duplicate entry ?
                                duplicateDelinquencyCount = existingDelinqencies.Where(d => d.PositionId == joinData.PositionId
                                                                                         && d.InvestorId == joinData.InvestorId
                                                                                         && d.MonthDue == delinquentMonth).Count();
                            }

                        
                            if (joinData.DividendFreq == "M")
                            {
                                if (matchingPositionDelinquencyCount > 0 && duplicateDelinquencyCount == 0)
                                    pastDueListing.Add(InitializeDelinquentIncome(joinData));
                                else
                                    continue;
                            }
                            else
                            {
                                string[] divMonths = joinData.DividendMonths.Split(',');
                                for (var i = 0; i < divMonths.Length; i++)
                                {
                                    if (divMonths[i].Trim() == delinquentMonth)
                                    {
                                        if (matchingPositionDelinquencyCount > 0 && duplicateDelinquencyCount == 0)
                                            pastDueListing.Add(InitializeDelinquentIncome(joinData));
                                        else
                                            continue;
                                    }
                                }
                            }
                            matchingPositionDelinquencyCount = 0;
                        }

                        // Persist to 'DelinquentIncome' as needed. 
                        if (pastDueListing.Any())
                        {
                            bool pastDueSaved = positionDataAccessComponent.SaveDelinquencies(pastDueListing.ToList());
                            if (pastDueSaved)
                            {
                                importVmToUpdate.MiscMessage = "Found & stored " + pastDueListing.Count() + " Position(s) with delinquent revenue.";
                                Log.Information("Saved {0} delinquent position(s) to 'DelinquentIncome' via ImportFileDataProcessing.SaveRevenue()", pastDueListing.Count());
                            }
                        }

                        // Finally, update PymtDue flags on received XLSX positions.
                        positionDataAccessComponent.UpdatePositionPymtDueFlags(ExtractPositionIdsForPymtDueProcessing(revenueListingToSave), true);
                    }
                }
                _ctx.Dispose();
            }

            // Missing amount & record count reflects error condition.
            return importVmToUpdate;
        }


        public DataImportVm SaveAssets(DataImportVm importVmToSave, PIMS3Context _ctx, string id)
        {
            ImportFileProcessing busLogicComponent = new ImportFileProcessing(importVmToSave, _ctx, null);

            if (busLogicComponent.ValidateVm())
            {
                assetListingToSave = busLogicComponent.ParsePortfolioSpreadsheetForAssetRecords(importVmToSave.ImportFilePath.Trim(), this, id);

                if (assetListingToSave == null || assetListingToSave.Count() == 0)
                {
                    if (!string.IsNullOrEmpty(importVmToSave.ExceptionTickers))
                        importVmToSave.MiscMessage = "Error saving position(s) for " + importVmToSave.ExceptionTickers + ". Check position(s)";
                    else
                        importVmToSave.MiscMessage = BuildLogMessage("position(s)");

                    return importVmToSave;
                }
                else
                {
                    List<Data.Entities.Profile> profilesToSave = new List<Data.Entities.Profile>();
                    List<Data.Entities.Asset> assetsToSave = new List<Data.Entities.Asset>();
                    List<Data.Entities.Position> positionsToSave = new List<Data.Entities.Position>();

                    for(int vmRecordIdx = 0; vmRecordIdx < assetListingToSave.Count(); vmRecordIdx++)
                    {
                        if (assetListingToSave.ElementAt(vmRecordIdx).Profile != null)
                            profilesToSave.Add(MapVmToEntities(assetListingToSave.ElementAt(vmRecordIdx).Profile) as Data.Entities.Profile);
                        else
                            profileDataAccessComponent = new ProfileDataProcessing(_ctx);

                        // "Asset" must first be initialized before referenced "Positions" can be added.
                        assetsToSave.Add(MapVmToEntities(assetListingToSave.ElementAt(vmRecordIdx)) as Data.Entities.Asset);

                        positionsToSave.Clear();
                        for (var position = 0; position < assetListingToSave.ElementAt(vmRecordIdx).Positions.Count(); position++)
                        {
                            positionsToSave.Add(MapVmToEntities(assetListingToSave.ElementAt(vmRecordIdx).Positions.ElementAt(position)) as Data.Entities.Position);
                            assetsToSave.ElementAt(vmRecordIdx).Positions.Add(positionsToSave.ElementAt(position));
                        }
                    }
                   
                    // Persist to PIMS2Db.
                    try
                    {
                        // Omitting "using{}": DI handles disposing of ctx; *non-disposed ctx* needed for later 
                        // call to profileDataAccessComponent.FetchDbProfileTicker().
                        if (profilesToSave.Count > 0)
                            _ctx.AddRange(profilesToSave);  

                        _ctx.AddRange(assetsToSave);            
                        recordsSaved = _ctx.SaveChanges(); 
                    }
                    catch (Exception ex)
                    {
                        Exception err = ex.InnerException;
                        Log.Error("Error persisting Profile(s)/Asset(s) to database within ImportFileDataProcessing.SaveAssets() due to {0}", err.Message);
                        return null;
                    }
                }
            }

            return HandleDbProcessingResults(importVmToSave, null, assetListingToSave);
        }


        private DataImportVm HandleDbProcessingResults(DataImportVm vmToProcess, IEnumerable<Data.Entities.Income> incomeListing,
                                                                                 IEnumerable<AssetCreationVm> assetListing) {

            totalAmtSaved = 0M;
            recordsSaved = 0;

            if (assetListing != null)
            {
                for(var record = 0; record < assetListing.Count(); record++)
                {
                    recordsSaved += 1;
                    if (recordsSaved == 1)
                    {
                        if(assetListing.ElementAt(record).Profile != null)
                            tickersProcessed = assetListing.ElementAt(record).Profile.TickerSymbol.ToUpper();
                        else
                        {
                            // Db Profile already exists, hence no Profile instance in vm with a ProfileId.
                            var id = assetListing.ElementAt(record).ProfileId;
                            tickersProcessed = profileDataAccessComponent.FetchDbProfileTicker(id).First();
                        }
                    }
                    else
                        tickersProcessed += ", " + assetListing.ElementAt(record).Profile.TickerSymbol.ToUpper();
                }
            }
            else
            {
                foreach (var record in incomeListing)
                {
                    recordsSaved += 1;
                    totalAmtSaved += record.AmountRecvd;
                }
                vmToProcess.AmountSaved = totalAmtSaved;
            }

            vmToProcess.RecordsSaved = recordsSaved;
            vmToProcess.MiscMessage = tickersProcessed;

            return vmToProcess;
        }


        private object MapVmToEntities(dynamic entityToMap){

            dynamic currentType = null;
            switch (entityToMap.GetType().Name)
            {
                case "Profile":
                    currentType = new Data.Entities.Profile
                    {
                        ProfileId = entityToMap.ProfileId,  // test this; entityToMap.ProfileId should be dynamically valid ?
                        CreatedBy = entityToMap.CreatedBy ?? string.Empty,
                        DividendFreq = entityToMap.DividendFreq,
                        DividendMonths = "NA",
                        DividendPayDay = entityToMap.DividendPayDay,
                        DividendRate = entityToMap.DividendRate,
                        DividendYield = entityToMap.DividendYield > 0
                            ? entityToMap.DividendYield
                            : 0M,
                        EarningsPerShare = entityToMap.EarningsPerShare > 0 
                            ? entityToMap.EarningsPerShare
                            : 0M,
                        LastUpdate = entityToMap.LastUpdate ?? DateTime.Now,
                        PERatio = entityToMap.PERatio > 0 
                            ? entityToMap.PERatio
                            : 0M,
                        TickerDescription = entityToMap.TickerDescription,
                        TickerSymbol = entityToMap.TickerSymbol,
                        UnitPrice = entityToMap.UnitPrice
                    };
                    break;
                case "AssetCreationVm":
                    currentType = new Data.Entities.Asset
                    {
                        AssetId = entityToMap.AssetId,
                        AssetClassId = entityToMap.AssetClassId,
                        InvestorId = entityToMap.InvestorId,
                        LastUpdate = entityToMap.LastUpdate ?? DateTime.Now,
                        ProfileId = entityToMap.ProfileId,
                        Positions = new List<Data.Entities.Position>()
                    };
                    break;
                case "Position":
                    currentType = new Data.Entities.Position
                    {
                        PositionId = entityToMap.PositionId,
                        AccountTypeId = entityToMap.AccountTypeId,
                        AssetId = entityToMap.AssetId,  // value needed
                        Fees = entityToMap.Fees > 0
                            ? entityToMap.Fees
                            : 0M,
                        LastUpdate = entityToMap.LastUpdate ?? DateTime.Now,
                        PositionDate = entityToMap.PositionDate ?? DateTime.Now,
                        Quantity = entityToMap.Quantity,
                        Status = "A",
                        UnitCost = entityToMap.UnitCost
                    };
                    break;
                default:
                    break;
            }
            return currentType;
        }


        private List<PositionsForPaymentDueVm> ExtractPositionIdsForPymtDueProcessing(IEnumerable<Income> incomeCollection)
        {
            // Use successfully saved income records to gather positionIds, which in turn will be used for updating corresponding
            // Position records with: 'PymtDue = true'.
            List<PositionsForPaymentDueVm> xlsxPositions = new List<PositionsForPaymentDueVm>();
            for(var i = 0; i < incomeCollection.Count(); i++)
            {
                // Only PositionIds needed.
                PositionsForPaymentDueVm posVm = new PositionsForPaymentDueVm
                {
                    PositionId = incomeCollection.ElementAt(i).PositionId
                };

                xlsxPositions.Add(posVm);
            }

            return xlsxPositions;
        }


        private string BuildLogMessage(string msgContext)
        {
            return string.Format("Error saving {0}. Bad data import xlsx format (columns, data), xlsx file path, or faulty network connectivity.", msgContext);
        }


        private DelinquentIncome MapVmToDelinquentIncome(IncomeReceivablesVm mapSource, string investorId)
        {
            return new DelinquentIncome
            {
                PositionId = mapSource.PositionId,
                MonthDue = mapSource.MonthDue.ToString(),
                InvestorId = investorId,
                TickerSymbol = mapSource.TickerSymbol
            };
        }


        private DelinquentIncome InitializeDelinquentIncome(dynamic delinquency)
        {
            return new DelinquentIncome
            {
                TickerSymbol = delinquency.TickerSymbol,
                PositionId = delinquency.PositionId,
                InvestorId = delinquency.InvestorId,
                MonthDue = DateTime.Now.AddMonths(-1).Month.ToString()
            };

        }

    }
   
}



