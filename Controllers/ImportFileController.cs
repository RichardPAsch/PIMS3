using Microsoft.AspNetCore.Mvc;
using PIMS3.ViewModels;
using PIMS3.DataAccess.ImportData;
using PIMS3.Data;
using Microsoft.AspNetCore.Authorization;

namespace PIMS3.Controllers
{
    /* ASPNET Core 2.1 documentation/notes:
        The ControllerBase class provides access to several properties and methods, e.g. BadRequest(ModelStateDictionary) and
        CreatedAtAction(String, Object, Object). These methods are called within action methods to return HTTP 400 and 201 status codes respectively. 

        The ModelState property, also provided by ControllerBase, is accessed to handle request model validation.
        The [ApiController] attribute is commonly coupled with ControllerBase to enable REST-specific behavior for controllers. 
        ControllerBase provides access to methods such as NotFound and File.

        ** Route templates applied to an action that begin with a / don't get combined with route templates applied to the controller.
        * Per: https://www.thereformedprogrammer.net/is-the-repository-pattern-useful-with-entity-framework-core/
        * The repository/unit-of-work pattern (shortened to Rep/UoW) isn’t useful with EF Core. EF Core already implements a Rep/UoW pattern, 
        * so layering another Rep/UoW pattern on top of EF Core isn’t helpful.
        * Per Rob Connery: Rep/UoW just duplicates what Entity Framework (EF) DbContext give you anyway
    */

    // Follow: https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.1
    //         https://docs.microsoft.com/en-us/ef/core/

   
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ImportFileController : ControllerBase
    {
        private static DataImportVm processedVm;
        private readonly PIMS3Context _dbCtx;


        public ImportFileController(PIMS3Context dbCtx)
        {
            _dbCtx = dbCtx;
        }


        [HttpPost("{Id}")]
        public ActionResult<DataImportVm> ProcessImportFile([FromBody] DataImportVm importFile, string Id, bool isRevenue = true)
        {
            if(!ModelState.IsValid)
                return BadRequest("Invalid model state: " + ModelState);

            var dataAccessComponent = new ImportFileDataProcessing();


            if (importFile.IsRevenueData)
            {
                processedVm = dataAccessComponent.SaveRevenue(importFile, _dbCtx, Id);

                // UI to interpret updated Vm attributes.
                if (processedVm.RecordsSaved == 0)
                {
                    if (importFile.ExceptionTickers != string.Empty)
                        return BadRequest(new { exceptionTickers = processedVm.ExceptionTickers });

                    return BadRequest(new { exceptionMessage = "Error processing income import data; please try later.", isRevenueData = true });
                }

                return CreatedAtAction("ProcessImportFile", new { count = processedVm.RecordsSaved, amount = processedVm.AmountSaved }, processedVm);
            }
            else  // aka 'Positions' processing.
            {
                processedVm = dataAccessComponent.SaveAssets(importFile, _dbCtx, Id);
                if (processedVm == null)
                    return BadRequest(new { exceptionMessage = "Error saving new Position(s).", isRevenueData = false });

                // Returned customized anonymous object to be data-import.service catchError().
                if (processedVm.ExceptionTickers != string.Empty)
                    return BadRequest(new { exceptionTickers = processedVm.ExceptionTickers, isRevenueData = false });

                return CreatedAtAction("ProcessImportFile", new { count = processedVm.RecordsSaved, savedTickers = processedVm.MiscMessage }, processedVm);
            }

        }

    }

}
