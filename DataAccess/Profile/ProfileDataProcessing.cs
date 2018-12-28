using PIMS3.Data;
using PIMS3.ViewModels;
using System;
using System.Linq;
using System.Net.Http;


namespace PIMS3.DataAccess.Profile
{
    public class ProfileDataProcessing
    {
        private PIMS3Context _ctx;
        private const string TiingoApiService = "https://api.tiingo.com/tiingo/daily/"; // + <ticker>

        public ProfileDataProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        // Note: Can we add a new Position w/o adding a Profile, if the Profile is already presisted? A: YES!

        public Data.Entities.Profile FetchDbProfile(string ticker)
        {
            var dbProfile = _ctx.Profile
                                .Where(p => p.TickerSymbol.ToUpper() == ticker.ToUpper())
                                .Select(p => p)
                                .AsQueryable();

            // ** Re-examine how we want to use AssetCreationVm.
            if (!dbProfile.Any())
                return null;
            else
            {
                // No need for mapping Vm to model, as we'll use model for insert or update.
                return dbProfile.First();
            }

        }


        // ** 12.28.18 - WIP
        public Data.Entities.Profile FetchWebProfile(string ticker)
        {

            return null;
        }



        private static ProfileVm InitializeProfile(string ticker, bool isDbProfileCheck, string _serverBaseUri)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(_serverBaseUri) })
            {
                HttpResponseMessage response = null;
                try
                {
                    if (!isDbProfileCheck)
                        response = client.GetAsync("Pims.Web.Api/api/Profile/" + ticker).Result;  // see GetProfileByTicker()
                    else
                        response = client.GetAsync("Pims.Web.Api/api/Profile/persisted/" + ticker).Result;


                    if (response.IsSuccessStatusCode)
                    {
                        var profile = response.Content.ReadAsAsync<ProfileVm>().Result;
                        // Enforce the 50 char limitation on the ticker 'description' dB field.
                        if (profile.TickerDescription.Length >= 50)
                            profile.TickerDescription = profile.TickerDescription.Substring(0, 50);

                        return profile;
                    }
                }
                catch (Exception e)
                {
                    if (e.InnerException != null) Console.WriteLine(e.InnerException.Message);
                }
            }

            return null;
        }

        /* From PIMS.ProfileController.cs
         
        // class-level vars:
        private static IGenericRepository<Profile> _repository;
        private const string BaseTiingoUrl = "https://api.tiingo.com/tiingo/daily/";
        private const string TiingoAccountToken = "95cff258ce493ec51fd10798b3e7f0657ee37740";
        private DateTime cutOffDateTimeForProfileUpdate = DateTime.Now.AddHours(-72);

         
        [HttpGet]
        [Route("{tickerForProfile?}")]
        // e.g. http://localhost/Pims.Web.Api/api/Profile/IBM
        public async Task<IHttpActionResult> GetProfileByTicker(string tickerForProfile)
        {
            var updatedOrNewProfile = new Profile();
            var existingProfile = await Task.FromResult(_repository.Retreive(p => p.TickerSymbol.Trim() == tickerForProfile.Trim()).AsQueryable());

            // By default, we'll use the last 6 months for pricing history.
            var priceHistoryStartDate = CalculateStartDate(-180);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseTiingoUrl + tickerForProfile); // https://api.tiingo.com/tiingo/daily/<ticker>
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + TiingoAccountToken);

                HttpResponseMessage historicPriceDataResponse;
                HttpResponseMessage metaDataResponse;
                JArray jsonTickerPriceData;
                Task<string> responsePriceData;

                
                historicPriceDataResponse = await client.GetAsync(client.BaseAddress + "/prices?startDate=" + priceHistoryStartDate + "&" + "token=" + TiingoAccountToken);
                if (historicPriceDataResponse == null)
                    return BadRequest("Unable to update Profile price data for: " + tickerForProfile);

                responsePriceData = historicPriceDataResponse.Content.ReadAsStringAsync();
                jsonTickerPriceData = JArray.Parse(responsePriceData.Result);

                // Sort Newtonsoft JArray historical results on 'date', e.g., date info gathered.
                var orderedJsonTickerPriceData = new JArray(jsonTickerPriceData.OrderByDescending(obj => obj["date"]));

                var sequenceIndex = 0;
                var divCashGtZero = false;
                var metaDataInitialized = false;
                foreach (var objChild in orderedJsonTickerPriceData.Children<JObject>())
                {
                    if (existingProfile.Any())
                    {
                        // Profile update IF last updated > 72hrs ago.
                        if (Convert.ToDateTime(existingProfile.First().LastUpdate) > cutOffDateTimeForProfileUpdate)
                            return Ok(existingProfile.First());

                        // 11.17.2017 - Due to Tiingo API limitations, update just dividend rate.
                        foreach (var property in objChild.Properties()) {
                            if (property.Name != "divCash") continue;
                            var cashValue = decimal.Parse(property.Value.ToString());

                            if (cashValue <= 0) break;
                            existingProfile.First().DividendRate = decimal.Parse(property.Value.ToString());
                            existingProfile.First().DividendPayDate = DateTime.Parse(objChild.Properties().ElementAt(0).Value.ToString());
                            existingProfile.First().Price = decimal.Parse(objChild.Properties().ElementAt(1).Value.ToString());
                            existingProfile.First().DividendYield = Utilities.CalculateDividendYield(existingProfile.First().DividendRate, existingProfile.First().Price);
                            existingProfile.First().LastUpdate = DateTime.Now;
                            updatedOrNewProfile = existingProfile.First();
                            return Ok(updatedOrNewProfile);
                        }
                        continue;
                    }
                   
                    if (divCashGtZero)
                        break;


                    // New Profile processing. Capture meta data. 
                    if (!metaDataInitialized)
                    {
                        metaDataResponse = await client.GetAsync(client.BaseAddress + "?token=" + TiingoAccountToken);
                        if (metaDataResponse == null)
                            return BadRequest("Unable to fetch Profile meta data for: " + tickerForProfile);

                            var responseMetaData = metaDataResponse.Content.ReadAsStringAsync();
                            var jsonTickerMetaData = JObject.Parse(await responseMetaData);

                            updatedOrNewProfile.TickerDescription = jsonTickerMetaData["name"].ToString().Trim();
                            updatedOrNewProfile.TickerSymbol = jsonTickerMetaData["ticker"].ToString().Trim();
                            metaDataResponse.Dispose();
                                            metaDataInitialized = true;
                    }
                   
                    
                    // Capture most recent closing price & dividend rate (aka divCash); 
                    if (sequenceIndex == 0) {
                        foreach (var property in objChild.Properties()) {
                            if (property.Name != "close") continue;
                            updatedOrNewProfile.Price = decimal.Parse(property.Value.ToString());
                            break;
                        }
                    } 
                    else
                    {
                        foreach (var property in objChild.Properties()) {
                            if (property.Name != "divCash") continue;
                            var cashValue = decimal.Parse(property.Value.ToString());

                            if (cashValue <= 0) continue;
                            updatedOrNewProfile.DividendRate = decimal.Parse(property.Value.ToString());
                            updatedOrNewProfile.DividendYield = Utilities.CalculateDividendYield(updatedOrNewProfile.DividendRate, updatedOrNewProfile.Price);
                            updatedOrNewProfile.DividendPayDate = DateTime.Parse(objChild.Properties().ElementAt(0).Value.ToString());
                            divCashGtZero = true;
                            break;
                        }
                    }

                    sequenceIndex += 1;
                }

                historicPriceDataResponse.Dispose();
                updatedOrNewProfile.ProfileId = Guid.NewGuid();
                updatedOrNewProfile.AssetId = Guid.NewGuid();
                updatedOrNewProfile.EarningsPerShare = 0;
                updatedOrNewProfile.PE_Ratio = 0;
                updatedOrNewProfile.ExDividendDate = null;

            } // end using()

            updatedOrNewProfile.LastUpdate = DateTime.Now;
            return Ok(updatedOrNewProfile);

        } // end fx


        // ** From ProfileRepository.cs **
        public IQueryable<Profile> Retreive(Expression<Func<Profile, bool>> predicate)
        {
            try {
                return RetreiveAll().Where(predicate);
            }
            catch (Exception) {
                return null;
            }
        }



        */


    }
}
