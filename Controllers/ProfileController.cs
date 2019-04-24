﻿using Microsoft.AspNetCore.Mvc;
using PIMS3.BusinessLogic.ProfileData;
using PIMS3.Data;
using PIMS3.DataAccess.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.ViewModels;
using PIMS3.Data.Entities;

namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly PIMS3Context _dbCtx;

        public ProfileController(PIMS3Context dbCtx)
        {
            _dbCtx = dbCtx;
        }

        [HttpGet("{tickerProfileToFetch}")]
        [ProducesResponseType(200, Type = typeof(Data.Entities.Profile))]
        public ActionResult<Data.Entities.Profile> GetProfile(string tickerProfileToFetch){

            ProfileProcessing profileBusLogicComponent = new ProfileProcessing();
            var profileModel = new Data.Entities.Profile
            {
                TickerSymbol = tickerProfileToFetch
            };

            try
            {
                Data.Entities.Profile initializedProfile = profileBusLogicComponent.BuildProfileForProjections(profileModel, _dbCtx);
                return Ok(initializedProfile);
            }
            catch (Exception)
            {
                // TODO: Log error.
                return BadRequest(new { errorMsg = "No web Profile data found."}); 
            }
        }
        

        [HttpGet("{ticker}/{useDb}")]
        public ActionResult<Data.Entities.Profile> GetProfile(string ticker, bool useDb)
        {

            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            try
            {
                IQueryable<Data.Entities.Profile> dBProfile = profileDataAccessComponent.FetchDbProfile(ticker);
                return Ok(dBProfile);
            }
            catch 
            {
                return BadRequest(new { errorMsg = "Error fetching Profile." });
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
            var profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            bool isOkUpdate = profileDataAccessComponent.UpdateProfile(MapToProfile(editedProfile));

            return Ok(isOkUpdate);
        }


        [HttpPost("")]
        public ActionResult<bool> PersistProfile([FromBody] dynamic createdProfile)
        {
            ProfileDataProcessing profileDataAccessComponent = new ProfileDataProcessing(_dbCtx);
            bool profileIsCreated = profileDataAccessComponent.SaveProfile(MapToProfile(createdProfile, true));

            return Ok(profileIsCreated);
        }



        private Profile MapToProfile(dynamic editsOrNew, bool isNewProfile = false)
        {
            DateTime today = DateTime.Now;

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
                    CreatedBy = "rpasch@rpclassics.net",     // TODO: replace once security implemented.
                    LastUpdate = today
                }
                : new Profile
                {
                    DividendMonths = editsOrNew.divPayMonths,
                    DividendPayDay = editsOrNew.divPayDay,
                    TickerSymbol = editsOrNew.tickerSymbol,
                    LastUpdate = DateTime.Now
                };
        }

    }

}