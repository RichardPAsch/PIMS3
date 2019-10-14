using Microsoft.Extensions.Logging;
using PIMS3.Data.Entities;
using PIMS3.Services;
using System;
using System.Collections.Generic;
using System.Linq;


namespace PIMS3.Data.Repositories.IncomeSummary
{
    // ** TODO: Make interface available to appropriate controller!
    //          Refer to existing PIMS source for transfer.
    //          "Launch browser" unchecked in project/debug settings in order to directly call/test API.

    public class IncomeRepository : IIncomeRepository
    {
        private readonly PIMS3Context _ctx;
        private readonly ILogger<IncomeRepository> _logger;
        private readonly ICommonSvc _commonSvc;

        public IncomeRepository(PIMS3Context ctx, ILogger<IncomeRepository> logger, ICommonSvc commonSvc)
        {
            _ctx = ctx;
            _logger = logger;
            _commonSvc = commonSvc;
        }


        public IEnumerable<Income> GetRevenueSummaryForYear(int yearsBackDated, string investorId)
        {
            var fromDate = new DateTime(DateTime.UtcNow.AddYears(-yearsBackDated).Year, 1, 1);
            var toDate = new DateTime(DateTime.UtcNow.AddYears(-yearsBackDated).Year, 12, 31);

            try
            {
                var positions = _ctx.Asset
                               .Where(a => a.InvestorId == investorId) 
                               .AsQueryable()
                               .SelectMany(a => a.Positions);
                var revenue = positions.SelectMany(p => p.Incomes)
                               .Where(i => i.DateRecvd >= fromDate && i.DateRecvd <= toDate)
                               .AsQueryable();

                return revenue.OrderBy(i => i.DateRecvd).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetRevenue() failed due to: {ex}");
                return null;
            }


            
        }
    }
}
