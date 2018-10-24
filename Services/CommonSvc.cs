using PIMS3.Data;
using PIMS3.Data.Repositories;


namespace PIMS3.Services
{
    public class CommonSvc : ICommonSvc
    {
        private readonly PIMS3Context _ctx;
        private readonly InvestorRepository _repo;

        public CommonSvc(PIMS3Context ctx, InvestorRepository repo)
        {
            _ctx = ctx;
            _repo = repo;
        }


        public string GetInvestorIdFromInvestor(string currentInvestor)
        {
          return _repo.RetreiveIdByName(currentInvestor);
        }
    }
}
