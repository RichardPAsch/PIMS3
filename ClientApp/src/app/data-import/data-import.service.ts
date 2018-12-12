import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataImportVm } from './data-importVm';
import { Observable } from 'rxjs';
import { catchError, map, tap,} from 'rxjs/operators';
import { of } from 'rxjs/observable/of';
import { MessageService } from '../message.service';


let httpHeaders = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Cache-Control', 'no-cache');

const httpOptions = {
    headers: httpHeaders
};


// Creates a provider for the service; providedIn: 'root' specifies that the service should be provided in the root injector.
// When you add a service provider to the root application injector, it’s available throughout the app.
// Should always provide your service in the root injector unless there is a case where you want the service to be available
// only if the consumer imports a particular @NgModule.
@Injectable()

export class DataImportService {

    constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string, private messageService: MessageService) {
        baseUrl = "https://localhost:44328";  // temp - conflicts with fetch-data's definition of baseUrl ?
    }
    

    /* All HttpClient methods return an RxJS Observable of something.
     * Observables' subscribe() is responsible for handling errors.
     * You can use pipes to link operators (RxJS) together. Pipes let you combine multiple functions into a single function.
     * The pipe() function takes as its arguments the functions you want to combine, and returns a new function that,
     * when executed, runs the composed functions in sequence.
       Also see: https://angular.io/tutorial/toh-pt6
    */

    postImportFileData(importFileToProcess: DataImportVm): Observable<DataImportVm> {
        //Send to backend for processing here: WIP -> ImportFileController.cs / ImportFileControllerReference.txt

        // Also see: https://stackblitz.com/angular/nqyqljajkrp?file=src%2Fapp%2Fhero.service.ts
        // POST args: (url, body of data [of any type], options [params, headers, etc. & is optional]); returns -> Observable<any>
        // All HttpClient methods return an RxJS Observable<some type>
        // APIs may bury the data that you want within an object; therefore, process Observable result with the RxJS 'map' operator.
        // To catch errors, you "pipe" the observable result from http.post() through an RxJS catchError() operator.
        // The catchError() operator intercepts an Observable that failed, & passes the error to an error handler.
        // RxJS 'tap' operator (callback) taps/intercepts into the flow of observable values, LOOKING at their value(s) only & passing them along the chain.
        return this.http.post<DataImportVm>(this.baseUrl + 'api/ImportFile/ProcessImportFile', importFileToProcess, httpOptions)
                        .pipe(
                            tap((processedResults: DataImportVm) => this.log("processed count of " + processedResults.recordsSaved + " XLSX recs totaling $" + processedResults.amountSaved)),
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

        if (operation == "'postImportFileData") {
            var debugVar = result;
            
        }

        return (error: any): Observable<T> => {
            if (error.error.exceptionTickers.length > 0) {
                alert("Unable to save income; due to the following invalid submitted Position(s) : \n" + error.error.exceptionTickers);
                this.log("Missing:" + error.error.exceptionTickers + " at: " + error.url);
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

}
