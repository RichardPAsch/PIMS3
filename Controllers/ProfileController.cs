﻿using Microsoft.AspNetCore.Mvc;
using PIMS3.BusinessLogic.ProfileData;
using PIMS3.Data;
using PIMS3.DataAccess.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Serilog;
using Newtonsoft.Json;
using PIMS3.ViewModels;


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class ProfileController : ControllerBase
    {
        private readonly PIMS3Context _dbCtx;

        public ProfileController(PIMS3Context dbCtx)
        {
            _dbCtx = dbCtx;
        }


        [HttpGet("{tickerProfileToFetch}")]
        [ProducesResponseType(200, Type = typeof(Profile))]
        public ActionResult<Profile> GetProfile(string tickerProfileToFetch){

            ProfileProcessing profileBusLogicComponent = new ProfileProcessing();
            var profileModel = new Profile
            {
                TickerSymbol = tickerProfileToFetch
            };
          
            Dictionary<string, string> dividendFreqAndMonths = profileBusLogicComponent.CalculateDivFreqAndDivMonths(profileModel.TickerSymbol, _dbCtx);


            if(dividendFreqAndMonths != null)
            {
                profileModel.DividendFreq = dividendFreqAndMonths["DF"];

                if (!string.IsNullOrEmpty(dividendFreqAndMonths["DPD"]))
                    profileModel.DividendPayDay = Convert.ToInt32(dividendFreqAndMonths["DPD"]);

                Profile initializedProfile = profileBusLogicComponent.BuildProfileForProjections(profileModel, _dbCtx);

                return Ok(initializedProfile);
            }
            else
            {
                return BadRequest(new { warningMsg = "No web Profile data found." }); // status: 400.
            }
        }
        

        [HttpGet("{ticker}/{useDb}/{loggedInName}")]
        public ActionResult<Profile> GetProfile(string ticker, bool useDb, string loggedInName)
        {

            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            try
            {
                IQueryable<Profile> dBProfile = profileDataAccessComponent.FetchDbProfile(ticker, loggedInName);
                return Ok(dBProfile);  // dBProfile : null || valid Profile.
            }
            catch 
            {
                Log.Warning("No Db profile found for {0}; expected outcome if duplicate profile check, otherwise potential error in ProfileController.GetProfile().", ticker);
                return BadRequest(new { errorMsg = "Error fetching custom Profile." });
            }
        }


        [HttpGet("~/api/DivInfo/{ticker}")]
        public ActionResult<Dictionary<string,string>> GetDivFreqAndMonths(string ticker)
        {
            ProfileProcessing profileBusLogicComponent = new ProfileProcessing();
            Dictionary<string, string> divSpecs = profileBusLogicComponent.CalculateDivFreqAndDivMonths(ticker, _dbCtx); 
                       
            return Ok(divSpecs);
        }


        [HttpPut("")]
        public ActionResult<bool> UpdateProfile([FromBody] dynamic editedProfile)
        {
            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            bool isOkUpdate = profileDataAccessComponent.UpdateProfile(MapToProfile(editedProfile));
            if(isOkUpdate)
                Log.Information("Profile successfully updated for ticker: " + editedProfile.tickerSymbol);

            return Ok(isOkUpdate); 
        }

        [HttpPut("{investorLogin}")]
        public ActionResult<string> UpdateAllProfiles(string investorLogin)
        {
            // Investor-initiated portfolio profile updates.
            string serializedBatchResponse = string.Empty;

            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            ProfilesUpdateSummaryResultModel batchResponse = profileDataAccessComponent.BatchUpdateProfiles(investorLogin);

            if(batchResponse != null || batchResponse.ProcessedTickersCount > 0)
            {
                serializedBatchResponse = JsonConvert.SerializeObject(batchResponse);
                return serializedBatchResponse;
            }
            else
            {
                Log.Error("Error updating Profiles via ProfileController.UpdateAllProfiles() for investor: " + investorLogin);
                return string.Empty;
            }
        }


        [HttpPost("")]
        public ActionResult<bool> PersistProfile([FromBody] dynamic createdProfile)
        {
            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            bool profileIsCreated = profileDataAccessComponent.SaveProfile(MapToProfile(createdProfile, true));

            return Ok(profileIsCreated);
        }

        // Unique routing to avoid signature conflicts.
        [HttpGet("~/api/GetDistributionSchedules/{loggedInvestorId}")]
        public ActionResult<DistributionScheduleVm> GetDistributionSchedules(string loggedInvestorId)
        {
            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);

            try
            {
                var profileSchedules = profileDataAccessComponent.FetchProfileDividendSchedules(loggedInvestorId);
                return Ok(profileSchedules);
            }
            catch (Exception)
            {
                Log.Warning("Error fetching profiles for {0}, via ProfileController.GetDistributionSchedules().", loggedInvestorId);
                return BadRequest(new { errorMsg = "Error fetching custom Profile." });
            }

        }



        private Profile MapToProfile(dynamic editsOrNew, bool isNewProfile = false)
        {
            return isNewProfile
                ? new Profile
                {
                    TickerSymbol = editsOrNew.tickerSymbol,
                    TickerDescription = editsOrNew.tickerDesc,
                    DividendRate = editsOrNew.divRate,
                    DividendYield = editsOrNew.divYield,
                    DividendFreq = editsOrNew.divFreq,
                    PERatio = editsOrNew.PE_ratio,
                    EarningsPerShare = editsOrNew.EPS,
                    UnitPrice = editsOrNew.unitPrice,
                    DividendMonths = editsOrNew.divPayMonths,
                    DividendPayDay = editsOrNew.divPayDay,
                    CreatedBy = editsOrNew.investor, 
                    LastUpdate = DateTime.Now
                }
                : new Profile
                {
                    TickerDescription = editsOrNew.tickerDesc,
                    DividendRate = editsOrNew.divRate ?? 0,
                    DividendYield = editsOrNew.divYield ?? 0,
                    DividendMonths = editsOrNew.divPayMonths,
                    DividendPayDay = editsOrNew.divPayDay,
                    DividendFreq = editsOrNew.divFreq,
                    TickerSymbol = editsOrNew.tickerSymbol,
                    PERatio = editsOrNew.PE_ratio ?? 0,
                    EarningsPerShare = editsOrNew.EPS ?? 0,
                    UnitPrice = editsOrNew.unitPrice ?? 0,
                    LastUpdate = DateTime.Now
                };
        }

        

    }

}