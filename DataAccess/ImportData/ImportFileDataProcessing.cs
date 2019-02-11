using PIMS3.ViewModels;
using PIMS3.Data;
using PIMS3.BusinessLogic.ImportData;
using System.Collections.Generic;
using System;
using System.Linq;
using PIMS3.DataAccess.Profile;

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


        public DataImportVm SaveRevenue(DataImportVm importVmToUpdate, PIMS3Context _ctx)
        {
            var busLayerComponent = new ImportFileProcessing(importVmToUpdate, _ctx);

            IEnumerable<Data.Entities.Income> revenueListingToSave;

            if (busLayerComponent.ValidateVm())
            {
                revenueListingToSave = busLayerComponent.ParseRevenueSpreadsheetForIncomeRecords(importVmToUpdate.ImportFilePath.Trim(), this);

                if (revenueListingToSave == null)
                {
                    importVmToUpdate.ExceptionTickers = _exceptionTickers;
                    return importVmToUpdate;
                }
                else
                {
                    using (_ctx)
                    {
                        _ctx.AddRange(revenueListingToSave);
                        recordsSaved = _ctx.SaveChanges();
                    }
                }

                if (recordsSaved > 0)
                {
                    totalAmtSaved = 0M;
                    foreach (var record in revenueListingToSave)
                    {
                        totalAmtSaved += record.AmountRecvd;
                    }

                    importVmToUpdate.AmountSaved = totalAmtSaved;
                    importVmToUpdate.RecordsSaved = recordsSaved;
                }
            }

            // Missing amount & record count reflects error condition.
            return importVmToUpdate;
        }


        public DataImportVm SaveAssets(DataImportVm importVmToSave, PIMS3Context _ctx)
        {
            var busLogicComponent = new ImportFileProcessing(importVmToSave, _ctx);

            if (busLogicComponent.ValidateVm())
            {
                assetListingToSave = busLogicComponent.ParsePortfolioSpreadsheetForAssetRecords(importVmToSave.ImportFilePath.Trim(), this);

                if (assetListingToSave == null)
                {
                    // TODO: Populate with any error msg if no exception tickers.
                    importVmToSave.ExceptionTickers = _exceptionTickers;
                    return importVmToSave;
                }
                else
                {
                    var profilesToSave = new List<Data.Entities.Profile>();
                    var assetsToSave = new List<Data.Entities.Asset>();
                    var positionsToSave = new List<Data.Entities.Position>();

                    for(var vmRecordIdx = 0; vmRecordIdx < assetListingToSave.Count(); vmRecordIdx++)
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
                        // Add 'Exception data' to model?
                        var err = ex.InnerException;
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




        /*  Old Code
            public IQueryable<Investor> RetreiveAll() {
            var investorQuery = (from investor in _nhSession.Query<Investor>() select investor);
            return investorQuery.AsQueryable();
             }


            public Investor RetreiveById(Guid idGuid) {
                return _nhSession.Get<Investor>(idGuid);
            }


            public IQueryable<Investor> Retreive(Expression<Func<Investor, bool>> predicate) {
                return RetreiveAll().Where(predicate);
            }

             commented after tfr from PIMS
            public bool SaveOrUpdateProfile(Profile newEntity)
            {
                //using (var trx = _nhSession.BeginTransaction()) {
                //    try {
                //        _nhSession.Save(newEntity);
                //        trx.Commit();
                //    }
                //    catch (Exception ex) {
                //        return false;
                //    }

                return true;
                //}
            }

             commented after tfr from PIMS
            public bool SavePositions(Position[] newPositions)
            {
                //using (var trx = _nhSession.BeginTransaction()) {
                //    try {
                //        _nhSession.Save(newEntity);
                //        trx.Commit();
                //    }
                //    catch (Exception ex) {
                //        return false;
                //    }

                return true;
                //}
            }
            
            public bool Update(Investor entity, object id) {
                using (var trx = _nhSession.BeginTransaction()) {
                    try {
                        _nhSession.Merge(entity);
                        trx.Commit();
                    }
                    catch (Exception) {
                        return false;
                    }
                }

                return true;
            }


            public bool Delete(Guid cGuid) {

                var deleteOk = true;
                var accountTypeToDelete = RetreiveById(cGuid);

                if (accountTypeToDelete == null)
                    return false;


                using (var trx = _nhSession.BeginTransaction()) {
                    try {
                        _nhSession.Delete(accountTypeToDelete);
                        trx.Commit();
                    }
                    catch (Exception) {
                        deleteOk = false;
                    }
                }

                return deleteOk;
            }
        */


    }
   
}



