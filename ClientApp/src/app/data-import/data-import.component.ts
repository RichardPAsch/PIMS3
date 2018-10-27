import { Component } from "@angular/core";
import { FormControl, FormGroup, FormBuilder, Validators } from '@angular/forms';


@Component({
  selector: 'data-import',
  templateUrl: './data-import.component.html',
  styleUrls: ['./data-import.component.css']
})


export class DataImportComponent {
  private testMsg: string = "Sample test message.";
  private filePathRegExpr: string = "^(([a-zA-Z]\\:)|(\\\\))(\\\\{1}|((\\\\{1})[^\\\\]([^/:*?<>\"|]*))+)$";
  //private importFilePath: string = "";
  //private importDataType: string = "revenue";
  private importFileVm: any = { ImportFilePath: "", IsRevenueData: true };

  // Constructor of FormControl sets its initial value; creation provides immediate access to listen for,
  // update, and validate the state of the form input.

  dataImportForm = this.frmBldr.group({
    importFilePath: ['', Validators.required],
    importDataType: this.frmBldr.group({
      revenueType: ['revenue'],
      assetType: ['']
    }),
    importActions: this.frmBldr.group({
      btnImport: [''],
      btnCancel: ['']
    })
  });
  
  
  constructor(private frmBldr: FormBuilder) { };


  public processImportFile() {

    alert("in processImportFile() with value(s): " +  this.dataImportForm.value);
    //if (this.importDataType === "") {
    //  alert("Data import terminated; please select an import file type.");
    //  return;
    //}

    //if (this.importFilePath.match(this.filePathRegExpr)) {
    //  this.importFileVm.ImportFilePath = this.importFilePath;
    //  this.importFileVm.IsRevenueData = this.importDataType === "revenue" ? true : false;

      // Send to backend for processing here: WIP
      //     -> ImportFileController.cs / ImportFileControllerReference.txt

      //var result = dataImportSvc.processImportFileModel(this.importFileModel, this);
    //} else {
    //    alert("Invalid file path submitted for import file.");
    //}

  }

  public cancelImportFile() {
    alert("in cancelImportFile() with value(s).");
  }


    // 10.25.18 - Notes:
    // WebClient should only be responsible for validating file type to be imported.
    // Build UI - WIP(70% complete)
    // If ok, pass import file to backend (MVC) for processing.WIP (5% complete)

}



/*  Client-side reference code (JS) from PIMS:
 *  ===========================================
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
        var filePathRegExpr = "^(([a-zA-Z]\\:)|(\\\\))(\\\\{1}|((\\\\{1})[^\\\\]([^/:*?<>\"|]*))+)$";   //done
        vm.importFilePath = "";                                                                         //done
        vm.importDataType = "revenue";                                                                  //done
        vm.importFileModel = {  //done
                ImportFilePath: "",
                IsRevenueData: true
        }
        

        vm.processImportFile = function () {                                                            // done
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
        

        vm.cancelImport = function () {                                                                 // to be implemented
        }

        // ** 10.24.18 - this fx originally commented. **                                               // to be implemented ?
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
