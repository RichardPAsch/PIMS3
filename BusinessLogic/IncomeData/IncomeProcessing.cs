using PIMS3.Data;
using System.Collections.Generic;
using PIMS3.ViewModels;
using System.Linq;
using System.Globalization;
using System;

namespace PIMS3.BusinessLogic.Income
{
    public class IncomeProcessing
    {
        private readonly PIMS3Context _ctx;

        public IncomeProcessing(PIMS3Context ctx)
        {
            _ctx = ctx;
        }


        public IEnumerable<YtdRevenueSummaryVm> CalculateRevenueTotals(IQueryable<Data.Entities.Income> recvdIncome)
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


       


        // moved to IncomeDataProcessing.
        //public IQueryable<Data.Entities.Income> FindIncomeDuplicates(string positionId, string dateRecvd, string amountRecvd)
        //{
        //    // No investorId needed, as PositionId will be unique to investor.
        //    var duplicateRecords = _ctx.Income
        //                               .Where(i => i.PositionId == positionId.Trim()
        //                                        && i.DateRecvd == DateTime.Parse(dateRecvd.Trim())
        //                                        && i.AmountRecvd == decimal.Parse(amountRecvd.Trim()))
        //                               .AsQueryable();

        //    return duplicateRecords;
        //}



    }
}





