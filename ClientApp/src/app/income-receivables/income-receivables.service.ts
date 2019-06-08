import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

let httpHeaders = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Cache-Control', 'no-cache');

const httpOptions = {
    headers: httpHeaders
};

const baseUrl = "https://localhost:44328";

@Injectable({
  providedIn: 'root'
})
export class IncomeReceivablesService {

    currentInvestorId: string;

    constructor(private http: HttpClient) {
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor')); 
        this.currentInvestorId = investor.id;
    }

    BuildIncomeReceivables(): Observable<string> {

        let webApiUri = baseUrl + "/api/Income/GetMissingIncomeSchedule/" + this.currentInvestorId;
        return this.http.get<string>(webApiUri);
    }

    UpdateIncomeReceivables(positionIdsToUpdate: any[]): Observable<any> {

        let webApiUri = baseUrl + "/api/Position/UpdatePymtDueFlags/";
        return this.http.put<any>(webApiUri, positionIdsToUpdate);

    }
}
