using PIMS3.Data;
using System.Linq;
using System;

namespace PIMS3.DataAccess.Income
{
    public class IncomeData
    {
        private readonly PIMS3Context _ctx;


        public IncomeData(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public IncomeData()
        {
        }


        public IQueryable<Data.Entities.Income> FindIncomeDuplicates(string positionId, string dateRecvd, string amountRecvd)
        {
            // 11.14.18 - No investorId needed, as PositionId will be unique to investor.
            //var currentInvestor = "rpasch@rpclassics.net";

            var duplicateIncomes = _ctx.Income
                                       .Where(i => i.PositionId == positionId.Trim()
                                                 && i.DateRecvd == DateTime.Parse(dateRecvd.Trim())
                                               && i.AmountRecvd == decimal.Parse(amountRecvd.Trim()))
                                       .AsQueryable();

            //var duplicateIncomes = await Task.FromResult(_repositoryAsset.Retreive(a => a.InvestorId == Utilities.GetInvestorId(_repositoryInvestor, currentInvestor.Trim()))
            //                                                             .SelectMany(r => r.Revenue)
            //                                                             .AsQueryable()
            //                                                             .Where(r => r.IncomePositionId == Guid.Parse(positionId.Trim())
            //                                                                      && r.DateRecvd == DateTime.Parse(dateRecvd.Trim())
            //                                                                      && r.Actual == decimal.Parse(amountRecvd.Trim()))
            //);

            return duplicateIncomes;
        }



    }
}
