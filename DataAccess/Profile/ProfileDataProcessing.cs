using Newtonsoft.Json.Linq;
using PIMS3.BusinessLogic.ImportData;
using PIMS3.Data;
using PIMS3.ViewModels;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Serilog;


namespace PIMS3.DataAccess.Profile
{
    public class ProfileDataProcessing
    {
        private PIMS3Context _ctx;
        private const string TiingoApiService = "https://api.tiingo.com/tiingo/daily/"; // + <ticker>
        private string investorLoginName = "";

        public ProfileDataProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }

        public IQueryable<Data.Entities.Profile> FetchDbProfile(string ticker, string currentlyLoggedName)
        {
            investorLoginName = currentlyLoggedName;
            IQueryable<Data.Entities.Profile> dbProfile = currentlyLoggedName == null || currentlyLoggedName == string.Empty
                ? _ctx.Profile.Where(p => p.TickerSymbol.ToUpper() == ticker.ToUpper())
                              .Select(p => p)
                              .AsQueryable()
                : _ctx.Profile.Where(p => p.TickerSymbol.ToUpper() == ticker.ToUpper() && p.CreatedBy == currentlyLoggedName)
                              .Select(p => p)
                              .AsQueryable();

            
            // ** Re-examine how we want to use AssetCreationVm.
            if (!dbProfile.Any())
                return null;
            else
            {
                // No need for mapping Vm to model, as we'll use model for insert or update.
                return dbProfile;
            }

        }


        public IQueryable<string> FetchDbProfileTicker(string profileId)
        {
            return _ctx.Profile.Where(p => p.ProfileId == profileId.Trim())
                               .Select(p => p.TickerSymbol.ToUpper().Trim())
                               .AsQueryable();
        }


