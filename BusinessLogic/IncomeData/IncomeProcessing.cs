using PIMS3.Data;


namespace PIMS3.BusinessLogic.Income
{
    public class IncomeProcessing
    {
        private readonly PIMS3Context _ctx;


        public IncomeProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        
        // moved to IncomeDataProcessing.
        //public IQueryable<Data.Entities.Income> FindIncomeDuplicates(string positionId, string dateRecvd, string amountRecvd)
        //{
        //    // No investorId needed, as PositionId will be unique to investor.
        //    var duplicateRecords = _ctx.Income
        //                               .Where(i => i.PositionId == positionId.Trim()
        //                                        && i.DateRecvd == DateTime.Parse(dateRecvd.Trim())
        //                                        && i.AmountRecvd == decimal.Parse(amountRecvd.Trim()))
        //                               .AsQueryable();

        //    return duplicateRecords;
        //}

       

    }
}





