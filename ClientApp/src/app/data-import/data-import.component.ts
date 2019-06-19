import { Component } from "@angular/core";
import { FormBuilder, Validators } from '@angular/forms';
import { DataImportVm } from './data-importVm';
import { DataImportService } from './data-import.service';
import { HttpErrorResponse, HttpClient, HttpHandler } from "@angular/common/http";


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
    private importFileVm: DataImportVm = {
        importFilePath: "",
        isRevenueData: true,
        recordsSaved: 0,
        amountSaved: 0,
        exceptionTickers: "",
        miscMessage: ""
    };
    private submittedFile: string = "";


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

        let recordsProcessed: number;
        let totalProcessed: number;
        
        if (this.dataImportForm.value.importFilePath == "") {
            alert("Data import terminated: missing import file path.");
            return;
        } else {
            this.submittedFile = this.dataImportForm.value.importFilePath.trim();
            if (this.submittedFile.indexOf(':') == -1 && ((this.getFileExtension(this.submittedFile) != "XLSX") || this.getFileExtension(this.submittedFile) != "XLS")) {
                alert("Data import terminated: invalid file type submitted, please submit data as a spreadsheet (xlsx/xls).");
                return;
            } else {
                this.importFileVm.importFilePath = this.dataImportForm.value.importFilePath;
                this.importFileVm.isRevenueData = this.dataImportForm.value.importDataType.importType === "revenue" ? true : false;

                // Backend API logic to handle processing import file type.
                // ** BASE_URL defined in main.ts **
                if (this.importFileVm.isRevenueData) {

                    this.svc.postImportFileData(this.importFileVm)
                        .subscribe(resp => {
                            if (resp.isRevenueData && resp.recordsSaved > 0) {
                                recordsProcessed = resp.recordsSaved;
                                totalProcessed = resp.amountSaved;
                                alert("Successfully saved  " + recordsProcessed + " XLSX/XLS income records, \nfor a total of $" + totalProcessed);
                            }
                        },
                        (err: HttpErrorResponse) => {
                            if (err.error instanceof Error) {
                                // TODO: 11.5.18 - just have 1 alert, but use logging to log different issues.
                                alert('Error saving import data (network?) due to: ' + err.error.message);
                            } else {
                                //Backend returns unsuccessful response codes such as 404, 500 etc.
                                alert('Error saving Income import data (server?) with status of : ' + err.message);
                            }
                        });
                } else {
                    // New Position import data.
                    // sample: C:\Development\VS2017\PIMS3_TestData\Asset_Files\Portfolio_Positions_Dec_21_Test1_MissingTicker.xlsx
                    this.svc.postImportFileData(this.importFileVm)
                        .subscribe(resp => {
                            if (!resp.isRevenueData && resp.recordsSaved > 0) {
                                recordsProcessed = resp.recordsSaved;
                                alert("Successfully added portfolio data for \n" + recordsProcessed + " new Position(s):  " + resp.miscMessage);
                            }
                        },
                        (err: HttpErrorResponse) => {
                            if (err.error instanceof Error) {
                                // TODO: 11.5.18 - just have 1 alert, but use logging to log different issues.
                                alert('Error saving import data (network?) due to: ' + err.error.message);
                            } else {
                                //Backend returns unsuccessful response codes such as 404, 500 etc.
                                alert('Error saving Position import data (server?) due to :\n ' + err.error.message);
                            }
                        });
                }
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

}

