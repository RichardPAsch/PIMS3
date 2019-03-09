import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const baseUrl = "https://localhost:44328";

@Injectable({ providedIn: 'root' })

export class IncomeService {
    
    constructor(private http: HttpClient) {
    }

    GetRevenue(yearsOfHx: number): Observable<string>
    {
        let webApi = baseUrl + "/api/Income/" + yearsOfHx;
        return this.http.get<any>(webApi);
    }

    UpdateIncome(editedRevenue: any[]): Observable<any>
    {
        let webApi = baseUrl + "/api/Income/";
        return this.http.put<any>(webApi, editedRevenue);
    }
}
