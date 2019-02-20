using Microsoft.AspNetCore.Mvc;
using PIMS3.BusinessLogic.ProfileData;
using PIMS3.Data;
using PIMS3.DataAccess.Position;
using System;
using System.Linq;

namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly PIMS3Context _dbCtx;
        // TODO: temporary until security implemented.
        private readonly string investorId = "CF256A53-6DCD-431D-BC0B-A810010F5B88";

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


        [HttpGet()]
        public IQueryable<Data.Entities.Profile> GetProfilesForIncomeSchedule()
        {
            var positionDataAccessComponent = new PositionDataProcessing(_dbCtx);
            var eligiblePositions = positionDataAccessComponent.GetPositionsByInvestorId(investorId);

            var eligibleProfiles = eligiblePositions.Select(p => p.PositionAsset.Profile)
                                                    .Distinct()
                                                    .Where(p => p.DividendFreq == "A" ||
                                                                p.DividendFreq == "S" ||
                                                                p.DividendFreq == "Q" ||
                                                                p.DividendFreq == "M")
                                                    .OrderBy(p => p.DividendFreq)
                                                    .ThenBy(p => p.TickerSymbol)
                                                    .AsQueryable();
            return eligibleProfiles;
        }

    }
}