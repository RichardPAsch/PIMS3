import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';


@Injectable({
  providedIn: 'root'
})
export class PositionsService {

    currentInvestorId: string;
    baseUrl: string; 

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor')); // parse string to object
        this.currentInvestorId = investor.id;
    }


    BuildPositions(includeInactiveStatus: boolean): Observable<string> {

        let webApi = this.baseUrl + "/api/Position/" + includeInactiveStatus + "/" + this.currentInvestorId;
        return this.http.get<string>(webApi);
    }


    UpdateEditedPositions(positionEdits: any[]): Observable<string> {

        let webApi = this.baseUrl + "/api/Position/UpdateEditedPositions";
        return this.http.put<any>(webApi, positionEdits);
    }


    GetAssetClassDescAndCode(): Observable<string> {
        let webApi = this.baseUrl + "/api/AssetClasses";
        return this.http.get<string>(webApi);
    }


    GetAccountTypes(): Observable<string[]> {
        let webApi = this.baseUrl + "/api/AccountTypes";
        return this.http.get<string[]>(webApi);
    }
}
