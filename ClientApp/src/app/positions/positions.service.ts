import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

const baseUrl = "https://localhost:44328";

@Injectable({
  providedIn: 'root'
})
export class PositionsService {

    constructor(private http: HttpClient) { }


    BuildPositions(): Observable<string> {

        let webApi = baseUrl + "/api/Position/GetPositions";
        return this.http.get<string>(webApi);
    }


    UpdateEditedPositions(positionEdits: any[]): Observable<string> {

        let webApi = baseUrl + "/api/Position/UpdateEditedPositions";
        return this.http.put<any>(webApi, positionEdits);
    }
}
