import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataImportVm } from './data-importVm';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';


let httpHeaders = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Authorization', 'this.basic')
    .set('Cache-Control', 'no-cache');

const httpOptions = {
    headers: httpHeaders
};


// Creates a provider for the service; providedIn: 'root' specifies that the service should be provided in the root injector.
// When you add a service provider to the root application injector, it’s available throughout the app.
// Should always provide your service in the root injector unless there is a case where you want the service to be available
// only if the consumer imports a particular @NgModule.
@Injectable(
    { providedIn: "root" }
)
export class DataImportService {

    Id: string;
    baseUrl: string;
    webApi: string;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor'));
        this.Id = investor.id;
        this.webApi = this.baseUrl + '/api/ImportFile/' + this.Id; 
    }
    
    /* All HttpClient methods return an RxJS Observable of something.
     * Observables' subscribe() is responsible for handling errors.
     * You can use pipes to link operators (RxJS) together. Pipes let you combine multiple functions into a single function.
     * The pipe() function takes as its arguments the functions you want to combine, and returns a new function that,
     * when executed, runs the composed functions in sequence.
       Also see: https://angular.io/tutorial/toh-pt6
    */

    postImportFileData(importFileToProcess: DataImportVm): Observable<DataImportVm> {
        
        return this.http.post<DataImportVm>(this.webApi, importFileToProcess, httpOptions); 
    }


    
    /* 6.17.19 Test data:

            C:\Development\VS2017\PIMS3_Revenue\2019JUN_Revenue_Brahms_Test.xlsx
            {
              "amountSaved": 0,
              "exceptionTickers": "",
              "importFilePath": ""C:\Development\VS2017\PIMS3_Revenue\2019JUN_Revenue_Brahms_Test.xlsx",
              "isRevenueData": true,
              "miscMessage": "",
              "recordsSaved": 0
            }
    */

}
