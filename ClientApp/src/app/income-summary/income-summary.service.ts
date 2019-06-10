import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';


@Injectable({
  providedIn: 'root'
})

export class IncomeSummaryService {

    baseUrl: string;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    BuildIncomeSummary(yearsBackDated: number): Observable<string> {

        let webApi = this.baseUrl + "/api/Income/" + yearsBackDated + "/" + true;
        return this.http.get<string>(webApi);
    }
}
