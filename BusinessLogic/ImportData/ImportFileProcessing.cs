using OfficeOpenXml;
using PIMS3.Data.Entities;
using PIMS3.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PIMS3.Services;
using PIMS3.Data;
using PIMS3.DataAccess.ImportData;
using PIMS3.DataAccess.Position;
using PIMS3.DataAccess.Profile;
using PIMS3.DataAccess.Account;
using System.Globalization;
using PIMS3.DataAccess.IncomeData;
using Serilog;

namespace PIMS3.BusinessLogic.ImportData
{
    public class ImportFileProcessing
    {
        private DataImportVm _viewModel;
        private static string _xlsTickerSymbolsOmitted = string.Empty;
        private IEnumerable<Data.Entities.Income> duplicateResults;
        private static int _totalXlsIncomeRecordsToSave = 0;
        private readonly PIMS3Context _ctx;
        private static string _assetsNotAddedListing = string.Empty;
        private string assetIdForPosition = string.Empty;
        private InvestorSvc _investorSvc;


        public ImportFileProcessing(DataImportVm viewModel, PIMS3Context ctx, InvestorSvc investorSvc)
        {
            _viewModel = viewModel;
            _ctx = ctx;
            _investorSvc = investorSvc;
        }


        public bool ValidateVm()
        {
            if ( _viewModel.ImportFilePath == string.Empty || _viewModel.ImportFilePath == null)
                return false;
            else
                return ValidateFileType(_viewModel.ImportFilePath);
        }

       
        private bool ValidateFileType(string filePath)
        {
            var ext = filePath.LastIndexOf('.') + 1;
            return (filePath.Substring(ext).ToUpper() == "XLSX" || filePath.Substring(ext).ToUpper() == "XLS") ? true : false;
        }


        public IEnumerable<Data.Entities.Income> ParseRevenueSpreadsheetForIncomeRecords(string filePath, ImportFileDataProcessing dataAccessComponent, string loggedInvestorId)
        {
            var newIncomeListing = new List<Data.Entities.Income>();
            var incomeDataAccessComponent = new IncomeDataProcessing(_ctx);
            var assetDataAccessComponent = new DataAccess.Asset.AssetData(_ctx);
            IQueryable<string> fetchedPositionId;

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
                        if (!ValidateFileAttributes(true, headerRow) || !ValidateFileType(filePath))
                            return null;

                        var row = workSheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                        var enumerableCells = row as string[] ?? row.ToArray();

                        // 'totalRows' may yield inaccurate results; we'll test for last record, e.g., 'enumerableCells[0] ('Recvd Date').
                        if (!enumerableCells.Any() || enumerableCells[0] == "")
                        {
                            if (_xlsTickerSymbolsOmitted.Any())
                            {
                                dataAccessComponent._exceptionTickers = _xlsTickerSymbolsOmitted;
                                return null;
                            }
                            return newIncomeListing;
                        }
                        
                        var xlsTicker = enumerableCells.ElementAt(3).Trim();
                        var xlsAccount = CommonSvc.ParseAccountTypeFromDescription(enumerableCells.ElementAt(1).Trim());
                       
                        fetchedPositionId = assetDataAccessComponent.FetchPositionId(loggedInvestorId, xlsTicker, xlsAccount).AsQueryable();

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

                        var newIncomeRecord = new Data.Entities.Income
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
                if(ex.Message.Length > 0 )
                    Log.Error("Error found within ImportFileProcessing.ParseRevenueSpreadsheetForIncomeRecords(), due to {0}.", ex.Message);
                else
                    Log.Error("Error found within ImportFileProcessing.ParseRevenueSpreadsheetForIncomeRecords().");

                return null;
            }
        }


