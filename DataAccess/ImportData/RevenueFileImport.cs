using PIMS3.ViewModels;
using PIMS3.Data;
using PIMS3.BusinessLogic.ImportData;
using System.Collections.Generic;

namespace PIMS3.DataAccess.ImportData
{
    // Mimics/replaces Repository/UoW pattern, as EF Core already implements a Repository/UoW pattern internally.
    // All CRUD operations are functionality-specific (e.g., revenue import file) and utilize direct use of EF Core commands.
    // No need to segregate Read-Only operations (via QueryObjects) at this time.

    public class RevenueFileImport
    {
        private readonly PIMS3Context _ctx;

        public RevenueFileImport(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public RevenueFileImport()
        {
        }


        public DataImportVm SaveRevenue(DataImportVm importVmToUpdate)
        {
            var processingSvc = new RevenueFileProcessing(importVmToUpdate);
            IEnumerable<Data.Entities.Income> revenueListingToSave;
            var recordsSaved = 0;
            var totalAmtSaved = 0M;

            if (processingSvc.ValidateVm())
            {
                revenueListingToSave = processingSvc.ParseRevenueSpreadsheetForIncomeRecords(importVmToUpdate.ImportFilePath.Trim());

                using (_ctx) {
                    _ctx.AddRange(revenueListingToSave);
                    recordsSaved = _ctx.SaveChanges();
                }

                if(recordsSaved > 0)
                {
                    foreach (var record in revenueListingToSave)
                    {
                        totalAmtSaved += totalAmtSaved + record.AmountRecvd;
                    }

                    importVmToUpdate.AmountSaved = totalAmtSaved;
                    importVmToUpdate.RecordsSaved = recordsSaved;
                }
            }

            // Missing amount & record count reflects error condition.
            return importVmToUpdate;
        }

        //public IQueryable<Investor> RetreiveAll() {
        //    var investorQuery = (from investor in _nhSession.Query<Investor>() select investor);
        //    return investorQuery.AsQueryable();
        //}


        //public Investor RetreiveById(Guid idGuid) {
        //    return _nhSession.Get<Investor>(idGuid);
        //}


        //public IQueryable<Investor> Retreive(Expression<Func<Investor, bool>> predicate) {
        //    return RetreiveAll().Where(predicate);
        //}

        // commented after tfr from PIMS
        //public bool SaveOrUpdateProfile(Profile newEntity)
        //{
        //    //using (var trx = _nhSession.BeginTransaction()) {
        //    //    try {
        //    //        _nhSession.Save(newEntity);
        //    //        trx.Commit();
        //    //    }
        //    //    catch (Exception ex) {
        //    //        return false;
        //    //    }

        //    return true;
        //    //}
        //}

        // commented after tfr from PIMS
        //public bool SavePositions(Position[] newPositions)
        //{
        //    //using (var trx = _nhSession.BeginTransaction()) {
        //    //    try {
        //    //        _nhSession.Save(newEntity);
        //    //        trx.Commit();
        //    //    }
        //    //    catch (Exception ex) {
        //    //        return false;
        //    //    }

        //    return true;
        //    //}
        //}



        //public bool Update(Investor entity, object id) {
        //    using (var trx = _nhSession.BeginTransaction()) {
        //        try {
        //            _nhSession.Merge(entity);
        //            trx.Commit();
        //        }
        //        catch (Exception) {
        //            return false;
        //        }
        //    }

        //    return true;
        //}


        //public bool Delete(Guid cGuid) {

        //    var deleteOk = true;
        //    var accountTypeToDelete = RetreiveById(cGuid);

        //    if (accountTypeToDelete == null)
        //        return false;


        //    using (var trx = _nhSession.BeginTransaction()) {
        //        try {
        //            _nhSession.Delete(accountTypeToDelete);
        //            trx.Commit();
        //        }
        //        catch (Exception) {
        //            deleteOk = false;
        //        }
        //    }

        //    return deleteOk;
        //}

    }
}



