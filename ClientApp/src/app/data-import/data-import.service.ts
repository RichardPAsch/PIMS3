import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataImportVm } from './data-importVm';
import { Observable } from 'rxjs';
import { catchError, map, tap,} from 'rxjs/operators';
import { of } from 'rxjs/observable/of';
import { MessageService } from '../message.service';
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

    constructor(private http: HttpClient, globalsSvc: GlobalsService, private messageService: MessageService) {
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
            .pipe(
                tap((processedResults: DataImportVm) => this.log("processed count of "
                                                                    + processedResults.recordsSaved
                                                                    + " XLSX recs totaling $"
                                                                    + processedResults.amountSaved
                                                                    + " for tickers: "
                                                                    + processedResults.miscMessage)),
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

            if (error.error.exceptionTickers.length > 0 && error.error.isRevenueData == true) {
                alert("Unable to save income, due to the following invalid and/or duplicate submitted Position(s) : \n" + error.error.exceptionTickers);
                this.log("Missing:" + error.error.exceptionTickers + " at: " + error.url);
            }

            // POSTing error for Position; unable to fetch Profile via web - bad ticker/unavailable data ?
            if (error.error.isRevenueData == false) {
                alert("Unable to fetch Profile data for: \n" + error.error.exceptionTickers + " \nCheck ticker validity, or enter Profile manually.");
                this.log("Error saving Position data for: " + error.error.exceptionTickers + " at: " + error.url);
            }

            // TODO: send the error to remote logging infrastructure
            //console.error(error); // log to console instead

            // TODO: better job of transforming error for user consumption
            //this.log(`${operation} failed: ${error.message}`);

            // Let the app keep running by returning an empty result.
            return of(result as T);
        };
    }


    private log(message: string) {
        this.messageService.add(`DataImportService: ${message}`);
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
