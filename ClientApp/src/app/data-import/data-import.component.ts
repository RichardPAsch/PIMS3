import { Component } from "@angular/core";



@Component({
  selector: 'data-import',
  templateUrl: './data-import.component.html'
})

export class DataImportComponent {

}

/*  Reference code (JS) from PIMS:
 *  ==============================
 *    dataImportSvc:
 *    
 *  (function () {

    "use strict";
    angular
        .module("incomeMgmt.core")
        .factory("dataImportSvc", dataImportSvc);

    dataImportSvc.$inject = ["$resource", 'appSettings'];

    function dataImportSvc($resource, appSettings) {
        var vm = this;
        vm.importFileControllerUrl = appSettings.serverPath + "/Pims.Web.Api/api/ImportFile";

        function processImportFileModel(importFileDataModel, ctrl) {
            $resource(vm.importFileControllerUrl).save(importFileDataModel).$promise.then(
                function (responseMsg) {
                    var respObj = responseMsg;
                    ctrl.postAsyncProcessImportFile(responseMsg);
                },
                function (err) {
                    alert("Unable to process XLSX import file for ticker symbol(s) :\n" + err.data.message + "\nValidate that : \n1) submitted file type is correct, and/or \n2) there are no duplicate or missing POSITION-ACCOUNT(S).");
                }
             );
        }

        // API
        return {
            processImportFileModel: processImportFileModel
        }
    }
}());

// dataImportCtrl:

(function () {
    "use strict";

    angular
        .module("incomeMgmt.dataImport")
        .controller("dataImportCtrl", dataImportCtrl);

    dataImportCtrl.$inject = ["dataImportSvc"];

    function dataImportCtrl(dataImportSvc) {
        var vm = this;
        // [Ex: valid local path: C:\Downloads\FidelityXLS\Portfolio_RevenueTEST_1_Fidelity.xlsx]
        var filePathRegExpr = "^(([a-zA-Z]\\:)|(\\\\))(\\\\{1}|((\\\\{1})[^\\\\]([^/:*?<>\"|]*))+)$";
        vm.importFilePath = ""; 
        vm.importDataType = "revenue";
        vm.importFileModel = {
                ImportFilePath: "",
                IsRevenueData: true
        }
        

        vm.processImportFile = function () {
            if (vm.importDataType === "") {
                alert("Data import terminated; please select an import file type.");
                return;
            }

            if (vm.importFilePath.match(filePathRegExpr)) {
                vm.importFileModel.ImportFilePath = vm.importFilePath;
                vm.importFileModel.IsRevenueData = vm.importDataType === "revenue" ? true : false;
                var result = dataImportSvc.processImportFileModel(vm.importFileModel, this);
            } else {
                alert("Invalid file path submitted for import file.");
            }
        }
        

        vm.cancelImport = function () {
        }

        // ** 10.24.18 - this fx originally commented. **
        // Async WebApi service calls
        //vm.postCheckRevenueDuplicate = function (duplicateFound) {

            //if (duplicateFound) {
            //    vm.isDuplicateIncome = duplicateFound;
            //    alert("Unable to save revenue; duplicate entry found for Asset: \n" +
            //           vm.selectedTicker.trim().toUpperCase() +
            //           "\n using account: " + vm.selectedAccountType +
            //          "\n on: " + $filter('date')(vm.incomeDateReceived, 'M/dd/yyyy'));
            //    return null;
            //}

            //// TODO: Fx name above should reflect save. Duplicate code - move to service.
            //var incomeBuild = createAssetWizardSvc.getBaseRevenue(); // fetch new instance to avoid duplicates.
            //var today = new Date();
            
            //incomeBuild.AcctType = vm.selectedAccountType;
            //incomeBuild.AmountRecvd = createAssetWizardSvc.formatCurrency(vm.incomeAmtReceived, 2);
            //incomeBuild.DateReceived = $filter('date')(vm.incomeDateReceived, 'M/dd/yyyy');
            //incomeBuild.AssetId = positionCreateSvc.getAssetId(positionData, vm.selectedTicker);
            //incomeBuild.AmountProjected = 0;
            //incomeBuild.DateUpdated = incomeMgmtSvc.formatDate(today);
            //incomeBuild.Url = createAssetWizardSvc.getBasePath
            //                                        + "Asset/"
            //                                        + vm.selectedTicker.trim().toUpperCase()
            //                                        + "/Income/"
            //                                        + incomeBuild.AcctType.toUpperCase();

            //// Extended properties needed for validation checking in saveRevenue().
            //incomeBuild.TickerSymbol = vm.selectedTicker.trim().toUpperCase();
            
            //var datePositionAdded = positionCreateSvc.getPositionAddDate(positionData, incomeBuild.TickerSymbol, incomeBuild.AcctType);
            //var formattedPosDate = new Date(datePositionAdded.toString());

            //incomeBuild.PositionAddDate = incomeMgmtSvc.formatDate(formattedPosDate);

            //incomeCreateSvc.saveRevenue(incomeBuild, vm);
            //return null;
//        }

//        vm.postAsyncProcessImportFile = function (responseModel) {
//            alert(responseModel.responseMsg);
//        }
//    }


//}());


*/
