using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIMS3.Data;
using PIMS3.DataAccess.Position;
using PIMS3.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PositionController : ControllerBase
    {
        private readonly PIMS3Context _ctx;

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


        [HttpGet("{includeInactiveStatus}/{investorId}")]
        public ActionResult GetPositions(bool includeInactiveStatus, string investorId)
        {
            PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_ctx);
            IQueryable<PositionsForEditVm> positionInfo = positionDataAccessComponent.GetPositions(investorId, includeInactiveStatus);

            return Ok(positionInfo);
        }


        [HttpPut("UpdateEditedPositions")]
        public ActionResult UpdateEditedPositions([FromBody] dynamic[] positionEdits)
        {
            var positionDataAccessComponent = new PositionDataProcessing(_ctx);
            var positionsUpdated = positionDataAccessComponent.UpdatePositions(MapToVm(positionEdits));

            if (positionsUpdated == positionEdits.Length)
                return Ok(positionsUpdated);

            return null;
        }

       
        private PositionsForEditVm[] MapToVm(dynamic[] sourcePositions)
        {
            var posVms = new List<PositionsForEditVm>();

            for(var i =0; i < sourcePositions.Length; i++)
            {
                var updatedPos = new PositionsForEditVm
                {
                    PositionId = sourcePositions[i].positionId.Value,
                    Status = sourcePositions[i].status.Value,
                    // Any change in 'PymtDue' results in true/false cast as a string -- not a boolean.
                    // TODO: able to specify type in agGrid dropdown?
                    PymtDue = sourcePositions[i].pymtDue.Value.GetType() == typeof(string)
                                            ? bool.Parse(sourcePositions[i].pymtDue.Value)
                                            : sourcePositions[i].pymtDue.Value
                };

                posVms.Add(updatedPos);
                updatedPos = null;
            }

            return posVms.ToArray();
        }

    }
}