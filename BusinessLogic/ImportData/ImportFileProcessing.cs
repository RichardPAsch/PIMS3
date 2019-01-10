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
using System.Threading.Tasks;

namespace PIMS3.BusinessLogic.ImportData
{
    public class ImportFileProcessing
    {
        private DataImportVm _viewModel;
        private static string _xlsTickerSymbolsOmitted = string.Empty;
        private IEnumerable<Income> duplicateResults;
        private static int _totalXlsIncomeRecordsToSave = 0;
        private readonly PIMS3Context _ctx;
        private static string _assetsNotAddedListing = string.Empty;
        private string assetIdForPosition = string.Empty;
        // 12.27.18 - Temporary assignment until security implemented.
        const string INVESTORID = "CF256A53-6DCD-431D-BC0B-A810010F5B88"; // RPA

        // ** Needs refactoring & testing with new Position data. *** 12.31.18

        public ImportFileProcessing(DataImportVm viewModel, PIMS3Context ctx)
        {
            _viewModel = viewModel;
            _ctx = ctx;
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


        public IEnumerable<Income> ParseRevenueSpreadsheetForIncomeRecords(string filePath, ImportFileDataProcessing dataAccessComponent)
        {
            var newIncomeListing = new List<Income>();
            var incomeDataAccessComponent = new DataAccess.Income.IncomeData(_ctx);
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


        public IEnumerable<AssetCreationVm> ParsePortfolioSpreadsheetForAssetRecords(string filePath, ImportFileDataProcessing dataAccessComponent)
        {
            List<AssetCreationVm> assetsToCreateList = new List<AssetCreationVm>();
            var profileDataAccessComponent = new ProfileDataProcessing(_ctx);
            var existingProfileId = string.Empty;
            // TODO: AssetClassId hard-coded to default: 'common stock'. Make available via XLSX? ** 12.27.18
            var existingAssetClassId = "6215631D-5788-4718-A1D0-A2FC00A5B1A7"; ;
            var newAssetId = Guid.NewGuid().ToString();
            List<Position> positionsToBeSaved = null;

            try
            {
                var lastTickerProcessed = string.Empty;
                var importFile = new FileInfo(filePath);

                using (var package = new ExcelPackage(importFile))
                {
                    var workSheet = package.Workbook.Worksheets[0];
                    var totalRows = workSheet.Dimension.End.Row;
                    var totalColumns = workSheet.Dimension.End.Column;
                    var newAsset = new AssetCreationVm();

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
                        var positionAccount = positionDataAccessComponent.GetPositionAssetByTickerAndAccount(enumerableCells.ElementAt(1).Trim(), enumerableCells.ElementAt(0).Trim());
                        if (!positionAccount.Any())  // No Position-Account
                        {
                            IQueryable<Profile> profilePersisted = null;

                            // Are we processing a different ticker symbol?
                            if (lastTickerProcessed.Trim().ToUpper() != enumerableCells.ElementAt(1).Trim().ToUpper())
                            {
                                profilePersisted = profileDataAccessComponent.FetchDbProfile(enumerableCells.ElementAt(1).Trim());

                                // 12.26.18:
                                // ** Re-examine how we want/need to initialize AssetCreationVm ? Have a seperate Vm for Asset & Position &
                                //    ctx.SaveChanges() on each?
                                //    Need to get Guids for AssetClassId, ProfileId, PositionId, & AssetTypeId. See in original PIMS code:
                                //    AssetController.CreateNewAsset() 
                                if (profilePersisted != null)
                                {
                                    // Bypassing Profile creation for new Position.
                                    existingProfileId = profilePersisted.First().ProfileId; // ok
                                    //newAssetId = Guid.NewGuid().ToString();   // ok
                                    // TODO: AssetClassId hard-coded to default: 'common stock'. Make available via XLSX? ** 12.27.18
                                    //existingAssetClassId = "6215631D-5788-4718-A1D0-A2FC00A5B1A7"; // ok

                                    // Are we processing our first Position XLSX record?
                                    if(positionsToBeSaved == null)
                                        positionsToBeSaved = InitializePositions(new List<Position>(), enumerableCells);  // ok
                                    else
                                        positionsToBeSaved = InitializePositions(positionsToBeSaved, enumerableCells);  // ok

                                    // Error seeding collection.
                                    if (positionsToBeSaved == null)
                                        return null;  // ok
                                    
                                    assetsToCreateList.Add(new AssetCreationVm
                                    {
                                        AssetId = newAssetId,  // ok
                                        AssetClassId = existingAssetClassId,  // ok
                                        InvestorId = INVESTORID,  // ok
                                        ProfileId = existingProfileId,  // ok
                                        LastUpdate = DateTime.Now, // ok
                                        Positions = positionsToBeSaved // collection updated via re-assignment; able to add 2 Positions?  // ok
                                    });
                                }
                                else
                                {
                                    // Obtain a new Profile via Tiingo API.
                                    var webProfileData = profileDataAccessComponent.FetchWebProfile(enumerableCells.ElementAt(1).Trim().ToUpper());
                                    //var webProfileData = profileDataAccessComponent.FetchWebProfile(enumerableCells.ElementAt(1).Trim().ToUpper()); // ok
                                    if (webProfileData == null) 
                                        return null;  // any necessary logging done via component.
                                    else
                                    {
                                        Profile newProfile = new Profile
                                        {
                                            ProfileId = webProfileData.ProfileId, // ok
                                            DividendYield = webProfileData.DividendYield > 0
                                                ? webProfileData.DividendYield
                                                : 0,  // always valid ?
                                            CreatedBy = null, // ok
                                            DividendRate = webProfileData.DividendRate > 0 ? webProfileData.DividendRate : 0, // ok
                                            ExDividendDate = webProfileData.ExDividendDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), // ok
                                            DividendFreq = webProfileData.DividendFreq ?? "M", // TODO: allow user to change ok
                                            DividendMonths = null, // TODO: allow user to change ok
                                            DividendPayDay = 15,    // TODO: allow user to change  ok
                                            EarningsPerShare = webProfileData.EarningsPerShare > 0  // ok
                                                ? webProfileData.EarningsPerShare 
                                                : 0,
                                            LastUpdate = DateTime.Now, // ok
                                            PERatio = webProfileData.PERatio > 0 // ok
                                                ? webProfileData.PERatio 
                                                : 0,
                                            TickerDescription = webProfileData.TickerDescription.Trim(), // ok
                                            TickerSymbol = webProfileData.TickerSymbol.ToUpper().Trim(), // ok
                                            UnitPrice = webProfileData.UnitPrice // ok
                                        };
                                        assetIdForPosition = newAssetId;
                                        assetsToCreateList.Add(new AssetCreationVm  // ok
                                        {
                                            AssetId = newAssetId, 
                                            AssetClassId = existingAssetClassId, 
                                            InvestorId = INVESTORID,
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
                                                // TODO: Log error.
                                                return null;
                                            }
                                        };
                                    }
                                }
                            }
                            else
                            {
                                // No need to re-check for Profile existence.
                                // Asset header initialization bypassed; processing same ticker - different account.
                                // Positions updated via re-assignment; able to add 2 Positions?
                                assetsToCreateList.Add(new AssetCreationVm
                                {
                                    AssetId = assetsToCreateList.Last().Positions.Last().AssetId, // ok
                                    AssetClassId = assetsToCreateList.Last().AssetClassId, // ok
                                    InvestorId = assetsToCreateList.Last().InvestorId, // ok 
                                    ProfileId = assetsToCreateList.Last().ProfileId, // ok
                                    LastUpdate = DateTime.Now, // ok
                                    Positions = InitializePositions(positionsToBeSaved, enumerableCells) // ok
                                });
                            }
                        }
                        else
                        {
                            // Attempted duplicate Position-Account insertion.
                            // TODO: What are we doing with  _assetsNotAddedListing ?
                            _assetsNotAddedListing += enumerableCells.ElementAt(1).Trim() + " ,";
                            lastTickerProcessed = enumerableCells.ElementAt(1).Trim();
                        }
                    }// end for
                } // end using
            }
            catch(Exception ex)
            {
                // TODO: Log exception.
                var debug = ex.Message;
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
            //var valuation = Utilities.CalculateValuation(decimal.Parse(currentRow.ElementAt(4)), decimal.Parse(currentRow.ElementAt(3)));
            //decimal fees = 0;
            //var costBasis = Utilities.CalculateCostBasis(fees, valuation);
            //var unitCost = Utilities.CalculateUnitCost(costBasis, decimal.Parse(currentRow.ElementAt(3)));

            //var newPositions = initializedPositions;
            //var test = acctDataAccessComponent.GetAccountTypeId(currentRow.ElementAt(0)).First().ToString().Trim();

            try
            {
                newPosition = new Position
                {
                    PositionId = "", 
                    AccountTypeId = acctDataAccessComponent.GetAccountTypeId(currentRow.ElementAt(0)).First().ToString().Trim(), // ok
                    AssetId = assetIdForPosition,
                    Fees = 0M, // ok
                    LastUpdate = DateTime.Now, // ok
                    PositionDate = DateTime.Now, // ok
                    Quantity = int.Parse(currentRow.ElementAt(3)),
                    Status = "A",  // ok
                    UnitCost = decimal.Parse(currentRow.ElementAt(4))  // ok
                };
            }
            catch (Exception)
            {
                // TODO: Log error msg.
                return null;
            }

            initializedPositions.Add(newPosition);
            return initializedPositions;


            /* Old code:
                PreEditPositionAccount = currentRow.ElementAt(0),
                PostEditPositionAccount = currentRow.ElementAt(0),
                Qty = decimal.Parse(currentRow.ElementAt(3)),
                UnitCost = costBasis,
                 TODO: Allow user to assign date position added.
                 Position add date will not have been assigned, therefore assign an unlikely date & allow for investor update via UI.
                DateOfPurchase = new DateTime(1950, 1, 1),
                DatePositionAdded = null,
                Url = "",
                LoggedInInvestor = _identityService.CurrentUser,
                ReferencedAssetId = Guid.NewGuid(),            // initialized during Asset creation
                ReferencedAccount = new AccountTypeVm
                {
                    AccountTypeDesc = currentRow.ElementAt(0),
                    KeyId = Guid.NewGuid(), // Guid for AccountType, initialized during Asset creation
                    Url = ""
                },
                ReferencedTransaction = new TransactionVm
                {
                    PositionId = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    Units = decimal.Parse(currentRow.ElementAt(3)),
                    TransactionEvent = "C",
                    MktPrice = mktPrice,
                    Fees = fees,
                    UnitCost = unitCost,
                    CostBasis = costBasis,
                    Valuation = valuation,
                    DateCreated = DateTime.Now,
                    DatePositionCreated = null
                }
            */
        }


        public decimal CalculateDividendYield(decimal divRate, decimal unitPrice)
        {
            var yield = divRate * 12 / unitPrice * 100;
            return decimal.Round(decimal.Parse(yield.ToString(CultureInfo.InvariantCulture)), 2);
        }

       


    }
}
