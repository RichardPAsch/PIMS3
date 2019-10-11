import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';


@Injectable({
  providedIn: 'root'
})

export class IncomeSummaryService {

    baseUrl: string;
    currentInvestorId: string;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor')); // parse string to object
        this.currentInvestorId = investor.id;
    }

    BuildIncomeSummary(yearsBackDated: number): Observable<string> {
        //throw new Error("testing log entry");
        let webApi = this.baseUrl + "/api/Income/" + yearsBackDated + "/" + true + "/" + this.currentInvestorId;
        return this.http.get<string>(webApi);
    }

}
