using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.ViewModels;
using PIMS3.Data.Entities;
using PIMS3.Services;


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


        public int DeleteRevenue(string[] revenueToBeDeleted) // return an Observable<int> ?
        {
            var revenueToDeleteListing = new List<Income>();

            for (int i = 0; i < revenueToBeDeleted.Length; i++)
            {
                revenueToDeleteListing.Add(new Income
                {
                    IncomeId = revenueToBeDeleted.ElementAt(i)
                });
            }

            _ctx.RemoveRange(revenueToDeleteListing);
            return _ctx.SaveChanges();
        }


        public ProfileForUpdateVm Process12MosRevenueHistory(string investorId)
        {
            // Revenue processing in response to investor-initiated Profile updating.
            ProfileForUpdateVm vm = new ProfileForUpdateVm();

            try
            {
                string exceptionTickers = string.Empty;

                IQueryable<string> currentPositions = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId && p.Status == "A")
                                                                   .Select(p => p.PositionAsset.Profile.TickerSymbol)
                                                                   .Distinct()
                                                                   .AsQueryable();

                IQueryable<PositionIncomeVm> joinedPositionIncome = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId && p.Status == "A")
                                                       .Join(_ctx.Income, p => p.PositionId, i => i.PositionId,
                                                                (p, i) => new PositionIncomeVm
                                                                {
                                                                    ProfileId = p.PositionAsset.Profile.ProfileId,
                                                                    PositionId = p.PositionId,
                                                                    TickerSymbol = p.PositionAsset.Profile.TickerSymbol,
                                                                    Account = p.AccountType.AccountTypeDesc,
                                                                    DateRecvd = i.DateRecvd,
                                                                    DividendYield = Convert.ToDecimal(p.PositionAsset.Profile.DividendYield),
                                                                    TickerDescription = p.PositionAsset.Profile.TickerDescription
                                                                })
                                                       .OrderBy(results => results.TickerSymbol)
                                                       .ThenBy(results => results.Account)
                                                       .AsQueryable();

                joinedPositionIncome.OrderBy(pi => pi.DateRecvd);

                // To be initialized return collection.
                List<Data.Entities.Profile> updateProfileModelList = new List<Data.Entities.Profile>();

                foreach (string currentTicker in currentPositions)
                {
                    // Do we have income data for this position that is greater than or equal to a least a year old, as is necessary
                    // for accurate calculation of both 1) dividend distribution frequency, and 2) dividend payout months values.
                    // Back-dated info is always from the 1st of the calculated year ago month.
                    int olderThanOneYearRevenueCount = joinedPositionIncome
                                                       .Where(pi => pi.TickerSymbol == currentTicker &&
                                                                    pi.DateRecvd <= DateTime.Now.AddMonths(-12).AddDays(-DateTime.Now.Day)).Count();

                    if (olderThanOneYearRevenueCount > 0)
                    {
                        Data.Entities.Profile updateProfileModel = new Data.Entities.Profile
                        {
                            TickerSymbol = currentTicker
                        };

                        IQueryable<PositionIncomeVm> withinLastYearData = joinedPositionIncome
                                                                          .Where(pi => pi.TickerSymbol == currentTicker &&
                                                                                       pi.DateRecvd >= DateTime.Now.AddMonths(-12).AddDays(-DateTime.Now.Day))
                                                                          .AsQueryable();

                        // Investor may have same position in multiple accounts, e.g. IRA, Roth-IRA.
                        int dupAcctCount = withinLastYearData.Select(pi => pi.Account).Distinct().Count();
                        if (dupAcctCount > 1)
                        {
                            IQueryable<PositionIncomeVm> withUniqueAcct = withinLastYearData
                                                                         .Where(pi => pi.Account == withinLastYearData.First().Account).OrderBy(pi => pi.TickerSymbol);
                            updateProfileModel.DividendFreq = CalculateDividendFrequency(withUniqueAcct);
                            updateProfileModel.DividendMonths = updateProfileModel.DividendFreq != "M" ? CalculateDividendMonths(withUniqueAcct) : "N/A";
                            updateProfileModel.DividendPayDay = CommonSvc.CalculateMedianValue(BuildDivDaysList((withUniqueAcct)));
                        }
                        else
                        {
                            updateProfileModel.DividendFreq = CalculateDividendFrequency(withinLastYearData);
                            updateProfileModel.DividendMonths = updateProfileModel.DividendFreq != "M" ? CalculateDividendMonths(withinLastYearData) : "N/A";
                            updateProfileModel.DividendPayDay = CommonSvc.CalculateMedianValue(BuildDivDaysList((withinLastYearData)));
                        }

                        // Satisfy 'required' Profile attributes & ProfileId for updating.
                        updateProfileModel.ProfileId = joinedPositionIncome.Where(d => d.TickerSymbol == currentTicker).First().ProfileId;
                        updateProfileModel.DividendYield = joinedPositionIncome.Where(d => d.TickerSymbol == currentTicker).First().DividendYield;
                        updateProfileModel.TickerDescription = joinedPositionIncome.Where(d => d.TickerSymbol == currentTicker).First().TickerDescription;
                        updateProfileModel.LastUpdate = DateTime.Now;

                        updateProfileModelList.Add(updateProfileModel);
                    }
                    else
                    {
                        exceptionTickers += currentTicker + ", ";
                    }
                }

                if (exceptionTickers.Length > 0)
                {
                    exceptionTickers.Trim();
                    exceptionTickers = exceptionTickers.Remove(exceptionTickers.LastIndexOf(','), 1);
                }

                vm.BatchProfilesList = updateProfileModelList;
                vm.ExceptionTickerSymbols = (exceptionTickers.Length > 0 ? exceptionTickers : "");
                vm.UpdateHasErrors = false;
            }
            catch (Exception)
            {
                vm.UpdateHasErrors = true;
            }

            return vm;
        }

       
        private class PositionIncomeVm
        {
            public string ProfileId { get; set; }
            public string PositionId { get; set; }
            public string TickerSymbol { get; set; }
            public string TickerDescription { get; set; }
            public string Account { get; set; }
            public DateTime DateRecvd { get; set; }
            public decimal DividendYield { get; set; } // Required per Db.
        }


        private string CalculateDividendFrequency(IQueryable<PositionIncomeVm> recsForTicker)
        {
            // Calculation based on last 12 MONTHS income data.
            string distributionFreq = string.Empty;

            // Multiple case values (eg. case 3, case 4) trap for possible non-joining PositionIds or irregular revenue payments.
            int countRecvd = recsForTicker.Count();
            switch (countRecvd)
            {
                case 3:
                case 4:
                    distributionFreq = "Q";
                    break;
                case 10:
                case 11:
                case 12:
                    distributionFreq = "M";
                    break;
                case 1:
                    distributionFreq = "A";
                    break;
                case 2:
                    distributionFreq = "S";
                    break;
                default:
                    distributionFreq = "Error count: " + countRecvd;
                    break;
            }

            return distributionFreq;
        }


        private string CalculateDividendMonths(IQueryable<PositionIncomeVm> recvdRecs)
        {

            string dividendMonths = string.Empty;
            List<int> unsortedMonths = new List<int>();

            foreach (var record in recvdRecs)
            {
                unsortedMonths.Add(record.DateRecvd.Month);
            }

            unsortedMonths.Sort();
            foreach (int month in unsortedMonths)
            {
                dividendMonths += month + ",";
            }

            // Trailing comma.
            return dividendMonths.Remove(dividendMonths.Count() - 1);
        }


        private List<int> BuildDivDaysList(IQueryable<PositionIncomeVm> recvdInfo)
        {

            List<int> unsortedDays = new List<int>();
            string dividendDays = string.Empty;

            foreach (var record in recvdInfo)
            {
                unsortedDays.Add(record.DateRecvd.Day);
            }

            unsortedDays.Sort();
            return unsortedDays;
        }

    }

}
