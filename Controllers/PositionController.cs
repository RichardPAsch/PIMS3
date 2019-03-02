using Microsoft.AspNetCore.Mvc;
using PIMS3.Data;
using PIMS3.DataAccess.Position;
using System.Linq;

namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PositionController : ControllerBase
    {
        private readonly PIMS3Context _ctx;
        private readonly string investorId = "CF256A53-6DCD-431D-BC0B-A810010F5B88";  // RPA

        public PositionController(PIMS3Context ctx)
        {
            _ctx = ctx;
        }


        [HttpPut("{positionIdsToUpdate}")]
        public ActionResult UpdatePymtDueFlags([FromBody] string[] positionIdsToUpdate)
        {
            // Pending updates for user-selected Position ids marked as having received income.
            // Bypassing bus logic processing, as there are no business rules to enforce.
            var positionDataAccessComponent = new PositionDataProcessing(_ctx);
            var updatesAreValid = positionDataAccessComponent.UpdatePositionPymtDueFlags(positionIdsToUpdate);
            return Ok(updatesAreValid);

        }


        [HttpGet("GetPositions")]
        public ActionResult GetPositions()
        {
            var positionDataAccessComponent = new PositionDataProcessing(_ctx);
            IQueryable<ViewModels.PositionsForEditVm> positionInfo = positionDataAccessComponent.GetPositions(investorId);

            return Ok(positionInfo);
        }


        [HttpPut("UpdateEditedPositions")]
        public ActionResult UpdateEditedPositions()
        {
            var positionDataAccessComponent = new PositionDataProcessing(_ctx);

            return null;

        }

    }
}