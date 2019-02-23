using PIMS3.DataAccess.Position;
using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.ViewModels;

namespace PIMS3.BusinessLogic.PositionData
{
    public class PositionProcessing
    {
        private Data.PIMS3Context _ctx;

        public PositionProcessing(Data.PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public IQueryable<IncomeReceivablesVm> GetPositionsWithIncomeDue(string investor)
        {
            var positionDataAccessComponent = new PositionDataProcessing(_ctx);
            var filteredJoinedPositionProfileData = positionDataAccessComponent.GetPositionsForIncomeReceivables(investor);
                       
            var currentMonth = DateTime.Now.Month;
            List<IncomeReceivablesVm> tickersWithIncomeDue = new List<IncomeReceivablesVm>();

            if (!filteredJoinedPositionProfileData.Any())
                return null; ;

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
                        DividendFreq = position.DividendFreq
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
                            DividendFreq = position.DividendFreq
                        });
                    }
                }
            }

            return tickersWithIncomeDue.AsQueryable(); ;
        }


        private bool CurrentMonthIsApplicable(string dividendMonths, string thisMonth)
        {
            // Does the current month fall within the Profile spec?
            var months = dividendMonths.Split(',');
            return months.Contains(thisMonth);
        }

        
    }
}
