import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';


@Injectable({ providedIn: 'root' })

export class IncomeService {

    currentInvestorId: string;
    baseUrl: string;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor'));
        this.currentInvestorId = investor.id;
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    GetRevenue(yearsOfHx: number): Observable<string>
    {
        let webApi = this.baseUrl + "/api/Income/" + yearsOfHx + "/" + this.currentInvestorId;
        return this.http.get<any>(webApi);
    }

    UpdateIncome(editedRevenue: any[]): Observable<any>
    {
        let webApi = this.baseUrl + "/api/Income/";
        return this.http.put<any>(webApi, editedRevenue);
    }

    DeleteIncome(incomeIds: any[]): Observable<any>
    {
        let webApi = this.baseUrl + "/api/Income";
        let idParams = new HttpParams();
        incomeIds.forEach(id => {
            idParams = idParams.append('incomeId', id)
        });

        //let keys = idParams.keys();
        return this.http.delete(webApi, { params: idParams });
    }
}
