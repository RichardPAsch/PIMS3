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

        // Needed ?
        //public IncomeData()
        //{
        //}


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
