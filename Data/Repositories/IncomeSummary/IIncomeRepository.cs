using System.Collections.Generic;
using PIMS3.Data.Entities;

namespace PIMS3.Data.Repositories.IncomeSummary
{
    public interface IIncomeRepository
    {
        IEnumerable<Income> GetRevenue();
    }
}