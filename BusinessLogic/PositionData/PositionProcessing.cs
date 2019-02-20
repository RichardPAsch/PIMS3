using Newtonsoft.Json;
using PIMS3.DataAccess.Position;
using System;
using System.Collections.Generic;
using System.Linq;


namespace PIMS3.BusinessLogic.PositionData
{
    public class PositionProcessing
    {
        private Data.PIMS3Context _ctx;

        public PositionProcessing(Data.PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public string GetPositionsWithIncomeDue(string investor)
        {
            var positionDataAccessComponent = new PositionDataProcessing(_ctx);
            var fetchedPositions = positionDataAccessComponent.GetPositionsByInvestorId("");
            var currentMonth = DateTime.Now.Month;
            List<string> tickersWithIncomeDue = new List<string>();

            // fetchedPositions = filtered via [investorId] & [status: "A"] & [pymtDue: null || false].
            foreach (var position in fetchedPositions)
            {
                // Add 'PositionId' for later db updates.
                if (position.PositionAsset.Profile.DividendFreq == "M")
                {
                    tickersWithIncomeDue.Add(position.PositionAsset.Profile.TickerSymbol + ", " +
                                             position.AccountType.AccountTypeDesc + ", " +
                                             position.PositionAsset.Profile.DividendPayDay + ", " +
                                             position.PositionAsset.Profile.DividendFreq.ToUpper());
                }

                if (position.PositionAsset.Profile.DividendFreq == "Q" || position.PositionAsset.Profile.DividendFreq == "S"
                                                                       || position.PositionAsset.Profile.DividendFreq == "A")
                {
                    if (CurrentMonthIsApplicable(position.PositionAsset.Profile.DividendMonths, currentMonth.ToString()))
                    {
                        tickersWithIncomeDue.Add(position.PositionAsset.Profile.TickerSymbol + ", " +
                                             position.AccountType.AccountTypeDesc + ", " +
                                             position.PositionAsset.Profile.DividendPayDay + ", " +
                                             position.PositionAsset.Profile.DividendFreq.ToUpper());
                    }
                }
            }

            return JsonConvert.SerializeObject(tickersWithIncomeDue);
        }


        private bool CurrentMonthIsApplicable(string dividendMonths, string thisMonth)
        {
            // Does the current month fall within the Profile spec?
            var months = dividendMonths.Split(',');
            return months.Contains(thisMonth);
        }

               
    }
}
