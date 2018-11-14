using Microsoft.Extensions.Logging;
using PIMS3.Data.Entities;
using PIMS3.Data.Repositories.IncomeSummary;
using System;
using System.Linq;
using System.Linq.Expressions;



namespace PIMS3.Data.Repositories
{
    public class InvestorRepository : IInvestorRepository
    {
        private readonly PIMS3Context _ctx;
        private readonly ILogger<IncomeRepository> _logger;

        public InvestorRepository(PIMS3Context ctx, ILogger<IncomeRepository> logger) {
            _ctx = ctx;
            _logger = logger;
        }


        public string RetreiveIdByName(string investorName)
        {
            //var fetchedInvestor = Retreive(i => i.EMailAddr.Trim() == investorName.Trim());
            //return fetchedInvestor.FirstOrDefault().InvestorId.ToString();
            
            return _ctx.Investor
                       .Where(investor => investor.EMailAddr.Trim() == investorName.Trim())
                       .FirstOrDefault().InvestorId
                       .ToString();
        }




        // ** 9.17.18 - To be implemented, pending security implementation ? Any changes made need to be added to interface!  **

        /*
           public IQueryable<Investor> RetreiveAll()
           {
               var investorQuery = (from investor in _nhSession.Query<Investor>() select investor);
               return investorQuery.AsQueryable();
               return null;
           }

           public Investor RetreiveById(string idGuid)
           {
               //return _nhSession.Get<Investor>(idGuid);
               return null;
           }


           public IQueryable<Investor> Retreive(Expression<Func<Investor, bool>> predicate)
           {
               return RetreiveAll().Where(predicate);
           }

           public bool Create(Investor newEntity) {
               return false;
           }


           public bool Update(Investor entity, object id) {


               return true;
           }


           public bool Delete(Guid cGuid) {

               var deleteOk = true;
               var accountTypeToDelete = RetreiveById(cGuid);

               if (accountTypeToDelete == null)
                   return false;




               return deleteOk;
           }
       */

       }

    }

