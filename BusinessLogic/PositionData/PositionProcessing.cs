using PIMS3.DataAccess.Position;
using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.ViewModels;
using PIMS3.Data.Entities;

namespace PIMS3.BusinessLogic.PositionData
{
    public class PositionProcessing
    {
        private Data.PIMS3Context _ctx;
        private IQueryable<DelinquentIncome> savedDelinquencies = new List<DelinquentIncome>().AsQueryable();
        private IQueryable<DelinquentIncome> savedDelinquenciesUpdated = new List<DelinquentIncome>().AsQueryable();
        private IList<DelinquentIncome> unSavedDelinquentPositions = new List<DelinquentIncome>();
        private List<IncomeReceivablesVm> tickersWithIncomeDue = new List<IncomeReceivablesVm>();

        public PositionProcessing(Data.PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public IQueryable<IncomeReceivablesVm> GetPositionsWithIncomeDue(string investor)
        {
            PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_ctx);

            // Get eligible Positions with currently due income, i.e., status = "A" & pymtDue.
            IQueryable<IncomeReceivablesVm> filteredJoinedPositionProfileData = positionDataAccessComponent.GetPositionsForIncomeReceivables(investor);

            // 'tickersWithIncomeDue'          - for read/write UI only.
            // 'currentOverduePositionIncomes' - for Db I/O only.
            int currentMonth = DateTime.Now.Month;

            if (!filteredJoinedPositionProfileData.Any())
                return null; ;

            // Filter Positions for applicable dividend frequency distribution.
            foreach (IncomeReceivablesVm position in filteredJoinedPositionProfileData)
            {
                if (position.DividendFreq == "M")
                {
                    tickersWithIncomeDue.Add(new IncomeReceivablesVm
                    {
                        PositionId = position.PositionId,
                        TickerSymbol = position.TickerSymbol,
                        AccountTypeDesc = position.AccountTypeDesc,
                        DividendPayDay = position.DividendPayDay,
                        DividendFreq = position.DividendFreq,
                        MonthDue = DateTime.Now.Month
                    });
                }

                if (position.DividendFreq == "Q" || position.DividendFreq == "S"
                                                 || position.DividendFreq == "A")
                {
                    if (CurrentMonthIsApplicable(position.DividendMonths, currentMonth.ToString()))
                    {
                        tickersWithIncomeDue.Add(new IncomeReceivablesVm
                        {
                            PositionId = position.PositionId,
                            TickerSymbol = position.TickerSymbol,
                            AccountTypeDesc = position.AccountTypeDesc,
                            DividendPayDay = position.DividendPayDay,
                            DividendFreq = position.DividendFreq,
                            MonthDue = DateTime.Now.Month
                        });
                    }
                }
            }

            // Do we have any persisted delinquencies to add ? 
            savedDelinquencies = positionDataAccessComponent.GetSavedDelinquentRecords(investor, "").AsQueryable();

            if (savedDelinquencies.Any())
            {
                foreach (DelinquentIncome delinquentIncome in savedDelinquencies)
                {
                    tickersWithIncomeDue.Add(new IncomeReceivablesVm
                    {
                        PositionId = delinquentIncome.PositionId,
                        TickerSymbol = delinquentIncome.TickerSymbol,
                        DividendFreq = delinquentIncome.DividendFreq,
                        AccountTypeDesc = _ctx.Position.Where(x => x.PositionId == delinquentIncome.PositionId)
                                                       .Select(y => y.AccountType.AccountTypeDesc)
                                                       .First()
                                                       .ToString(),
                        MonthDue = int.Parse(delinquentIncome.MonthDue)
                    });
                }
            }

            return tickersWithIncomeDue.OrderByDescending(r => r.MonthDue)
                                       .ThenBy(r => r.DividendFreq)
                                       .ThenBy(r => r.TickerSymbol)
                                       .AsQueryable();
        }


        public List<DelinquentIncome> CaptureDelinquentPositions(string investor)
        {
            // At months' end, any Positions in arrears, will be returned for persistence into 'DelinquentIncome' table.
            List<DelinquentIncome> delinquentIncomeCandidates = new List<DelinquentIncome>();
            PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_ctx);
            IQueryable<PositionsForEditVm> availablePositions = positionDataAccessComponent.GetPositions(investor, false);

            return delinquentIncomeCandidates;
        }


