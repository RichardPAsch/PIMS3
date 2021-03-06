﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIMS3.Data;
using PIMS3.DataAccess.Position;
using PIMS3.ViewModels;
using System.Collections.Generic;
using System.Linq;
using PIMS3.DataAccess.Asset;
using Serilog;
using PIMS3.DataAccess.Account;


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

        
        [HttpPut("{positionsToUpdate}")]
        public ActionResult UpdatePymtDueFlags([FromBody] List<PositionsForPaymentDueVm> positionsToUpdate)
        {
            // Pending updates for user-selected Position ids marked as having received income.
            PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_ctx);
            bool updatesAreValid = positionDataAccessComponent.UpdatePositionPymtDueFlags(positionsToUpdate);
            if (updatesAreValid)
            {
                Log.Information("Payment(s) received & marked as paid ('PymtDue': False) for {0} position(s).", positionsToUpdate.Count());
            }
            else
            {
                Log.Warning("'Income due' payment processing aborted; one or more delinquent positions found among current selections. " + 
                                                                    "Processing via PositionController.UpdatePymtDueFlags().");
            }
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
            int recordsUpdatedCount = 0;
            PositionDataProcessing positionDataAccessComponent = new PositionDataProcessing(_ctx);
            int positionsUpdated = positionDataAccessComponent.UpdatePositions(MapToVm(positionEdits));
            string positionTickerSymbolsUpdated = string.Empty;

            if (positionsUpdated == 0)
            {
                return Ok(recordsUpdatedCount);  
            }
            else
            {
                for(var i = 0; i < positionEdits.Length; i++)
                {
                    if (positionTickerSymbolsUpdated.Length == 0)
                        positionTickerSymbolsUpdated = positionEdits[i].tickerSymbol.Value;
                    else
                        positionTickerSymbolsUpdated += ", " + positionEdits[i].tickerSymbol.Value;
                }

                Log.Information("Position(s) successfuly updated for {0}", positionTickerSymbolsUpdated);
            }

            // Update referenced 'Asset' table should asset class have been modified.
            string fetchedAssetId = positionDataAccessComponent.FetchAssetId(positionEdits.First().positionId.Value);
            AssetData assetDataAccessComponent = new AssetData(_ctx);
            bool assetClassUpdated = assetDataAccessComponent.UpdateAssetClass(fetchedAssetId, positionEdits.First().assetClass.Value);

            if(positionEdits.Length > 0)
            {
                recordsUpdatedCount = positionEdits.Length;
            }
           
            return Ok(recordsUpdatedCount);
        }

       
        private PositionsForEditVm[] MapToVm(dynamic[] sourcePositions)
        {
            List<PositionsForEditVm> posVms = new List<PositionsForEditVm>();
            AccountDataProcessing accountDataAccessComponent = new AccountDataProcessing(_ctx);
                       
            for (var i = 0; i < sourcePositions.Length; i++)
            {
                var updatedPos = new PositionsForEditVm
                {
                    PositionId = sourcePositions[i].positionId.Value,
                    Status = sourcePositions[i].status.Value,
                    AssetClass = sourcePositions[i].assetClass.Value,
                    Account = sourcePositions[i].accountTypeDesc.Value,
                    AccountTypeId = accountDataAccessComponent.GetAccountTypeId(sourcePositions[i].accountTypeDesc.Value),
                    // Any change in 'PymtDue' results in true/false cast as a string -- not a boolean.
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