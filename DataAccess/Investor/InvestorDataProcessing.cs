using PIMS3.Data;
using System.Linq;


namespace PIMS3.DataAccess.Investor
{
    public class InvestorDataProcessing
    {
        private readonly PIMS3Context _ctx;
        //private readonly ILogger<IncomeRepository> _logger;

        public InvestorDataProcessing(PIMS3Context ctx) {
            _ctx = ctx;
            //_logger = logger; // to be implemented
        }


        public IQueryable<Data.Entities.Investor> RetreiveAll()
        {
            return _ctx.Investor.Select(i => i).AsQueryable();
        }


        public Data.Entities.Investor RetreiveByName(string investorLoginName)
        {
            return _ctx.Investor
                       .Where(investor => investor.LoginName == investorLoginName.Trim())
                       .FirstOrDefault();
        }


        public bool Update(Data.Entities.Investor investorToUpdate)
        {
            int updateCount = 0;
            _ctx.Update(investorToUpdate);
            updateCount = _ctx.SaveChanges();

            return updateCount == 1
                ? true
                : false;
        }
       

    }

}

