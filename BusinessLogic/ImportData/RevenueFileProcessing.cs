using OfficeOpenXml;
using PIMS3.Data.Entities;
using PIMS3.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PIMS3.Services;


namespace PIMS3.BusinessLogic.ImportData
{
    public class RevenueFileProcessing
    {
        private DataImportVm _viewModel;
        private static string _xlsTickerSymbolsOmitted = string.Empty;
        private bool validationResults = false;
        private IEnumerable<Income> parsingResults;
        private IEnumerable<Income> duplicateResults;
        //private static OkNegotiatedContentResult<List<AssetIncomeVm>> _existingInvestorAssets;
        private static int _totalXlsIncomeRecordsToSave = 0;


        public RevenueFileProcessing(DataImportVm viewModel)
        {
            _viewModel = viewModel;
        }


        public bool ValidateVm()
        {
            if (!_viewModel.IsRevenueData || _viewModel.ImportFilePath == string.Empty || _viewModel.ImportFilePath == null)
            {
                return false;
            }
            else
            {
                validationResults = ValidateFileName(_viewModel.ImportFilePath) && ValidateFileType(_viewModel.ImportFilePath);
                if (!validationResults)
                    return false;
                else
                {
                    parsingResults = ParseRevenueSpreadsheetForIncomeRecords(_viewModel.ImportFilePath);
                }
                
            }
            return false;

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
            var incomeDataAccessComponent = new DataAccess.Income.IncomeData();
            var assetDataAccessComponent = new DataAccess.Asset.AssetData();
            IQueryable<string> fetchedPositionId;
            const string INVESTORID = "CF256A53-6DCD-431D-BC0B-A810010F5B88"; // id for me; temp until security implemented!

            try
            {
                var importFile = new FileInfo(filePath);

                using (var package = new ExcelPackage(importFile))
                {
                    var workSheet = package.Workbook.Worksheets[1];
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
                        var xlsTicker = enumerableCells.ElementAt(3).Trim();
                        var xlsAccount = CommonSvc.ParseAccountTypeFromDescription(enumerableCells.ElementAt(1).Trim());
                        // 11.14.18 - Needs reworking!
                        //var currentXlsAsset = _existingInvestorAssets.Content
                        //                                             .Find(a => a.RevenueTickerSymbol == xlsTicker 
                        //                                       && string.Equals(a.RevenueAccount, xlsAccount, StringComparison.CurrentCultureIgnoreCase));
                        fetchedPositionId = assetDataAccessComponent.FetchPositionId(INVESTORID, xlsTicker, xlsAccount);

                        // Validate either a bad ticker symbol, or no account was found to be affiliated with this position/asset.
                        if (!fetchedPositionId.Any())
                        {
                            if (_xlsTickerSymbolsOmitted == string.Empty)
                                _xlsTickerSymbolsOmitted += xlsTicker;
                            else
                                _xlsTickerSymbolsOmitted += ", " + xlsTicker;

                            continue;
                        }

                        // 11.13.18 - use Income dataAccess routine.
                        //var incomeCtrl = new IncomeController(_identityService, _repositoryAsset, _repositoryInvestor, _repositoryIncome);
                        //_isDuplicateIncomeData = incomeCtrl.FindIncomeDuplicates(currentXlsAsset.RevenuePositionId.ToString(), enumerableCells.ElementAt(0), enumerableCells.ElementAt(4))
                        //                                   .Result;

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
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return newIncomeListing;
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