        public IEnumerable<AssetCreationVm> ParsePortfolioSpreadsheetForAssetRecords(string filePath, ImportFileDataProcessing dataAccessComponent, string id)
        {
            List<AssetCreationVm> assetsToCreateList = new List<AssetCreationVm>();
            var profileDataAccessComponent = new ProfileDataProcessing(_ctx);
            var existingProfileId = string.Empty;
            var existingAssetClassId = "6215631D-5788-4718-A1D0-A2FC00A5B1A7"; ;
            var newAssetId = Guid.NewGuid().ToString();
            List<Position> positionsToBeSaved = null;

            try
            {
                string lastTickerProcessed = string.Empty;
                var importFile = new FileInfo(filePath);

                using (var package = new ExcelPackage(importFile))
                {
                    ExcelWorksheet workSheet = package.Workbook.Worksheets[0];
                    int totalRows = workSheet.Dimension.End.Row;
                    int totalColumns = workSheet.Dimension.End.Column;
                    AssetCreationVm newAsset = new AssetCreationVm();

                    // Iterate XLS/CSV, ignoring column headings (row 1).
                    for (var rowNum = 2; rowNum <= totalRows; rowNum++)
                    {
                        // Validate XLS
                        var headerRow = workSheet.Cells[1, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                        if (!ValidateFileType(filePath))
                            return null;

                        // Args: Cells[fromRow, fromCol, toRow, toCol]
                        var row = workSheet.Cells[rowNum, 1, rowNum, totalColumns].Select(c => c.Value == null ? string.Empty : c.Value.ToString());
                        var enumerableCells = row as string[] ?? row.ToArray();
                        var positionDataAccessComponent = new PositionDataProcessing(_ctx);

                        // Existing Position-Account implies Profile existence.
                        IQueryable<Asset> positionAccount = positionDataAccessComponent.GetPositionAssetByTickerAndAccount(enumerableCells.ElementAt(1).Trim(), 
                                                                                                                           enumerableCells.ElementAt(0).Trim(), 
                                                                                                                           id);
                        if (!positionAccount.Any())  // No Position-Account found.
                        {
                            IQueryable<Profile> profilePersisted = null;

                            // Are we processing a different ticker symbol?
                            if (lastTickerProcessed.Trim().ToUpper() != enumerableCells.ElementAt(1).Trim().ToUpper())
                            {
                                lastTickerProcessed = enumerableCells.ElementAt(1).Trim().ToUpper();

                                // Do we first have a standard web-derived Profile (via 3rd party) record in our database?
                                profilePersisted = profileDataAccessComponent.FetchDbProfile(enumerableCells.ElementAt(1).Trim(), "");
                                if (profilePersisted == null)
                                {
                                    // Do we secondly have a Customized Profile (via 3rd party) record in our database?
                                    // Check for lost _investorSvc reference.
                                    if(_investorSvc == null) { _investorSvc = new InvestorSvc(_ctx); };
                                    Investor currentInvestor = _investorSvc.GetById(id);
                                    profilePersisted = profileDataAccessComponent.FetchDbProfile(enumerableCells.ElementAt(1).Trim(), currentInvestor.LoginName);
                                }

                                if (profilePersisted != null)
                                {
                                    // Bypassing Profile creation for new Position.
                                    existingProfileId = profilePersisted.First().ProfileId;

                                    if (assetIdForPosition == string.Empty)
                                        assetIdForPosition = newAssetId;

                                    // Are we processing our first XLSX Position record?
                                    if (positionsToBeSaved == null)
                                        positionsToBeSaved = InitializePositions(new List<Position>(), enumerableCells);  
                                    else
                                        positionsToBeSaved = InitializePositions(positionsToBeSaved, enumerableCells);  

                                    // Error seeding collection.
                                    if (positionsToBeSaved == null)
                                        return null;

                                    assetsToCreateList.Add(new AssetCreationVm
                                    {
                                        AssetId = Guid.NewGuid().ToString(),  
                                        AssetClassId = existingAssetClassId,  
                                        InvestorId = id,   
                                        ProfileId = existingProfileId,  
                                        LastUpdate = DateTime.Now, 
                                        Positions = positionsToBeSaved  
                                    });
                                }
                                else
                                {
                                    // Obtain a new Profile via Tiingo API.
                                    var webProfileData = profileDataAccessComponent.BuildProfile(enumerableCells.ElementAt(1).Trim().ToUpper());
                                    if (webProfileData == null)
                                    {
                                        dataAccessComponent._exceptionTickers = enumerableCells.ElementAt(1).Trim().ToUpper();
                                        return null;  // any necessary logging done via component.
                                    }
                                    else
                                    {
                                        // Dividend freq, months, & payDay all may be edited via 'Asset Profile'
                                        // functionality for any created *customized* Profile only. Any standard
                                        // Profile may NOT be edited this way, as this would impact many investors.
                                        Profile newProfile = new Profile
                                        {
                                            ProfileId = webProfileData.ProfileId, 
                                            DividendYield = webProfileData.DividendYield > 0
                                                ? webProfileData.DividendYield
                                                : 0,  
                                            CreatedBy = null, 
                                            DividendRate = webProfileData.DividendRate > 0 ? webProfileData.DividendRate : 0, 
                                            ExDividendDate = webProfileData.ExDividendDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
                                            DividendFreq = webProfileData.DividendFreq ?? "M", 
                                            DividendMonths = null,                            
                                            DividendPayDay = 15,                           
                                            EarningsPerShare = webProfileData.EarningsPerShare > 0  
                                                ? webProfileData.EarningsPerShare 
                                                : 0,
                                            LastUpdate = DateTime.Now, 
                                            PERatio = webProfileData.PERatio > 0 
                                                ? webProfileData.PERatio 
                                                : 0,
                                            TickerDescription = webProfileData.TickerDescription.Trim(), 
                                            TickerSymbol = webProfileData.TickerSymbol.ToUpper().Trim(), 
                                            UnitPrice = webProfileData.UnitPrice 
                                        };
                                        assetIdForPosition = newAssetId;
                                        assetsToCreateList.Add(new AssetCreationVm  
                                        {
                                            AssetId = Guid.NewGuid().ToString(), 
                                            AssetClassId = existingAssetClassId, 
                                            InvestorId = id, //INVESTORID,
                                            ProfileId = newProfile.ProfileId,
                                            LastUpdate = DateTime.Now,
                                            Positions = positionsToBeSaved == null
                                                ? InitializePositions(new List<Position>(), enumerableCells)
                                                : InitializePositions(positionsToBeSaved, enumerableCells),
                                            Profile = newProfile
                                        });
                                        foreach(var asset in assetsToCreateList)
                                        {
                                            if (asset.Positions.Count == 0)
                                            {
                                                Log.Error("Error creating new Position(s) for assetsToCreateList in ImportFileProcessing.ParsePortfolioSpreadsheetForAssetRecords().");
                                                return null;
                                            }
                                        };
                                    }
                                }
                            }
                            else
                            {
                                // Asset header initialization & Profile check bypassed; processing SAME ticker - DIFFERENT account.
                                // Adding to existing AssetCreationVm.Positions.
                                var updatedPositions = InitializePositions(assetsToCreateList.Last().Positions.ToList(), enumerableCells);
                                assetsToCreateList.Last().Positions.Add(updatedPositions.Last());
                            }
                        }
                        else
                        {
                            // Attempted duplicate Position-Account insertion.
                            // TODO: What are we doing with  _assetsNotAddedListing ?
                            _assetsNotAddedListing += enumerableCells.ElementAt(1).Trim() + " ,";
                            lastTickerProcessed = enumerableCells.ElementAt(1).Trim();
                        }
                    }   // end for
                }       // end using
            }
            catch(Exception ex)
            {
                if (ex.Message.Length > 0)
                    Log.Error("Error found within ImportFileProcessing.ParsePortfolioSpreadsheetForAssetRecords(), due to {0}.", ex.Message);
                else
                    Log.Error("Error found within ImportFileProcessing.ParsePortfolioSpreadsheetForAssetRecords().");

                return null;
            }

            return assetsToCreateList;
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


        private List<Position> InitializePositions(List<Position> initializedPositions, string[] currentRow)
        {
            // AssetId must never be null & always point to a referencing Asset when creating new Position(s).
            if (assetIdForPosition == string.Empty || assetIdForPosition == null)
                return null;

            // Build Position listing.
            if (initializedPositions == null) return null; 
            if (currentRow == null) throw new ArgumentNullException("currentRow");

            var mktPrice = decimal.Parse(currentRow.ElementAt(4));
            var acctDataAccessComponent = new AccountDataProcessing(_ctx);
            Position newPosition = null;
            
            try
            {
                newPosition = new Position
                {
                    PositionId = Guid.NewGuid().ToString(), 
                    AccountTypeId = acctDataAccessComponent.GetAccountTypeId(currentRow.ElementAt(0)).First().ToString().Trim(), 
                    AssetId = assetIdForPosition,
                    Fees = 0M, 
                    LastUpdate = DateTime.Now, 
                    PositionDate = DateTime.Now, 
                    Quantity = int.Parse(currentRow.ElementAt(3)),
                    Status = "A",  
                    UnitCost = decimal.Parse(currentRow.ElementAt(4))  
                };
            }
            catch (Exception)
            {
                // TODO: Log error msg.
                return null;
            }

            initializedPositions.Add(newPosition);
            return initializedPositions;
        }


        public decimal CalculateDividendYield(decimal divRate, decimal unitPrice)
        {
            var yield = divRate * 12 / unitPrice * 100;
            return decimal.Round(decimal.Parse(yield.ToString(CultureInfo.InvariantCulture)), 2);
        }

       


    }
}
