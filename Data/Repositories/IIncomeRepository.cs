using System.Collections.Generic;
using PIMS3.Data.Entities;

namespace PIMS3.Data.Repositories
{
    public interface IIncomeRepository
    {
        IEnumerable<Income> GetRevenue();
    }
}