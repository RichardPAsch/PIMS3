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
            List<IncomeReceivablesVm> tickersWithIncomeDue = new List<IncomeReceivablesVm>();

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

            // Capture & persist any delinquent income receivables on the first business day of EACH MONTH; this allows 
            // for back-dated income payments to be displayed and acted upon.
            savedDelinquentPositions = positionDataAccessComponent.GetPositionsWithOverdueIncome(investor, CalculateDelinquentMonth());
            if (tickersWithIncomeDue.Count >= 1 && DateTime.Now.Day <= 3 && 
                (DateTime.Now.DayOfWeek.ToString() != "Saturday" && DateTime.Now.DayOfWeek.ToString() != "Sunday"))
            {
                // Table already initialized with investors' delinquent payments?
                if (!savedDelinquentPositions.Any())
                {
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
            }

            // Append any delinquencies to current 'tickersWithIncomeDue', for display.
            if (savedDelinquentPositions.Any()) 
            {
                List<DelinquentIncome> delinquencies = savedDelinquentPositions.ToList();
                tickersWithIncomeDue = MapAndAddOverduePositionsToCurrentPositions(delinquencies, tickersWithIncomeDue);
            }
           
            return tickersWithIncomeDue.AsQueryable(); 
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
