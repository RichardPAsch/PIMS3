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


namespace PIMS3.BusinessLogic.ImportData
{
    public class ImportFileProcessing
    {
        private DataImportVm _viewModel;
        private static string _xlsTickerSymbolsOmitted = string.Empty;
        private IEnumerable<Income> duplicateResults;
        //private static OkNegotiatedContentResult<List<AssetIncomeVm>> _existingInvestorAssets;
        private static int _totalXlsIncomeRecordsToSave = 0;
        private readonly PIMS3Context _ctx;
        private static string _assetsNotAddedListing = string.Empty;
        private string newAssetId = string.Empty;

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
            var existingAssetClassId = string.Empty;
            // Id to be fetched once security is implemented. ** 12.27.18
            var currentInvestorId = "CF256A53-6DCD-431D-BC0B-A810010F5B88"; // me
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
                        // Existing Position-Account implies a Profile existence.
                        var positionAccountExists = positionDataAccessComponent.GetPositionByTickerAndAccount(enumerableCells.ElementAt(1).Trim(), enumerableCells.ElementAt(0).Trim());

                        if (!positionAccountExists)
                        {
                            Profile assetProfilePersisted = null;

                            // Are we processing a different ticker symbol?
                            if (lastTickerProcessed.Trim().ToUpper() != enumerableCells.ElementAt(1).Trim().ToUpper())
                            {
                                // Do we have an existing Profile ? Although no Position-Account exist for (XLSX) record in question, we'll 
                                // first check for Profile existence affiliated with another investor.
                                assetProfilePersisted = profileDataAccessComponent.FetchDbProfile(enumerableCells.ElementAt(1).Trim()).FirstOrDefault();

                                // 12.26.18:
                                // ** Re-examine how we want/need to initialize AssetCreationVm ? Have a seperate Vm for Asset & Position &
                                //    ctx.SaveChanges() on each?
                                //    Need to get Guids for AssetClassId, ProfileId, PositionId, & AssetTypeId. See in original PIMS code:
                                //    AssetController.CreateNewAsset() 
                                if (assetProfilePersisted != null)
                                {
                                    // Bypassing Profile creation.
                                    existingProfileId = assetProfilePersisted.ProfileId;
                                    newAssetId = CommonSvc.GenerateGuid();
                                    // TODO: AssetClassId hard-coded to default: 'common stock'. Make available via XLSX? ** 12.27.18
                                    existingAssetClassId = "6215631D-5788-4718-A1D0-A2FC00A5B1A7";

                                    // Are we processing our first XLSX record?
                                    if(positionsToBeSaved == null)
                                        positionsToBeSaved = InitializePositions(new List<Position>(), enumerableCells);
                                    else
                                        positionsToBeSaved = InitializePositions(positionsToBeSaved, enumerableCells);

                                    // Error seeding collection.
                                    if (positionsToBeSaved == null)
                                        return null;
                                    
                                    assetsToCreateList.Add(new AssetCreationVm
                                    {
                                        AssetId = newAssetId,
                                        AssetClassId = existingAssetClassId,
                                        InvestorId = currentInvestorId,
                                        ProfileId = existingProfileId,
                                        LastUpdate = DateTime.Now,
                                        Positions = positionsToBeSaved // collection updated via re-assignment; able to add 2 Positions?
                                    });
                                }
                                else
                                {
                                    // Obtain a new Profile via Tiingo API.
                                    var webProfileData = profileDataAccessComponent.FetchWebProfile(enumerableCells.ElementAt(1).Trim().ToUpper());
                                    if (webProfileData == null)
                                        return null;
                                    else
                                    {
                                        Profile newProfile = new Profile
                                        {
                                            ProfileId = webProfileData.Result.ProfileId,
                                            DividendYield = webProfileData.Result.DividendYield,
                                            CreatedBy = null,
                                            DividendRate = webProfileData.Result.DividendRate > 0 ? webProfileData.Result.DividendRate : 0,
                                            ExDividendDate = webProfileData.Result.ExDividendDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                                            DividendFreq = webProfileData.Result.DividendFreq ?? "M", // TODO: allow to change
                                            DividendMonths = null,
                                            DividendPayDay = 15,    // TODO: allow to change
                                            EarningsPerShare = webProfileData.Result.EarningsPerShare,
                                            LastUpdate = DateTime.Now,
                                            PERatio = webProfileData.Result.PERatio,
                                            TickerDescription = webProfileData.Result.TickerDescription.Trim(),
                                            TickerSymbol = webProfileData.Result.TickerSymbol.ToUpper().Trim(),
                                            UnitPrice = webProfileData.Result.UnitPrice
                                        };

                                        assetsToCreateList.Add(new AssetCreationVm
                                        {
                                            AssetId = newAssetId,
                                            AssetClassId = existingAssetClassId,
                                            InvestorId = currentInvestorId,
                                            ProfileId = newProfile.ProfileId,
                                            LastUpdate = DateTime.Now,
                                            Positions = positionsToBeSaved == null 
                                                ? InitializePositions(new List<Position>(), enumerableCells)
                                                : InitializePositions(positionsToBeSaved, enumerableCells),
                                            Profile = newProfile
                                        });
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
                                    AssetId = assetsToCreateList.Last().Positions.Last().AssetId,
                                    AssetClassId = assetsToCreateList.Last().AssetClassId,
                                    InvestorId = assetsToCreateList.Last().InvestorId,
                                    ProfileId = assetsToCreateList.Last().ProfileId,
                                    LastUpdate = DateTime.Now,
                                    Positions = InitializePositions(positionsToBeSaved, enumerableCells) 
                                });
                            }
                        }
                        else
                        {
                            // Attempted duplicate Position-Account insertion.
                            _assetsNotAddedListing += enumerableCells.ElementAt(1).Trim() + " ,";
                            lastTickerProcessed = enumerableCells.ElementAt(1).Trim();
                        }
                    }// end for
                } // end using
            }
            catch
            {
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
            // Build Position listing.
            if (initializedPositions == null) return null; 
            if (currentRow == null) throw new ArgumentNullException("currentRow");

            var mktPrice = decimal.Parse(currentRow.ElementAt(4));
            var acctDataAccessComponent = new AccountDataProcessing(_ctx);
            //var valuation = Utilities.CalculateValuation(decimal.Parse(currentRow.ElementAt(4)), decimal.Parse(currentRow.ElementAt(3)));
            //decimal fees = 0;
            //var costBasis = Utilities.CalculateCostBasis(fees, valuation);
            //var unitCost = Utilities.CalculateUnitCost(costBasis, decimal.Parse(currentRow.ElementAt(3)));

            //var newPositions = initializedPositions;

            var newPosition = new Position
            {
                PositionId = CommonSvc.GenerateGuid(),
                AccountTypeId = acctDataAccessComponent.GetAccountTypeId(currentRow.ElementAt(4)).ToString(),
                AssetId = newAssetId,
                Fees = 0M,
                LastUpdate = DateTime.Now,
                PositionDate = DateTime.Now,
                Quantity = decimal.Parse(currentRow.ElementAt(3)),
                //PreEditPositionAccount = currentRow.ElementAt(0),
                //PostEditPositionAccount = currentRow.ElementAt(0),
                Status = "A"
                //Qty = decimal.Parse(currentRow.ElementAt(3)),
                //UnitCost = costBasis,
                // TODO: Allow user to assign date position added.
                // Position add date will not have been assigned, therefore assign an unlikely date & allow for investor update via UI.
                //DateOfPurchase = new DateTime(1950, 1, 1),
                //DatePositionAdded = null,
                //Url = "",
                //LoggedInInvestor = _identityService.CurrentUser,
                //ReferencedAssetId = Guid.NewGuid(),            // initialized during Asset creation
                //ReferencedAccount = new AccountTypeVm
                //{
                //    AccountTypeDesc = currentRow.ElementAt(0),
                //    KeyId = Guid.NewGuid(), // Guid for AccountType, initialized during Asset creation
                //    Url = ""
                //},
                //ReferencedTransaction = new TransactionVm
                //{
                //    PositionId = Guid.NewGuid(),
                //    TransactionId = Guid.NewGuid(),
                //    Units = decimal.Parse(currentRow.ElementAt(3)),
                //    TransactionEvent = "C",
                //    MktPrice = mktPrice,
                //    Fees = fees,
                //    UnitCost = unitCost,
                //    CostBasis = costBasis,
                //    Valuation = valuation,
                //    DateCreated = DateTime.Now,
                //    DatePositionCreated = null
                //}
            };

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
