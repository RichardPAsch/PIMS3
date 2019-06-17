using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.ViewModels;
using PIMS3.Data.Entities;


namespace PIMS3.DataAccess.IncomeData
{
    public class IncomeDataProcessing
    {
        private readonly Data.PIMS3Context _ctx;

        public IncomeDataProcessing(Data.PIMS3Context ctx)
        {
            _ctx = ctx;
        }


        public IQueryable<Income> FindIncomeDuplicates(string positionId, string dateRecvd, string amountRecvd)
        {
            // No investorId needed, as PositionId will be unique to investor.
            var duplicateRecords = _ctx.Income
                                       .Where(i => i.PositionId == positionId.Trim()
                                                && i.DateRecvd == DateTime.Parse(dateRecvd.Trim())
                                                && i.AmountRecvd == decimal.Parse(amountRecvd.Trim()))
                                       .AsQueryable();

            return duplicateRecords;
        }


        public IQueryable<IncomeSavedVm> GetRevenueHistory(int yearsToBackDate, string investorId)
        {
            var currentYear = DateTime.Now.Year;
            var fromYear = currentYear - yearsToBackDate;

            IQueryable<Income> income = _ctx.Income.Where(i => i.DateRecvd >= Convert.ToDateTime("1/1/" + fromYear.ToString()) &&
                                                               i.DateRecvd <= Convert.ToDateTime("12/31/" + currentYear.ToString()));

            // Ignoring Position status, as "I"nactive positions would give erroneous results when compared to 'Income Summary'.
            IQueryable<Data.Entities.Position> positions = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId );

            IQueryable <IncomeSavedVm> joinData = income.Join(positions, p => p.PositionId, i => i.PositionId, (incomeInfo, positionInfo) => new IncomeSavedVm
            {
                TickerSymbol = positionInfo.PositionAsset.Profile.TickerSymbol,
                AccountTypeDesc = positionInfo.AccountType.AccountTypeDesc,
                DividendFreq = positionInfo.PositionAsset.Profile.DividendFreq,
                DateRecvd = incomeInfo.DateRecvd,
                AmountReceived = incomeInfo.AmountRecvd,
                IncomeId = incomeInfo.IncomeId
            })
            .AsQueryable();

            return joinData.OrderByDescending(i => i.DateRecvd)
                           .ThenBy(i => i.TickerSymbol);
        }


        public int UpdateRevenue(IncomeForEditVm[] editedRevenue)
        {
            var revenueToUpdateListing = new List<Income>();
            int updateCount = 0;

            // Get existing Income for pending updates.
            foreach (IncomeForEditVm incomeRecord in editedRevenue)
                revenueToUpdateListing.Add(_ctx.Income.Where(i => i.IncomeId == incomeRecord.IncomeId).FirstOrDefault());
               

            revenueToUpdateListing.OrderBy(i => i.IncomeId);
            editedRevenue.OrderBy(i => i.IncomeId);

            // Update.
            if (revenueToUpdateListing.Count() == editedRevenue.Length)
            {
                for(int i = 0; i < revenueToUpdateListing.Count; i++)
                {
                    if (revenueToUpdateListing.ElementAt(i).IncomeId == editedRevenue.ElementAt(i).IncomeId)
                    {
                        revenueToUpdateListing.ElementAt(i).DateRecvd = editedRevenue.ElementAt(i).DateRecvd;
                        revenueToUpdateListing.ElementAt(i).AmountRecvd = editedRevenue.ElementAt(i).AmountReceived;
                        revenueToUpdateListing.ElementAt(i).LastUpdate = DateTime.Now;
                    }
                }
               
                _ctx.UpdateRange(revenueToUpdateListing);
                updateCount = _ctx.SaveChanges();
            }

            return updateCount;
        }
    }
}
