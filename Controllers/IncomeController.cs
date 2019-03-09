using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PIMS3.Data.Entities;
using PIMS3.Data.Repositories.IncomeSummary;
using PIMS3.ViewModels;
//using AutoMapper; // deferred use.
using System.Globalization;
using PIMS3.Data;
using PIMS3.BusinessLogic.PositionData;
using PIMS3.DataAccess.IncomeData;


// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace PIMS3.Controllers
{
    // Using token replacement in route templates ([controller], [action], [area]).
    [Route("api/[controller]")]
    public class IncomeController : Controller
    {
        public readonly IIncomeRepository _repo;
        private readonly ILogger<IncomeController> _logger;
        //private readonly IMapper _mapper;
        private int _counter = 1;
        private decimal _runningYtdTotal;
        private decimal[] _incomeCount;
        private IList<YtdRevenueSummaryVm> _tempListing = new List<YtdRevenueSummaryVm>();
        private readonly PIMS3Context _ctx;
        // TODO: temporary until security implemented.
        private readonly string investorId = "CF256A53-6DCD-431D-BC0B-A810010F5B88";  // RPA

        public IncomeController(IIncomeRepository repo, ILogger<IncomeController> logger, PIMS3Context ctx)//, IMapper mapper)
        {
            _repo = repo;
            _logger = logger;
            _ctx = ctx;
            //_mapper = mapper;
        }


        // Using attribute routing with Http[Verb] attributes.
        [HttpGet("{yearsToBackDate:int}/{isRevenueSummary:bool}")]
        public IEnumerable<YtdRevenueSummaryVm> GetRevenueSummary(int yearsToBackDate, bool isRevenueSummary)
        {
            // isRevenueSummary param used only for URL routing here vs. GetRevenue().
            // AutoMapper not needed; summary data is read-only.
            IEnumerable<Income> incomeData;
            try
            {
                _logger.LogInformation("Attempting GetRevenueSummary().");
                incomeData = _repo.GetRevenueSummaryForYear(yearsToBackDate);
                var ytdRevenueSummary = CalculateRevenueTotals(incomeData.AsQueryable());
                _incomeCount = new decimal[ytdRevenueSummary.Count()];
                ytdRevenueSummary.ToList().ForEach(CalculateAverages);
                _logger.LogInformation("Successfull YTD income summary via GetRevenueSumary()");
                return ytdRevenueSummary.OrderByDescending(r => r.MonthRecvd).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unable to fetch/calculate income via GetRevenueSummary() due to: {ex}");
                return null;
            }
        }


        [HttpGet("GetMissingIncomeSchedule")]
        public IEnumerable<IncomeReceivablesVm> GetMissingIncomeSchedule()
        {
            // Creates on a monthly basis, a schedule of due income receipts; to be used for validating received
            // revenue during each month before income is actually imported at months' end. Each ticker acknowledgement
            // of received income, removes that ticker from the schedule.

            // Qualifying Positions will drive processing in 'PositionProcessing'.
            var positionBusLogicComponent = new PositionProcessing(_ctx);

            var positionsDuePymt = positionBusLogicComponent.GetPositionsWithIncomeDue(investorId);

            return positionsDuePymt;
        }


        [HttpGet("{backDatedYears:int}")]
        public IEnumerable<IncomeSavedVm> GetRevenue(int backDatedYears)
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
        // TODO: Move to busLogic compenent !
        private static IEnumerable<YtdRevenueSummaryVm> CalculateRevenueTotals(IQueryable<Income> recvdIncome)
            {
                IList<YtdRevenueSummaryVm> averages = new List<YtdRevenueSummaryVm>();
                var currentMonth = 0;
                var total = 0M;
                var counter = 0;

                foreach (var income in recvdIncome)
                {
                    if (currentMonth != DateTime.Parse(income.DateRecvd.ToString(CultureInfo.InvariantCulture)).Month)
                    {
                        // Last record for currently processed month.
                        if (total > 0)
                        {
                            averages.Add(new YtdRevenueSummaryVm
                            {
                                AmountRecvd = total,
                                MonthRecvd = currentMonth
                            });
                            total = 0M;
                        }
                    }

                    currentMonth = DateTime.Parse(income.DateRecvd.ToString(CultureInfo.InvariantCulture)).Month;
                    total += income.AmountRecvd;
                    counter++;

                    // Add last record.
                    if (counter == recvdIncome.Count())
                    {
                        averages.Add(new YtdRevenueSummaryVm
                        {
                            AmountRecvd = total,
                            MonthRecvd = DateTime.Parse(income.DateRecvd.ToString(CultureInfo.InvariantCulture)).Month
                        });
                    }

                }

                return averages.AsQueryable();
            }

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
