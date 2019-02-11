using Microsoft.AspNetCore.Mvc;
using PIMS3.BusinessLogic.ProfileData;
using PIMS3.Data;
using System;

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

            var profileBusLogicComponent = new ProfileProcessing();
            var profileModel = new Data.Entities.Profile
            {
                TickerSymbol = tickerProfileToFetch
            };

            try
            {
                var initializedProfile = profileBusLogicComponent.BuildProfileForProjections(profileModel, _dbCtx);
                return Ok(initializedProfile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { errorMsg = "Unable to fetch Profile data for " + tickerProfileToFetch + " due to: " + ex.Message}); 
            }
        }

    }
}