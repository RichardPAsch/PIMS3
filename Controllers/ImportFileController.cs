using Microsoft.AspNetCore.Mvc;
using PIMS3.ViewModels;
using PIMS3.DataAccess.ImportData;


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

    [Route("api/[controller]")]
    [ApiController]
    public class ImportFileController : ControllerBase
    {
        private static DataImportVm processedVm;


        public ImportFileController()
        {
        }

        // There are multiple return/response types and paths in this action.
        [HttpPost("{isRevenue}")]
        [ProducesResponseType(400)]
        [ProducesResponseType(200, Type = typeof(DataImportVm))]
        public ActionResult<DataImportVm> ProcessImportFile([FromBody] DataImportVm importFile, bool isRevenue = true)
        {
            if (isRevenue && !importFile.IsRevenueData)
            {
                return BadRequest(ModelState);
            }

            var dataAccessComponent = new RevenueFileImport();
            
            // 12.6.18 - debug stopped here; 
            processedVm = dataAccessComponent.SaveRevenue(importFile);

            // UI to interpret updated Vm attributes.
            if (processedVm.RecordsSaved == 0)
            {
                if(importFile.ExceptionTickers != string.Empty)
                    return BadRequest(new { exceptionTickers = processedVm.ExceptionTickers });

                return BadRequest(new { exceptionMessage = "Error processing income import data; please try later." });
            }

            return CreatedAtAction("ProcessImportFile", new { count = processedVm.RecordsSaved, amount = processedVm.AmountSaved });
            
                

            
            //string dataPersistenceResults;
            //var importFileUrl = importFile.ImportFilePath;

            /*  -------- OLD PIMS code ------------
           var requestUri = Request.RequestUri.AbsoluteUri;

                      _serverBaseUri = Utilities.GetWebServerBaseUri(requestUri);

                      // Verify investor login via email addr.
                      _currentInvestor = _identityService.CurrentUser;
                      if (_currentInvestor == null)
                      {
                          //return BadRequest("Import aborted; Investor login required."); 
                          // un-comment during Fiddler testing
                          // TODO: in Production, exit if not logged in.
                          _currentInvestor = "rpasch@rpclassics.net";
                      }

                      if (importFile.IsRevenueData)
                      {
                          var assetCtrl = new AssetController(_repositoryAsset, _identityService, _repositoryInvestor);
                          var investorId = Utilities.GetInvestorId(_repositoryInvestor, _currentInvestor);
                          _existingInvestorAssets = await assetCtrl.GetByInvestorAllAssets(investorId) as OkNegotiatedContentResult<List<AssetIncomeVm>>;
                          var portfolioRevenueToBeInserted = ParseRevenueSpreadsheet(importFileUrl);
                          if (portfolioRevenueToBeInserted == null)
                              return BadRequest("Invalid XLS data submitted.");

                          var revenueToBeInserted = portfolioRevenueToBeInserted as Income[] ?? portfolioRevenueToBeInserted.ToArray();
                          if (!revenueToBeInserted.Any())
                              return BadRequest("No income data saved; please check ticker symbol(s), amount(s), and/or account(s) for validity.");

                          dataPersistenceResults = PersistIncomeData(revenueToBeInserted);
                      }
                      else
                      {
                          var portfolioListing = ParsePortfolioSpreadsheet(importFileUrl);
                          if (portfolioListing == null)
                              return BadRequest("Error processing Position(s) in one or more accounts.");

                          var portfolioAssetsToBeInserted = portfolioListing as AssetCreationVm[] ?? portfolioListing.ToArray();
                          if (!portfolioAssetsToBeInserted.Any())
                              return BadRequest("Invalid XLS data, duplicate position-account ?");

                          dataPersistenceResults = PersistPortfolioData(portfolioAssetsToBeInserted);
                      }

                      var responseVm = new HttpResponseVm { ResponseMsg = dataPersistenceResults };
                      return Ok(responseVm);
                  }
             */
            //return Ok();

        }




    }
}
