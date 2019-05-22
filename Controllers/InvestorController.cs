using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PIMS3.Data;
using PIMS3.Data.Entities;
using PIMS3.DataAccess.Investor;
using PIMS3.Services;
using PIMS3.ViewModels;


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // 5.22.19 - to be implemented
    public class InvestorController : ControllerBase
    {
        private readonly PIMS3Context _context;
        private readonly InvestorSvc _investorSvc;

        public InvestorController(PIMS3Context context, InvestorSvc investorSvc)
        {
            _context = context;
            _investorSvc = investorSvc;
        }


        // GET: api/Investor
        [HttpGet]
        public ActionResult GetAllInvestors()
        {
            InvestorDataProcessing investorDataAccessComponent = new InvestorDataProcessing(_context);
            IQueryable<InvestorVm> investors = MapToVm(investorDataAccessComponent.RetreiveAll());
            return Ok(investors);
        }


        // POST: api/Investor
        [AllowAnonymous]
        [HttpPost("AuthenticateInvestor")]
        public IActionResult AuthenticateInvestor([FromBody] Investor newInvestor)
        {
            var loggedInvestor = _investorSvc.Authenticate(newInvestor.LoginName, newInvestor.Password);

            // TODO: log results ?
            if (loggedInvestor == null)
                return BadRequest(new { message = "Unable to validate login credentials, check name and/or password." });

            return Ok(loggedInvestor);


            // 5.20.19 - Ok to here.
            // 5.22.19 - not needed here anymore.
            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);
            //}


            //_context.Investor.Add(newInvestor);
            //try
            //{
            //    await _context.SaveChangesAsync();
            //}
            //catch (DbUpdateException DbExec)
            //{
            //    if (DbExec.InnerException != null)
            //    {
            //        return new StatusCodeResult(StatusCodes.Status409Conflict);
            //    }
            //    else
            //    {
            //        throw;
            //    }
            //}

            //return CreatedAtAction("RegisterInvestor", new { id = newInvestor.InvestorId }, newInvestor);
      
        }






        // GET: api/Investor/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetInvestor([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var assetInvestor = await _context.AssetInvestor.FindAsync(id);

            if (assetInvestor == null)
            {
                return NotFound();
            }

            return Ok(assetInvestor);
        }

        // PUT: api/Investor/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssetInvestor([FromRoute] string id, [FromBody] AssetInvestor assetInvestor)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != assetInvestor.AssetId)
            {
                return BadRequest();
            }

            _context.Entry(assetInvestor).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetInvestorExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

       

        // DELETE: api/Investor/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssetInvestor([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var assetInvestor = await _context.AssetInvestor.FindAsync(id);
            if (assetInvestor == null)
            {
                return NotFound();
            }

            _context.AssetInvestor.Remove(assetInvestor);
            await _context.SaveChangesAsync();

            return Ok(assetInvestor);
        }

        private bool AssetInvestorExists(string id)
        {
            return _context.AssetInvestor.Any(e => e.AssetId == id);
        }


        private IQueryable<InvestorVm> MapToVm(IQueryable<Investor> investorEntitiesToMap)
        {
            IQueryable<InvestorVm> mappedVms = null;
            foreach(var entity in investorEntitiesToMap)
            {
                var mappedVm = new InvestorVm
                {
                    KeyId = Guid.Parse(entity.InvestorId),
                    FName = entity.FirstName,
                    LName = entity.LastName,
                    LoginName = entity.LoginName,
                    Password = entity.Password
                };
                mappedVms.Append(mappedVm);
            }
            return mappedVms;
        }

        
    }
}