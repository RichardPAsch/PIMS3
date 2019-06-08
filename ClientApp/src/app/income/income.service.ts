import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const baseUrl = "https://localhost:44328";

@Injectable({ providedIn: 'root' })

export class IncomeService {

    currentInvestorId: string;

    constructor(private http: HttpClient) {
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor'));
        this.currentInvestorId = investor.id;
    }

    GetRevenue(yearsOfHx: number): Observable<string>
    {
        let webApi = baseUrl + "/api/Income/" + yearsOfHx + "/" + this.currentInvestorId;
        return this.http.get<any>(webApi);
    }

    UpdateIncome(editedRevenue: any[]): Observable<any>
    {
        let webApi = baseUrl + "/api/Income/";
        return this.http.put<any>(webApi, editedRevenue);
    }
}