        private List<DelinquentIncome> CheckForDelinquentPayments(string investorId)
        {
            var backdatedPositionProfileJoinData = _ctx.Position.Where(p => p.PositionAsset.InvestorId == investorId
                                                                         && p.Status == "A"
                                                                         && p.PymtDue == true)
                                                                .Join(_ctx.Profile, p => p.PositionAsset.Profile.ProfileId, pr => pr.ProfileId, (position, profile) =>
                                                                            new { position.PositionId, position.LastUpdate,
                                                                                profile.TickerSymbol, position.PymtDue, position.AccountType,
                                                                                profile.DividendMonths, profile.DividendFreq})
                                                                .OrderBy(profile => profile.TickerSymbol);

            foreach (dynamic backdatedPos in backdatedPositionProfileJoinData)
            {
                // Do we have a corresponding record in 'tickersWithIncomeDue', if so, move on, as this record represents a 
                // currently due payment. If not, it's a candidate for evaluation.
                var matchingRecord = tickersWithIncomeDue.Where(currentlyDue => currentlyDue.PositionId == backdatedPos.PositionId);
                if (!matchingRecord.Any())
                {
                    dynamic lastUpdate = backdatedPos.LastUpdate;

                    // Evaluate non-matching 'backdatedPositionProfileJoinData' position.
                    if (backdatedPos.DividendFreq != "M")
                    {
                        string[] divMonths = backdatedPos.DividendMonths.Split(',');

                        // If lastUpdate, or (lastUpdate month -1) is NOT accounted for in divMonths, then add as a delinquent payment.
                        if (!(divMonths.Where(m => m == lastUpdate.Month.ToString())).Any() && 
                            !(divMonths.Where(m => m == lastUpdate.AddMonths(-1).Month.ToString())).Any())
                        {
                            unSavedDelinquentPositions.Add(InitializeModel(backdatedPos));
                        }
                    }
                    else
                    {
                        // "M" dividend frequencies with a 'lastUpdate' > 30 days ago are delinquent.
                        if (DateTime.Now.AddMonths(-1).Month != lastUpdate.Month)
                        {
                            unSavedDelinquentPositions.Add(InitializeModel(backdatedPos));
                        }
                    }
                }
            }
            return unSavedDelinquentPositions.ToList();
        }


        private DelinquentIncome InitializeModel(dynamic backdatedPos)
        {
            DelinquentIncome overduePos = new DelinquentIncome
            {
                MonthDue = DateTime.Now.AddMonths(-1).ToString(),
                AccountTypeDesc = backdatedPos.AccountType,
                PositionId = backdatedPos.PositionId,
                TickerSymbol = backdatedPos.TickerSymbol
            };
            return overduePos;
        }


        public bool CurrentMonthIsApplicable(string dividendMonths, string thisMonth)
        {
            // Does the current month fall within the Profile specification regarding dividend distributions ?
            var months = dividendMonths.Split(',');
            return months.Contains(thisMonth);
        }


        private string CalculateDelinquentMonth()
        {
            return Convert.ToString(DateTime.Now.Month == 1 ? 12 : DateTime.Now.Month - 1);
        }


        private List<IncomeReceivablesVm> MapAndAddOverduePositionsToCurrentPositions(List<DelinquentIncome> delinquentSource, List<IncomeReceivablesVm> fordisplayTarget)
        {
            // Intentionally ommitting dividend: (freq, months, & payday) in delinquencies collection for inclusion in returned collection, 
            // so that these records are more conspicious in the UI.
            // TODO: 1. show DivFreq, 2. use "" instead of zeroes for divDay.
            foreach (DelinquentIncome delinquentPosition in delinquentSource)
            {
                IncomeReceivablesVm overduePositionIncomeToAdd = new IncomeReceivablesVm
                {
                    InvestorId = delinquentPosition.InvestorId,
                    PositionId = delinquentPosition.PositionId,
                    MonthDue = int.Parse(delinquentPosition.MonthDue),
                    TickerSymbol = delinquentPosition.TickerSymbol,
                    AccountTypeDesc = delinquentPosition.AccountTypeDesc
                };

                fordisplayTarget.Add(overduePositionIncomeToAdd);
            }
            return fordisplayTarget;
        }


        private List<IncomeReceivablesVm> MapAndAddOverduePositions(List<DelinquentIncome> delinquentSource)
        {
            List<IncomeReceivablesVm> targetVmListing = new List<IncomeReceivablesVm>();
            foreach (DelinquentIncome delinquentPosition in delinquentSource)
            {
                IncomeReceivablesVm overduePositionIncomeToAdd = new IncomeReceivablesVm
                {
                    InvestorId = delinquentPosition.InvestorId,
                    PositionId = delinquentPosition.PositionId,
                    MonthDue = int.Parse(delinquentPosition.MonthDue),
                    TickerSymbol = delinquentPosition.TickerSymbol,
                    AccountTypeDesc = delinquentPosition.AccountTypeDesc
                };

                targetVmListing.Add(overduePositionIncomeToAdd);
            }
            return targetVmListing;
        }


        private DelinquentIncome MapToDelinquentIncome(Position mapSource, string investorId, string currentTicker)
        {
            DelinquentIncome mapTarget = new DelinquentIncome
            {
                PositionId = mapSource.PositionId,
                InvestorId = investorId,
                MonthDue = DateTime.Now.AddMonths(-1).Month.ToString(),
                TickerSymbol = currentTicker
            };
            return mapTarget;
        }


    }
}
