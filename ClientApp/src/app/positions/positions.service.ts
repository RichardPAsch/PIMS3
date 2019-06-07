import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const baseUrl = "https://localhost:44328";


@Injectable({
  providedIn: 'root'
})
export class PositionsService {

    currentInvestorId: string;

    constructor(private http: HttpClient) {
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor')); // parse string to object
        this.currentInvestorId = investor.id;
    }


    BuildPositions(includeInactiveStatus: boolean): Observable<string> {

        let webApi = baseUrl + "/api/Position/" + includeInactiveStatus + "/" + this.currentInvestorId;
        return this.http.get<string>(webApi);
    }


    UpdateEditedPositions(positionEdits: any[]): Observable<string> {

        let webApi = baseUrl + "/api/Position/UpdateEditedPositions";
        return this.http.put<any>(webApi, positionEdits);
    }
}
