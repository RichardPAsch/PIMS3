import { Component } from "@angular/core";
import { FormBuilder, Validators } from '@angular/forms';
import { DataImportVm } from './data-importVm';
import { DataImportService } from './data-import.service';
import { Observable } from "rxjs";
import { HttpErrorResponse, HttpClient, HttpHandler } from "@angular/common/http";
//import { MessageService } from "../message.service";

/* *********** Debug note: 11.24.18 ******************
 * Experiment with 1.ng build (** src edits not reflected via running npm start ***) &
 *                 2.npm start, when making code changes, until service erorr is identified.
 * Error occurs with DI of DataImportService here.
 * Re-examine use of parameters in ctor inside of DataImportService()!!
*/


@Component({
    selector: 'data-import',
    templateUrl: './data-import.component.html',
    styleUrls: ['./data-import.component.css'],
    providers: []  // An array of providers for services that this component requires; provided via ctor DI.
})


export class DataImportComponent {
    private testMsg: string = "Sample test message.";
    //private filePathRegExpr: string = "^(?:[\w]\:|\\)(\\[a-z_\-\s0-9\.]+)+\.(xls|xlsx)$";
    private importFileVm: DataImportVm = { filePath: "", isRevenueData: true, recordsSaved: 0, amountSaved: 0 };
    private submittedFile: string = "";
    //private submittedImportFile: Observable<DataImportVm>;
    //private results: any;


  // Constructor of FormControl sets its initial value; creation provides immediate access to listen for,
  // update, and validate the state of the form input.

  dataImportForm = this.frmBldr.group({
    importFilePath: ['', Validators.required],
    importDataType: this.frmBldr.group({
      importType: ['revenue', Validators.required]
    }),
    importActions: this.frmBldr.group({
      btnImport: [''],
      btnCancel: ['']
    })
  });
  
  
    constructor(private frmBldr: FormBuilder, private svc: DataImportService) { }; 


    public processImportFile() {
        
        // Test data file_1:  C:\Development\VS2017\PIMS3_TestData\2018AUG_Revenue2Recs.xlsx
        if (this.dataImportForm.value.importFilePath == "") {
            alert("Data import terminated: missing import file path.");
            return;
        } else {
            this.submittedFile = this.dataImportForm.value.importFilePath.trim();
            if (this.submittedFile.indexOf(':') == -1 && ((this.getFileExtension(this.submittedFile) != "XLSX") || this.getFileExtension(this.submittedFile) != "XLS")) {
                alert("Data import terminated: invalid file type submitted, please submit data as a spreadsheet (xlsx/xls).");
                return;
            } else {
                alert("processing...");
                this.importFileVm.filePath = this.dataImportForm.value.importFilePath;
                this.importFileVm.isRevenueData = this.dataImportForm.value.importDataType.importType === "revenue" ? true : false;

                // 11.30.18 - bone up on Angular services!
                // Backend API logic to handle processing import file type.
                // ** BASE_URL defined in main.ts **
                //let svc = new DataImportService(new HttpClient(null), "http://localhost/", new MessageService());
                this.svc.postImportFileData(this.importFileVm)
                        .subscribe(resp => {
                            if (resp.isRevenueData && resp.recordsSaved > 0) {
                                let recordsProcessed = resp.recordsSaved;
                                let totalProcessed = resp.amountSaved;
                                alert("Successfully saved " + recordsProcessed + " for a total of $" + totalProcessed);
                            } else {
                                alert("asset processing debug here - TBI");
                                // asset processing to be implemented.
                            }
                        },
                        (err: HttpErrorResponse) => {
                            if (err.error instanceof Error) {
                                // TODO: 11.5.18 - just have 1 alert, but use logging to log different issues.
                                //A client-side or network error occurred.				 
                                alert('Error saving import data (network?) due to: ' +  err.error.message);
                            } else {
                                //Backend returns unsuccessful response codes such as 404, 500 etc.
                                alert('Error saving import data (server?) with status of : ' + err.status);
                                //console.log('Backend returned status code: ', err.status);
                                //console.log('Response body:', err.error);
                            }
                        });
                alert("processing completed.");
            }
        }
    }


  public cancelImportFile() {
    alert("in cancelImportFile() with value(s).");
  }


  private getFileExtension(filePath: string) {
    let submittedFilePath = filePath;
    return submittedFilePath.substring(submittedFilePath.indexOf(".") + 1).toUpperCase();
  }


    // 10.25.18 - Notes:
    // WebClient should only be responsible for validating file type to be imported. - done
    // Build UI - WIP(85% complete)
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
