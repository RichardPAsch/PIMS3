import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Profile } from '../profile/profile';
import { ProjectionProfile } from '../income-projections/projection-profile';


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

    getProfileDataViaDb(ticker: string): Observable<boolean> {

        let webApiUri = baseUrl + "/api/Profile/" + ticker + "/" + true;

        return this.http.get<any>(webApiUri);
    }

    getProfileDividendInfo(searchTicker: string): Observable<any> {

        let webApiUri = baseUrl + "/api/DivInfo/" + searchTicker;
        return this.http.get<ProjectionProfile>(webApiUri);
    }

    updateProfile(partialProfileUpdate: Profile): Observable<boolean> {

        let webApiUri = baseUrl + "/api/Profile";
        return this.http.put<boolean>(webApiUri, partialProfileUpdate);
    }
}
