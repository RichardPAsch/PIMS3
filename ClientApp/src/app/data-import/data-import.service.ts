import { Injectable, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { DataImportVm } from './data-importVm';
import { Observable } from 'rxjs';

const httpOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
};



@Injectable()

export class DataImportService {
    private _http: HttpClient;
    private url: string;

    constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this._http = http;
        this.url = baseUrl;
    }

    /* All HttpClient methods return an RxJS Observable of something.
       Also see: https://angular.io/tutorial/toh-pt6
    */

    postImportFileData(importFileToProcess: DataImportVm): Observable<DataImportVm> {
        //Send to backend for processing here: WIP -> ImportFileController.cs / ImportFileControllerReference.txt

        // Also see: https://stackblitz.com/angular/nqyqljajkrp?file=src%2Fapp%2Fhero.service.ts
        return this._http.post<DataImportVm>(this.url + 'api/Income/GetRevenueSummary', DataImportVm, httpOptions)
            .pipe(); // WIP TODO

        //return http.post<DataImportVm>(baseUrl + 'api/Income/GetRevenueSummary', DataImportVm, httpOptions)
        //           .pipe(tap((hero: Hero) => this.log(`added hero w/ id=${hero.id}`)),
        //    catchError(this.handleError<Hero>('addHero'))
        //);
 

    }

}
