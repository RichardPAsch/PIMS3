using PIMS3.Data.Entities;
using System.Linq;

namespace PIMS3.Services
{
    interface IInvestorSvc
    {
        Investor Authenticate(string username, string password);
        IQueryable<Investor> GetAll();
    }
}
