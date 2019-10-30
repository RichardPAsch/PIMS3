import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataImportVm } from './data-importVm';
import { Observable } from 'rxjs';
import { catchError, map, tap,} from 'rxjs/operators';
import { of } from 'rxjs/observable/of';
import { GlobalsService } from '../shared/globals.service';
import { AlertService } from '../shared/alert.service';


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

    constructor(private http: HttpClient, globalsSvc: GlobalsService, private alertSvc: AlertService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor'));
        this.Id = investor.id;
    }
    
    /* All HttpClient methods return an RxJS Observable of something.
     * Observables' subscribe() is responsible for handling errors.
     * You can use pipes to link operators (RxJS) together. Pipes let you combine multiple functions into a single function.
     * The pipe() function takes as its arguments the functions you want to combine, and returns a new function that,
     * when executed, runs the composed functions in sequence.
       Also see: https://angular.io/tutorial/toh-pt6
    */

    postImportFileData(importFileToProcess: DataImportVm): any {

        // Also see: https://stackblitz.com/angular/nqyqljajkrp?file=src%2Fapp%2Fhero.service.ts
        // POST args: (url, body of data [of any type], options [params, headers, etc. & is optional]); returns -> Observable<any>
        // All HttpClient methods return an RxJS Observable<some type>
        // APIs may bury the data that you want within an object; therefore, process Observable result with the RxJS 'map' operator.
        // To catch errors, you "pipe" the observable result from http.post() through an RxJS catchError() operator.
        // The catchError() operator intercepts an Observable that failed, & passes the error to an error handler.
        // RxJS 'tap' operator (callback) taps/intercepts into the flow of observable values, LOOKING at their value(s) only & passing them along the chain.
        // Returned 'DataImportVm' - will contain results of processing.

        let fullUrl = this.baseUrl + '/api/ImportFile/' + this.Id; 
        return this.http.post<DataImportVm>(fullUrl, importFileToProcess, httpOptions)
            .pipe(tap(() => console.log("Import data ok.")),
            catchError(this.handleError<DataImportVm>('postImportFileData'))
        );
    }


    /**
         * Handle Http operation that failed, & let the app continue.
         * @param operation - name of the operation that failed
         * @param result - optional value to return as the observable result
         * Because each service method can return a different kind of Observable result, handleError() takes a type parameter,
         * so it can return the safe value as the type that the app expects.
     */
    private handleError<T>(operation = 'operation', result?: T) {

        if (operation == "postImportFileData") {
            var debugVar = result;
        }

        return (error: any): Observable<T> => {
            // 10.30.19 - 'MessageService' deprecated. Errors SHOULD now bubble up to either:
            //             1) http.error.interceptor, or
            //             2) error.service, depending upon source of error.
            if (error.error.exceptionTickers.length > 0 && error.error.isRevenueData == true) {
                this.alertSvc.warn("Unable to save income, due to the following invalid and/or duplicate submitted Position(s) : " + "'" + error.error.exceptionTickers + "'");
            }

            // POSTing error for Position; unable to fetch Profile via web - bad ticker/unavailable data ?
            if (error.error.isRevenueData == false) {
                this.alertSvc.warn("Unable to fetch Profile data via web for: " + "'" + error.error.exceptionTickers + "'."
                    + " Check ticker validity, or enter Profile manually.");
            }

            // Let the app keep running by returning an empty result.
            return of(result as T);
        };
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
