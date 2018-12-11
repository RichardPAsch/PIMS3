using OfficeOpenXml;
using PIMS3.Data.Entities;
using PIMS3.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PIMS3.Services;
using PIMS3.Data;

namespace PIMS3.BusinessLogic.ImportData
{
    public class RevenueFileProcessing
    {
        private DataImportVm _viewModel;
        private static string _xlsTickerSymbolsOmitted = string.Empty;
        private IEnumerable<Income> duplicateResults;
        //private static OkNegotiatedContentResult<List<AssetIncomeVm>> _existingInvestorAssets;
        private static int _totalXlsIncomeRecordsToSave = 0;
        private readonly PIMS3Context _ctx;


        public RevenueFileProcessing(DataImportVm viewModel, PIMS3Context ctx)
        {
            _viewModel = viewModel;
            _ctx = ctx;
        }


        public bool ValidateVm()
        {
            if (!_viewModel.IsRevenueData || _viewModel.ImportFilePath == string.Empty || _viewModel.ImportFilePath == null)
            {
                return false;
            }
            else
            {
                return ValidateFileName(_viewModel.ImportFilePath) && ValidateFileType(_viewModel.ImportFilePath);
            }
        }


        private bool ValidateFileName(string filePath)
        {
            // error: filePath = null !
            return filePath.ToUpper().IndexOf("REVENUE") > 0 ? true : false;
        }


        private bool ValidateFileType(string filePath)
        {
            var ext = filePath.LastIndexOf('.') + 1;
            return (filePath.Substring(ext).ToUpper() == "XLSX" || filePath.Substring(ext).ToUpper() == "XLS") ? true : false;
        }


        public IEnumerable<Income> ParseRevenueSpreadsheetForIncomeRecords(string filePath)
        {
            var newIncomeListing = new List<Income>();
            var incomeDataAccessComponent = new DataAccess.Income.IncomeData(_ctx);
            var assetDataAccessComponent = new DataAccess.Asset.AssetData(_ctx);
            IQueryable<string> fetchedPositionId;
            const string INVESTORID = "CF256A53-6DCD-431D-BC0B-A810010F5B88"; // id for me; temporary until security implemented!

            try
            {
                var importFile = new FileInfo(filePath);

                using (var package = new ExcelPackage(importFile))
                {
                    var workSheet = package.Workbook.Worksheets[0];
                    var totalRows = workSheet.Dimension.End.Row;
                    var totalColumns = workSheet.Dimension.End.Column;
                    _xlsTickerSymbolsOmitted = string.Empty;

                    for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                    {
                        // Validate XLS
                        var headerRow = workSheet.Cells[1, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                        if (!ValidateFileAttributes(true, headerRow))
                            return null;

                        var row = workSheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                        var enumerableCells = row as string[] ?? row.ToArray();

                        // 'totalRows' may yield inaccurate results; we'll test for last record, e.g., 'enumerableCells[0] ('Recvd Date').
                        if (!enumerableCells.Any() || enumerableCells[0] == "" )
                            return newIncomeListing;

                        var xlsTicker = enumerableCells.ElementAt(3).Trim();
                        var xlsAccount = CommonSvc.ParseAccountTypeFromDescription(enumerableCells.ElementAt(1).Trim());
                       
                        fetchedPositionId = assetDataAccessComponent.FetchPositionId(INVESTORID, xlsTicker, xlsAccount).AsQueryable();

                        // Checking PositionId rather than asset is sufficient.
                        // Validate either a bad ticker symbol, or no account was found to be affiliated with this position/asset in question.
                        if (!fetchedPositionId.Any())
                        {
                            if (_xlsTickerSymbolsOmitted == string.Empty)
                                _xlsTickerSymbolsOmitted += xlsTicker;
                            else
                                _xlsTickerSymbolsOmitted += ", " + xlsTicker;

                            continue;
                        }

                        duplicateResults = incomeDataAccessComponent.FindIncomeDuplicates(fetchedPositionId.First().ToString(), enumerableCells.ElementAt(0), enumerableCells.ElementAt(4));
                        if (duplicateResults.Any())
                        {
                            if (_xlsTickerSymbolsOmitted == string.Empty)
                                _xlsTickerSymbolsOmitted += xlsTicker;
                            else
                                _xlsTickerSymbolsOmitted += ", " + xlsTicker;

                            continue;
                        }

                        if (_xlsTickerSymbolsOmitted != string.Empty)
                            _viewModel.ExceptionTickers = _xlsTickerSymbolsOmitted;

                        var newIncomeRecord = new Income
                        {
                            IncomeId = Guid.NewGuid().ToString(),
                            PositionId = fetchedPositionId.First().ToString(),
                            DateRecvd = DateTime.Parse(enumerableCells.ElementAt(0)),
                            AmountRecvd = decimal.Parse(enumerableCells.ElementAt(4)),
                            LastUpdate = DateTime.Now
                        };

                        newIncomeListing.Add(newIncomeRecord);
                        _totalXlsIncomeRecordsToSave += 1;
                        newIncomeRecord = null;
                    } // for
                    return newIncomeListing;
                } // using
            }
            catch (Exception ex)
            {
                var debugError = ex.Message;
                return null;
            }
        }


        public bool ValidateFileAttributes(bool containsRevenueData, IEnumerable<string> xlsRow)
        {
            if (xlsRow == null) return false;
            var enumerable = xlsRow as string[] ?? xlsRow.ToArray();

            // Revenue import file.
            if (containsRevenueData)
                return enumerable.ElementAt(0).Trim() == "Recvd Date" &&
                       enumerable.ElementAt(1).Trim() == "Account" &&
                       enumerable.ElementAt(2).Trim() == "Description" &&
                       enumerable.ElementAt(3).Trim() == "Symbol" &&
                       enumerable.ElementAt(4).Trim() == "Amount";

            // Asset import file.
            return enumerable.ElementAt(0).Trim() == "Account Name" &&
                   enumerable.ElementAt(1).Trim() == "Symbol" &&
                   enumerable.ElementAt(2).Trim() == "Description" &&
                   enumerable.ElementAt(3).Trim() == "Quantity" &&
                   enumerable.ElementAt(4).Trim() == "Last Price";
        }


    }
}
