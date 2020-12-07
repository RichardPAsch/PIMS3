using Microsoft.AspNetCore.Mvc;
using PIMS3.BusinessLogic.ProfileData;
using PIMS3.Data;
using PIMS3.DataAccess.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using PIMS3.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Serilog;


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
                    TickerSymbol = editsOrNew.tickerSymbol,
                    PERatio = editsOrNew.PE_ratio ?? 0,
                    EarningsPerShare = editsOrNew.EPS ?? 0,
                    UnitPrice = editsOrNew.unitPrice ?? 0,
                    LastUpdate = DateTime.Now
                };
        }

    }

}