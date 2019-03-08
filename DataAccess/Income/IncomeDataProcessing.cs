using System;
using System.Linq;
using PIMS3.ViewModels;


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


        public IQueryable<IncomeSavedVm> GetRevenueHistory(int yearsToBackDate, string investorId)
        {
            var currentYear = DateTime.Now.Year;
            var fromYear = currentYear - yearsToBackDate;

            var income = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId && p.Status == "A")
                                      .SelectMany(i => i.Incomes)
                                      .Where(i => i.DateRecvd >= Convert.ToDateTime("1/1/" + fromYear.ToString()) &&
                                                  i.DateRecvd <= Convert.ToDateTime("12/31/" + currentYear.ToString()))
                                      .AsQueryable();

            var positions = _ctx.Position.Select(p => p.PositionAsset)
                                .SelectMany(a => a.Positions)
                                .AsQueryable();

            var joinData = income.Join(positions, p => p.PositionId, i => i.PositionId, (incomeInfo, positionInfo) => new IncomeSavedVm
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


    }
}
