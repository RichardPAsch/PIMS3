using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PIMS3.Controllers
{
    /* ASPNET Core 2.1 documentation:
        The ControllerBase class provides access to several properties and methods, e.g. BadRequest(ModelStateDictionary) and
        CreatedAtAction(String, Object, Object). These methods are called within action methods to return HTTP 400 and 201 status codes respectively. 

        The ModelState property, also provided by ControllerBase, is accessed to handle request model validation.
        The [ApiController] attribute is commonly coupled with ControllerBase to enable REST-specific behavior for controllers. 
        ControllerBase provides access to methods such as NotFound and File.
    */

    [Route("api/[controller]")]
    [ApiController]
    public class ImportFileController : ControllerBase
    {
        /*
        public async Task<IHttpActionResult> ProcessImportFile([FromBody] ImportFileVm importFile)
        {
            string dataPersistenceResults;
            var importFileUrl = importFile.ImportFilePath;
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
    }

    


}
