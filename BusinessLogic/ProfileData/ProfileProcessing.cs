using PIMS3.Data.Entities;
using PIMS3.DataAccess.Profile;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using System;
using Serilog;


namespace PIMS3.BusinessLogic.ProfileData
{
    public class ProfileProcessing
    {
        Profile profileToBeInitialized;

        public Profile BuildProfileForProjections(Profile profile, Data.PIMS3Context ctx)
        {
            profileToBeInitialized = profile; // should now have DF :Yes!
            var profileDataAccessComponent = new ProfileDataProcessing(ctx);

            // try-catch here, or done in data access component? 
            profileToBeInitialized = profileDataAccessComponent.BuildProfile(profile.TickerSymbol.Trim());

            // Fetched dividend rate via 3rd party service is unreliable.
            CalculateDividendYield();

            return profileToBeInitialized;
        }

        private void CalculateDividendYield()
        {
            decimal calculatedAnuualizedDividendRate = 0M;
            if (profileToBeInitialized.DividendRate > 0 && profileToBeInitialized.UnitPrice > 0)
            {
                switch (profileToBeInitialized.DividendFreq)
                {
                    case "Q":
                        calculatedAnuualizedDividendRate = profileToBeInitialized.DividendRate * 4;
                        break;
                    case "M":
                        calculatedAnuualizedDividendRate = profileToBeInitialized.DividendRate * 12;
                        break;
                    case "S":
                        calculatedAnuualizedDividendRate = profileToBeInitialized.DividendRate * 2;
                        break;
                    case "A":
                        calculatedAnuualizedDividendRate = profileToBeInitialized.DividendRate;
                        break;
                }

                profileToBeInitialized.DividendYield = decimal.Round(calculatedAnuualizedDividendRate / profileToBeInitialized.UnitPrice * 100, 2);
            }
            else
                profileToBeInitialized.DividendYield = 0M;
        }


        public Dictionary<string,string> CalculateDivFreqAndDivMonths(string tickerSymbol, Data.PIMS3Context ctx) 
        {
            StringBuilder oneYearDivPayMonths_1 = new StringBuilder();
            string oneYearDivPayMonths_2 = string.Empty;
            int divFreqCounter = 0;
            string tempDate = "";

            var profileDataAccessComponent = new ProfileDataProcessing(ctx);
            JArray orderedJsonTickerPriceData = profileDataAccessComponent.FetchDividendSpecsForTicker(tickerSymbol);

            if (orderedJsonTickerPriceData == null)
            {
                Log.Warning("ProfileProcessing.CalculateDivFreqAndDivMonths() - found unresolved ticker in Tiingo service, unable to update Profile price data for {0}: ", tickerSymbol);
                return null;
            }

            foreach (JObject objChild in orderedJsonTickerPriceData.Children<JObject>())
            {
                // Loop will key in on "divCash" for gathering needed freq & month specs.
                foreach (var property in objChild.Properties())
                {
                    if (property.Name == "date")
                        tempDate = property.Value.ToString();

                    if (property.Name == "divCash")
                    {
                        if (decimal.Parse(property.Value.ToString()) > 0)
                        {
                            divFreqCounter += 1;
                            oneYearDivPayMonths_1.Append(ExtractMonthFromDivPayDate(tempDate));
                            oneYearDivPayMonths_1.Append(",");
                        }
                    }
                }
            }

            if (oneYearDivPayMonths_1.Length == 0)
                return null;

            // Strip trailing comma.
            oneYearDivPayMonths_2 = oneYearDivPayMonths_1.ToString().Substring(0, oneYearDivPayMonths_1.Length-1);
            string[] oneYearDivPayMonths_3 = oneYearDivPayMonths_2.Split(',');

            Dictionary<string, string> finalProfileSpecs = new Dictionary<string, string>();
            int monthsCount = oneYearDivPayMonths_3.Length;

            if(monthsCount >= 3 && monthsCount <= 5)
            {
                // Due to possible inconsistent number of income receipts made within the last 12 month price history obtained
                // from our 3rd party service (Tiingo), we'll account for this by designating as (Q)uarterly dividend frequency.
                finalProfileSpecs.Add("DF", "Q");
                finalProfileSpecs.Add("DM", oneYearDivPayMonths_2);
            }
            else if(monthsCount >= 5){
                finalProfileSpecs.Add("DF", "M");
                finalProfileSpecs.Add("DM", "-");
            }
            else if (monthsCount == 2)
            {
                finalProfileSpecs.Add("DF", "S");
                finalProfileSpecs.Add("DM", oneYearDivPayMonths_2);
            }
            else {
                finalProfileSpecs.Add("DF", "A");
                finalProfileSpecs.Add("DM", oneYearDivPayMonths_2);
            }

            return finalProfileSpecs;
        }


        private string ExtractMonthFromDivPayDate(string sourceDate)
        {
            // Dates reported by 3rd party API (Tiingo) are 'ex-Dividend Date" & will 
            // vary, depending on ticker. Therefore, payout months are approximations.
            int payoutMonth;
            if (Convert.ToDateTime(sourceDate).Month == 12)
                payoutMonth = 1;
            else
                payoutMonth = Convert.ToDateTime(sourceDate).Month + 1;
            

            return payoutMonth.ToString();
        }


       



    }
}