        public Data.Entities.Profile BuildProfile(string ticker)
        {
            // Update or initialize Profile data via 3rd party Tiingo service.
            DateTime cutOffDateTimeForProfileUpdate = DateTime.Now.AddHours(-72);
            const string BaseTiingoUrl = "https://api.tiingo.com/tiingo/daily/";
            const string TiingoAccountToken = "95cff258ce493ec51fd10798b3e7f0657ee37740";
            ImportFileProcessing busLayerComponent = new ImportFileProcessing(null, _ctx, null);


            var updatedOrNewProfile = new Data.Entities.Profile();
            // By default, we'll use the last 6 months for pricing history.
            string priceHistoryStartDate = CalculateStartDate(-180);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseTiingoUrl + ticker); // e.g., https://api.tiingo.com/tiingo/daily/IBM
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + TiingoAccountToken);

                string metaDataResponse;
                JArray jsonTickerPriceData;

                // Ex: https://api.tiingo.com/tiingo/daily/STK/prices?startDate=2018-3-26&token=95cff258ce493ec51fd10798b3e7f0657ee37740
                var url = client.BaseAddress + "/prices?startDate=" + priceHistoryStartDate + "&" + "token=" + TiingoAccountToken;

                // Using 'Tiingo' end-point service for Profile data retreival.
                string webResponse = FetchProfileViaWebSync(client, url);  
                if (webResponse == "" || webResponse == null)
                    return null; 

                jsonTickerPriceData = JArray.Parse(webResponse);

                // Sort Newtonsoft JArray historical results on 'date', e.g., date info gathered.
                var orderedJsonTickerPriceData = new JArray(jsonTickerPriceData.OrderByDescending(obj => obj["date"]));

                var sequenceIndex = 0;
                var divCashGtZero = false;
                var metaDataInitialized = false;
                IQueryable<Data.Entities.Profile> existingProfile;

                // Check for Profile to update, either custom, or standard. The latter will contain
                // an uninitialized investorLoginName, and is accounted for in FetchDbProfile().
                try
                {
                    existingProfile = FetchDbProfile(ticker, investorLoginName);
                }
                catch (Exception)
                {
                    Log.Error("Error retreiving Db Profile via ProfileDataProcessing.BuildProfile() for ticker: {0}", ticker);
                    return null;
                }

                foreach (JObject objChild in orderedJsonTickerPriceData.Children<JObject>())
                {
                    if (existingProfile != null)
                    {
                        // Profile update IF last updated > 72hrs ago.
                        if (Convert.ToDateTime(existingProfile.First().LastUpdate) < cutOffDateTimeForProfileUpdate)
                        {
                            // Due to Tiingo API limitations, update just dividend rate.
                            foreach (var property in objChild.Properties())
                            {
                                if (property.Name != "divCash") continue;
                                var cashValue = decimal.Parse(property.Value.ToString());

                                if (cashValue <= 0) break;
                                existingProfile.First().DividendRate = decimal.Parse(property.Value.ToString());
                                // Arbitrary day; will need to allow for updating !
                                existingProfile.First().DividendPayDay = 15;
                                existingProfile.First().UnitPrice = decimal.Parse(objChild.Properties().ElementAt(1).Value.ToString());
                                existingProfile.First().DividendYield = busLayerComponent.CalculateDividendYield(existingProfile.First().DividendRate, existingProfile.First().UnitPrice);
                                existingProfile.First().LastUpdate = DateTime.Now;
                                updatedOrNewProfile = existingProfile.First();
                                return updatedOrNewProfile;
                            }
                            continue;
                        }
                    }

                    if (divCashGtZero)
                        break;

                    // New Profile processing. Capture meta data (name/ticker).
                    if (!metaDataInitialized)
                    {
                        var Uri = client.BaseAddress + "?token=" + TiingoAccountToken;
                        metaDataResponse = FetchProfileViaWebSync(new HttpClient(), Uri);
                        if (metaDataResponse == null || metaDataResponse == string.Empty)
                        {
                            Log.Warning("BadRequest - unable to fetch Profile meta data for: {0}", ticker);
                            return null; 
                        }

                        var responseMetaData = metaDataResponse;  // Content.ReadAsStringAsync();
                        var jsonTickerMetaData = JObject.Parse(responseMetaData);

                        updatedOrNewProfile.TickerDescription = jsonTickerMetaData["name"].ToString().Trim();
                        updatedOrNewProfile.TickerSymbol = jsonTickerMetaData["ticker"].ToString().Trim();
                        metaDataResponse = null;
                        metaDataInitialized = true;
                    }

                    // Capture most recent closing price & dividend rate (aka divCash); 
                    if (sequenceIndex == 0)
                    {
                        foreach (var property in objChild.Properties())
                        {
                            if (property.Name != "close") continue;
                            // Latest closing price.
                            updatedOrNewProfile.UnitPrice = decimal.Parse(property.Value.ToString());
                            break;
                        }
                    }
                    else
                    {
                        foreach (var property in objChild.Properties())
                        {
                            if (property.Name != "divCash") continue;
                            var cashValue = decimal.Parse(property.Value.ToString());

                            if (cashValue <= 0) continue;
                            updatedOrNewProfile.DividendRate = decimal.Parse(property.Value.ToString());
                            updatedOrNewProfile.DividendYield = busLayerComponent.CalculateDividendYield(updatedOrNewProfile.DividendRate, updatedOrNewProfile.UnitPrice);
                            updatedOrNewProfile.DividendPayDay = 15; 
                            divCashGtZero = true;
                            break;
                        }
                    }

                    sequenceIndex += 1;

                } // end foreach

                updatedOrNewProfile.ProfileId = Guid.NewGuid().ToString();
                updatedOrNewProfile.CreatedBy = string.Empty;
                updatedOrNewProfile.EarningsPerShare = 0;
                updatedOrNewProfile.PERatio = 0;
                updatedOrNewProfile.ExDividendDate = null;

            } // end using

            updatedOrNewProfile.LastUpdate = DateTime.Now;
            return updatedOrNewProfile;

        }


        public JArray FetchDividendSpecsForTicker(string searchTicker)
        {
            // Data fetch for determining dividend frequency & months paid.
            const string BaseTiingoUrl = "https://api.tiingo.com/tiingo/daily/";
            const string TiingoAccountToken = "95cff258ce493ec51fd10798b3e7f0657ee37740";

            // We'll use the last 12 months pricing history.
            string priceHistoryStartDate = CalculateStartDate(-360);
            JArray jsonTickerPriceData;
            JArray orderedJsonTickerPriceData;
           
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(BaseTiingoUrl + searchTicker); 
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + TiingoAccountToken);

                string url = client.BaseAddress + "/prices?startDate=" + priceHistoryStartDate + "&" + "token=" + TiingoAccountToken;
                string webResponse = FetchProfileViaWebSync(client, url);
                if (webResponse == null || webResponse == string.Empty)
                    return null;

                jsonTickerPriceData = JArray.Parse(webResponse);
                orderedJsonTickerPriceData = new JArray(jsonTickerPriceData.OrderByDescending(obj => obj["date"]));

                return orderedJsonTickerPriceData;
            }
        }


        private string FetchProfileViaWebSync(HttpClient client, string Url)
        {
            var webResponse = string.Empty;
            try
            {
                using (client)
                {
                    var response = client.GetAsync(Url).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content;

                        // Using '.Result' - results in synchronously reading the result.
                        string responseString = responseContent.ReadAsStringAsync().Result;

                        webResponse = responseString;
                    }
                    else
                    {
                        Log.Warning("Error fetching Profile data via ProfileDataProcessing.FetchProfileViaWebSync().");
                        return webResponse;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error (connection ?) via ProfileDataProcessing.FetchProfileViaWebSync() due to: {0}.", ex.Message);
                return webResponse;
            }
            
            return webResponse;
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


        private static string CalculateStartDate(int priorNumberOfDays)
        {
            if (priorNumberOfDays >= 0)
                return "Invalid hours submitted.";

            var today = DateTime.Now;
            var priorDate = today.AddDays(priorNumberOfDays);
            return (priorDate.Year + "-" + priorDate.Month + "-" + priorDate.Day).Trim();
        }


        public bool UpdateProfile(Data.Entities.Profile editedProfile)
        {
            Data.Entities.Profile profileToUpdate = new Data.Entities.Profile();
            int updateCount = 0;

            try
            {
                IQueryable<Data.Entities.Profile> existingProfile = _ctx.Profile.Where(p => p.TickerSymbol == editedProfile.TickerSymbol).AsQueryable();

                existingProfile.First().TickerDescription = editedProfile.TickerDescription;
                existingProfile.First().DividendRate = editedProfile.DividendRate;
                existingProfile.First().DividendYield = editedProfile.DividendYield;
                existingProfile.First().PERatio = editedProfile.PERatio;
                existingProfile.First().DividendMonths = editedProfile.DividendMonths;
                existingProfile.First().DividendPayDay = editedProfile.DividendPayDay;
                existingProfile.First().UnitPrice = editedProfile.UnitPrice;
                existingProfile.First().LastUpdate = Convert.ToDateTime(editedProfile.LastUpdate);

                _ctx.UpdateRange(existingProfile);
                updateCount = _ctx.SaveChanges();
            }
            catch (Exception)
            {
                return false;
            }

            return updateCount == 1 ? true : false;

        }


        public bool SaveProfile(Data.Entities.Profile newProfile)
        {
            // Check for existing Profile first, therefore run through UI 'Asset Profile'
            // menu option first, utilizing 'Create Profile' button & debug.
            int savedCount = 0;
            bool profileSaved = false;

            try
            {
                _ctx.Add(newProfile);
                savedCount = _ctx.SaveChanges();
                profileSaved = savedCount == 1 ? true : false;
            }
            catch (Exception ex)
            {
                Exception err = ex.InnerException;
                Log.Error("Error saving Profile data via ProfileDataProcessing.SaveProfile() due to: {0}.", err);
            }

            return profileSaved;
        }


        private Data.Entities.Profile MapVmToEntity(ProfileVm mapSource)
        {
            return new Data.Entities.Profile
            {
                TickerSymbol = mapSource.TickerSymbol.ToUpper().Trim(),
                TickerDescription = mapSource.TickerDescription.Trim(),
                DividendRate = mapSource.DividendRate,
                DividendYield = mapSource.DividendYield,
                DividendFreq = mapSource.DividendFreq,
                PERatio = mapSource.PE_Ratio,
                EarningsPerShare = mapSource.EarningsPerShare,
                UnitPrice = mapSource.UnitPrice,
                DividendMonths = mapSource.DividendMonths,
                DividendPayDay = mapSource.DividendPayDay,
                CreatedBy = mapSource.CreatedBy
            };

        }





    }


}
