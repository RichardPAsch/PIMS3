using System;
using System.Linq;
using System.Linq.Expressions;
using PIMS3.Data.Entities;

namespace PIMS3.Data.Repositories
{
    public interface IInvestorRepository
    {
        //IQueryable<Investor> Retreive(Expression<Func<Investor, bool>> predicate);
        //IQueryable<Investor> RetreiveAll();
        //Investor RetreiveById(string idGuid);
        string RetreiveIdByName(string investorName);
    }
}