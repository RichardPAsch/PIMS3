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
        private IQueryable<DelinquentIncome> savedDelinquentPositions = new List<DelinquentIncome>().AsQueryable();
        private IQueryable<DelinquentIncome> savedDelinquentPositionsUpdated = new List<DelinquentIncome>().AsQueryable();
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


            // Process any applicable overdue income receipts.
            savedDelinquentPositions = positionDataAccessComponent.GetDelinquentRecords(investor, CalculateDelinquentMonth());
            if (!savedDelinquentPositions.Any())
            {
                //  TODO:   1. test with "M" & "Q" positions (1 each) at months' end data import, 
                //          2. refactor using line 86 as starting point.
                List<DelinquentIncome> delinquencies = CheckForDelinquentPayments(investor);
                if (delinquencies.Any())
                    tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(delinquencies, tickersWithIncomeDue);

                return tickersWithIncomeDue.AsQueryable(); //Ok.
            }
            else
            {
                if (tickersWithIncomeDue.Count >= 1 && DateTime.Now.Day <= 3 && (DateTime.Now.DayOfWeek.ToString() != "Saturday" 
                                                                             && DateTime.Now.DayOfWeek.ToString() != "Sunday"))
                {
                    // Have we already persisted these new income delinquencies?
                    var persistedNewDelinquencies = positionDataAccessComponent.GetDelinquentRecords(investor, CalculateDelinquentMonth());
                    if(persistedNewDelinquencies.Any())
                        return tickersWithIncomeDue.AsQueryable();

                    // Capture & persist new delinquent income receivables, before appending to our current collection; this allows
                    // for displaying an updated set that can be acted upon as needed.
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
                }

                // Now append all (new & old) delinquent records to our returning collection.
                savedDelinquentPositionsUpdated = positionDataAccessComponent.GetDelinquentRecords(investor, CalculateDelinquentMonth());
                tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(savedDelinquentPositionsUpdated.ToList(), tickersWithIncomeDue);
            }

                // If 1st business day of the month, capture & persist any delinquent income receivables. If there are any delinquencies, then append 
                // them to our current month collection for display & as eligible to be acted upon.
               // savedDelinquentPositions = positionDataAccessComponent.GetDelinquentRecords(investor, CalculateDelinquentMonth());
            
            //if (tickersWithIncomeDue.Count >= 1 && DateTime.Now.Day <= 3 && (DateTime.Now.DayOfWeek.ToString() != "Saturday" && DateTime.Now.DayOfWeek.ToString() != "Sunday"))
            //{
                // Table already initialized with investors' delinquent payments?
                //if (!savedDelinquentPositions.Any())
                //{
                //    return tickersWithIncomeDue.AsQueryable();
                    //List<DelinquentIncome> currentOverduePositionIncomes = new List<DelinquentIncome>();
                    //foreach (IncomeReceivablesVm position in tickersWithIncomeDue)
                    //{
                    //    DelinquentIncome overduePositionIncome = new DelinquentIncome
                    //    {
                    //        InvestorId = investor,
                    //        PositionId = position.PositionId,
                    //        MonthDue = CalculateDelinquentMonth(),
                    //        TickerSymbol = position.TickerSymbol,
                    //        AccountTypeDesc = position.AccountTypeDesc
                    //    };
                    //    currentOverduePositionIncomes.Add(overduePositionIncome);
                    //}

                    //bool dataSaved = positionDataAccessComponent.SavePositionsWithOverdueIncome(currentOverduePositionIncomes);
                    //if (!dataSaved)
                    //    Log.Warning("Unable to save Position(s) with delinquent due payment(s) for investor: {0}, via PositionProcessing.GetPositionsWithIncomeDue()", investor);
                //}
            //}
            

            // Append any delinquencies to current 'tickersWithIncomeDue', for display.
            //if (savedDelinquentPositions.Any()) 
            //{
            //    List<DelinquentIncome> delinquencies = savedDelinquentPositions.ToList();
            //    tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(delinquencies, tickersWithIncomeDue);
            //}
           
            return tickersWithIncomeDue.AsQueryable(); 
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
                        //dynamic lastUpdate = backdatedPos.LastUpdate;   

                        // If lastUpdate, or (lastUpdate month -1) is NOT accounted for in divMonths, then add as a delinquent payment.
                        if (!(divMonths.Where(m => m == lastUpdate.Month.ToString())).Any() && 
                            !(divMonths.Where(m => m == lastUpdate.AddMonths(-1).Month.ToString())).Any())
                        {
                            //DelinquentIncome overduePos = new DelinquentIncome
                            //{
                            //    MonthDue = DateTime.Now.AddMonths(-1).ToString(),
                            //    AccountTypeDesc = backdatedPos.AccountType,
                            //    PositionId = backdatedPos.PositionId,
                            //    TickerSymbol = backdatedPos.TickerSymbol
                            //};
                            //unSavedDelinquentPositions.Add(overduePos);
                            unSavedDelinquentPositions.Add(InitializeModel(backdatedPos));
                        }
                    }
                    else
                    {
                        // "M" dividend frequencies with a 'lastUpdate' > 30 days ago are delinquent.
                        if (DateTime.Now.AddMonths(-1).Month != lastUpdate.Month)
                        {
                            //DelinquentIncome overduePos = new DelinquentIncome
                            //{
                            //    MonthDue = DateTime.Now.AddMonths(-1).ToString(),
                            //    AccountTypeDesc = backdatedPos.AccountType,
                            //    PositionId = backdatedPos.PositionId,
                            //    TickerSymbol = backdatedPos.TickerSymbol
                            //};
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


    }
}
