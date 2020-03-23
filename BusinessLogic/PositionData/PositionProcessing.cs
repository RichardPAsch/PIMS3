using PIMS3.DataAccess.Position;
using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.ViewModels;
using PIMS3.Data.Entities;
using Serilog;

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


            // ** Processing for any applicable overdue income receipts. **
            //  TODO:   1. test with "M" & "Q" positions (1 each) at months' end data import, 
            //          2. refactor using line 86 as starting point.  DONE

            // Do we have any already persisted delinquencies to later add? 
            savedDelinquencies = positionDataAccessComponent.GetSavedDelinquentRecords(investor, CalculateDelinquentMonth());


            // ============ new code start ================== WIP

            // Are we at the end of the month? What if we have no 'tickersWithIncomeDue' at months' end, as would be the 
            // case if all pymts due were paid. Maybe we just have a few delinquencies instead.
            if (DateTime.Now.Day <= 3 && DateTime.Now.DayOfWeek.ToString() != "Saturday" && DateTime.Now.DayOfWeek.ToString() != "Sunday")
            {
                // Do we have persisted income receivables for the month just ended? If so, just append them to our returning collection.
                IQueryable<DelinquentIncome> delinquenciesForMonthJustEnded = savedDelinquencies.Where(p => int.Parse(p.MonthDue) == DateTime.Now.AddMonths(-1).Month);

                if (tickersWithIncomeDue.Count >= 1)
                {
                    // Capture & persist now delinquent income receivables, before appending to our current collection; this allows
                    // for displaying an updated set that can be acted upon.
                    List<DelinquentIncome> currentOverduePositionIncomes = new List<DelinquentIncome>();
                    foreach (IncomeReceivablesVm position in tickersWithIncomeDue)
                    {
                        DelinquentIncome overduePositionIncome = new DelinquentIncome
                        {
                            InvestorId = investor,
                            PositionId = position.PositionId,
                            MonthDue = CalculateDelinquentMonth(),
                            TickerSymbol = position.TickerSymbol,
                            AccountTypeDesc = position.AccountTypeDesc
                        };
                        currentOverduePositionIncomes.Add(overduePositionIncome);
                    }

                    bool dataSaved = positionDataAccessComponent.SavePositionsWithOverdueIncome(currentOverduePositionIncomes);
                    if (!dataSaved)
                        Log.Warning("Unable to save Position(s) with delinquent due payment(s) for investor: {0}, via PositionProcessing.GetPositionsWithIncomeDue()", investor);


                    if (delinquenciesForMonthJustEnded.Any())
                    {
                        // 'savedDelinquencies' has last months' delinquencies, so append to returning collection.
                        tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(delinquenciesForMonthJustEnded.ToList(), tickersWithIncomeDue);
                    }
                    else
                    {
                        // No 'delinquenciesForMonthJustEnded' found, so just return savedDelinquencies + new tickersWithIncomeDue.
                        if (savedDelinquencies.Any())
                        {
                            tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(savedDelinquencies.ToList(), tickersWithIncomeDue);
                        }
                    }
                }
                else
                {
                    if (delinquenciesForMonthJustEnded.Any())
                    {
                        tickersWithIncomeDue = MapAndAddOverduePositions(delinquenciesForMonthJustEnded.ToList());
                    }
                }
            }
            else
            {
                // Non end-of-the-month processing. Add any persisted, if applicable, delinquencies to returned collection.
                if (savedDelinquencies.Any())
                {
                    tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(savedDelinquencies.ToList(), tickersWithIncomeDue);
                }
            }

            return tickersWithIncomeDue.OrderByDescending(r => r.MonthDue)
                                       .ThenBy(r => r.DividendFreq)
                                       .ThenBy(r => r.TickerSymbol)
                                       .AsQueryable();

            // ============= new code stop ============================





            // ============= original code start ============================
            //if (!savedDelinquencies.Any())
            //{
            //        // Do we have any unsaved current delinquencies to add?** BUT this should be done at months' end right?
            //        List<DelinquentIncome> currentDelinquencies = CheckForDelinquentPayments(investor);
            //        if (currentDelinquencies.Any())
            //            tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(currentDelinquencies, tickersWithIncomeDue);

            //        return tickersWithIncomeDue.AsQueryable(); //Ok.
            //}
            //else
            //{
            //    if (tickersWithIncomeDue.Count >= 1 && DateTime.Now.Day <= 3 && (DateTime.Now.DayOfWeek.ToString() != "Saturday" 
            //                                                                    && DateTime.Now.DayOfWeek.ToString() != "Sunday"))
            //    {
            //        // Have we already persisted these new income delinquencies?
            //        var persistedNewDelinquencies = positionDataAccessComponent.GetSavedDelinquentRecords(investor, CalculateDelinquentMonth());
            //        if(persistedNewDelinquencies.Any())
            //            return tickersWithIncomeDue.AsQueryable();

            //        // Capture & persist new delinquent income receivables, before appending to our current collection; this allows
            //        // for displaying an updated set that can be acted upon as needed.
            //        List<DelinquentIncome> currentOverduePositionIncomes = new List<DelinquentIncome>();
            //        foreach (IncomeReceivablesVm position in tickersWithIncomeDue)
            //        {
            //            DelinquentIncome overduePositionIncome = new DelinquentIncome
            //            {
            //                InvestorId = investor,
            //                PositionId = position.PositionId,
            //                MonthDue = CalculateDelinquentMonth(),
            //                TickerSymbol = position.TickerSymbol,
            //                AccountTypeDesc = position.AccountTypeDesc
            //            };
            //            currentOverduePositionIncomes.Add(overduePositionIncome);
            //        }

            //        bool dataSaved = positionDataAccessComponent.SavePositionsWithOverdueIncome(currentOverduePositionIncomes);
            //        if (!dataSaved)
            //            Log.Warning("Unable to save Position(s) with delinquent due payment(s) for investor: {0}, via PositionProcessing.GetPositionsWithIncomeDue()", investor);
            //    }

            //    // Now append all (new & old) delinquent records to our returning collection.
            //    savedDelinquenciesUpdated = positionDataAccessComponent.GetSavedDelinquentRecords(investor, CalculateDelinquentMonth());
            //    tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(savedDelinquenciesUpdated.ToList(), tickersWithIncomeDue);
            //}

            //return tickersWithIncomeDue.AsQueryable();

            // ============= original code stop ============================

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


        private bool CurrentMonthIsApplicable(string dividendMonths, string thisMonth)
        {
            // Does the current month fall within the Profile spec?
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


    }
}
