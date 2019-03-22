import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Profile } from '../income-projections/profile';


let httpHeaders = new HttpHeaders()
    .set('Content-Type', 'application/json')
    .set('Cache-Control', 'no-cache');

const baseUrl = "https://localhost:44328";

@Injectable({
  providedIn: 'root'
})

export class ProfileService {

    constructor(private http: HttpClient) {
    }

    getProfileData(ticker: string): Observable<Profile> {

        let webApiUri = baseUrl + "/api/Profile/" + ticker;

        // Returns Profile as an Observable, to be subscribed to.
        return this.http.get<Profile>(webApiUri);
    }

    getProfileDividendInfo(searchTicker: string): Observable<any> {

        let webApiUri = baseUrl + "/api/DivInfo/" + searchTicker;
        return this.http.get<Profile>(webApiUri);
    }
}
