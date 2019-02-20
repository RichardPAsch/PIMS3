using System;
using System.Linq;


namespace PIMS3.DataAccess.IncomeData
{
    public class IncomeDataProcessing
    {
        private readonly Data.PIMS3Context _ctx;

        public IncomeDataProcessing(Data.PIMS3Context ctx)
        {
            _ctx = ctx;
        }


        public IQueryable<Data.Entities.Income> FindIncomeDuplicates(string positionId, string dateRecvd, string amountRecvd)
        {
            // No investorId needed, as PositionId will be unique to investor.
            var duplicateRecords = _ctx.Income
                                       .Where(i => i.PositionId == positionId.Trim()
                                                && i.DateRecvd == DateTime.Parse(dateRecvd.Trim())
                                                && i.AmountRecvd == decimal.Parse(amountRecvd.Trim()))
                                       .AsQueryable();

            return duplicateRecords;
        }


    }
}
