using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PIMS3.Data.Entities;
using PIMS3.Data.Repositories.IncomeSummary;
using PIMS3.ViewModels;
using PIMS3.Data;
using PIMS3.BusinessLogic.PositionData;
using PIMS3.DataAccess.IncomeData;
using Microsoft.AspNetCore.Authorization;
using PIMS3.BusinessLogic.Income;
using Serilog;


namespace PIMS3.Controllers
{
    // Using token replacement in route templates ([controller], [action], [area]).
    [Route("api/[controller]")]
    [Authorize] 
    public class IncomeController : Controller
    {
        public readonly IIncomeRepository _repo;
        private readonly ILogger<IncomeController> _logger;
        private int _counter = 1;
        private decimal _runningYtdTotal;
        private decimal[] _incomeCount;
        private IList<YtdRevenueSummaryVm> _tempListing = new List<YtdRevenueSummaryVm>();
        private readonly PIMS3Context _ctx;
        public readonly IncomeProcessing _incomeProcessingBusLogicComponent;


        public IncomeController(IIncomeRepository repo, ILogger<IncomeController> logger, PIMS3Context ctx)
        {
            _repo = repo;
            _logger = logger;
            _ctx = ctx;
            _incomeProcessingBusLogicComponent = new IncomeProcessing(_ctx);
        }


        // Using attribute routing with Http[Verb] attributes.
        [HttpGet("{yearsToBackDate:int}/{isRevenueSummary:bool}/{Id}")]
        public IEnumerable<YtdRevenueSummaryVm> GetRevenueSummary(int yearsToBackDate, bool isRevenueSummary, string Id)
        {
            // isRevenueSummary param used only for URL routing here vs. GetRevenue().
            IEnumerable<Income> incomeData;
            IncomeProcessing blComponent = new IncomeProcessing(_ctx);
            try
            {
                incomeData = _repo.GetRevenueSummaryForYear(yearsToBackDate, Id);
                IEnumerable<YtdRevenueSummaryVm> ytdRevenueSummary = blComponent.CalculateRevenueTotals(incomeData.AsQueryable());
                _incomeCount = new decimal[ytdRevenueSummary.Count()];
                ytdRevenueSummary.ToList().ForEach(CalculateAverages);
                _logger.LogInformation("Successfull YTD income summary via GetRevenueSumary()");
                return ytdRevenueSummary.OrderByDescending(r => r.MonthRecvd).ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"Unable to fetch/calculate income due to: {ex.Message.ToString()}");
               // _logger.LogError($"Unable to fetch/calculate income via GetRevenueSummary() due to: {ex}");
                return null;
            }
        }


        [HttpGet("GetMissingIncomeSchedule/{investorId}")]
        public IEnumerable<IncomeReceivablesVm> GetMissingIncomeSchedule(string investorId)
        {
            // Creates on a monthly basis, a schedule of due income receipts; to be used for validating received
            // revenue during each month before income is actually imported at months' end. Each ticker acknowledgement
            // of received income, removes that ticker from the schedule.

            // Qualifying Positions will drive processing in 'PositionProcessing'.
            var positionBusLogicComponent = new PositionProcessing(_ctx);

            IQueryable<IncomeReceivablesVm> positionsDuePymt = positionBusLogicComponent.GetPositionsWithIncomeDue(investorId); 

            return positionsDuePymt;
        }


        [HttpGet("{backDatedYears:int}/{investorId}")]
        public IEnumerable<IncomeSavedVm> GetRevenue(int backDatedYears, string investorId)
        {
            var incomeDataAccessComponent = new IncomeDataProcessing(_ctx);
            return incomeDataAccessComponent.GetRevenueHistory(backDatedYears, investorId);
        }

        [HttpPut("")]
        public ActionResult UpdateEditedIncome([FromBody] dynamic editedIncome)
        {
            var incomeDataAccessComponent = new IncomeDataProcessing(_ctx);
            dynamic updatedCount = incomeDataAccessComponent.UpdateRevenue(MapToVm(editedIncome));

            return updatedCount == editedIncome.Count ? (ActionResult)Ok(updatedCount) : null;
        }



        #region Helpers

        private void CalculateAverages(YtdRevenueSummaryVm item)
        {
            // YTD & 3Mos rolling averages.
            _runningYtdTotal += item.AmountRecvd;
            _incomeCount[_counter - 1] = item.AmountRecvd;
            item.YtdAverage = Math.Round(_runningYtdTotal / _counter, 2);
            if (_counter >= 3)
            {
                item.Rolling3MonthAverage = Math.Round((item.AmountRecvd + _incomeCount[_counter - 2] + _incomeCount[_counter - 3]) / 3, 2);
            }
            _tempListing.Add(item);
            _counter += 1;
        }

        private IncomeForEditVm[] MapToVm(dynamic sourceData)
            {
                // Mapping only necessary fields.
                var listing = new List<IncomeForEditVm>();

                foreach(var item in sourceData)
                {
                    listing.Add(new IncomeForEditVm
                    {
                        IncomeId = item.incomeId,
                        DateRecvd = item.dateRecvd,
                        AmountReceived = item.amountRecvd
                    });
                }

                return listing.ToArray();
            }

        #endregion


    }
}
