using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIMS3.Data;
using PIMS3.DataAccess.Position;
using PIMS3.ViewModels;

namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AssetClassesController : ControllerBase
    {
        private readonly PIMS3Context _context;

        public AssetClassesController(PIMS3Context context)
        {
            _context = context;
        }


        // GET: api/AssetClasses
        [HttpGet]
        public ActionResult<AssetClassesVm> GetAssetClass()
        {
            PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_context);
            return Ok(positionDataAccessComponent.GetAssetClassDescriptions());
        }


        #region DEFERRED until needed
        /*

        GET: api/AssetClasses/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssetClass([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var assetClass = await _context.AssetClass.FindAsync(id);

            if (assetClass == null)
            {
                return NotFound();
            }

            return Ok(assetClass);
        }


        PUT: api/AssetClasses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssetClass([FromRoute] string id, [FromBody] AssetClass assetClass)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != assetClass.AssetClassId)
            {
                return BadRequest();
            }

            _context.Entry(assetClass).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetClassExists(id))
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


        POST: api/AssetClasses
        [HttpPost]
        public async Task<IActionResult> PostAssetClass([FromBody] AssetClass assetClass)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.AssetClass.Add(assetClass);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAssetClass", new { id = assetClass.AssetClassId }, assetClass);
        }


        // DELETE: api/AssetClasses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssetClass([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var assetClass = await _context.AssetClass.FindAsync(id);
            if (assetClass == null)
            {
                return NotFound();
            }

            _context.AssetClass.Remove(assetClass);
            await _context.SaveChangesAsync();

            return Ok(assetClass);
        }

        private bool AssetClassExists(string id)
        {
            return _context.AssetClass.Any(e => e.AssetClassId == id);
        }

        */
        #endregion



    }
}