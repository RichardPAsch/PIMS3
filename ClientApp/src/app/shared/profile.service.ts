import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Profile } from '../profile/profile';
import { ProjectionProfile } from '../income-projections/projection-profile';
import { GlobalsService } from '../shared/globals.service';


@Injectable({
  providedIn: 'root'
})

export class ProfileService {

    // 'HttpBackend' injection may be a future option, as it bypasses use of 'http.error.interceptor'; however,
    // it also interferes with normal valid backend operations, eg. 'CSQ'. Investigate?

    baseUrl: string;
    currentInvestorId: string;
 
    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor'));
        this.currentInvestorId = investor.id;
    }


    getProfileData(ticker: string): Observable<Profile> {

        let webApiUri = this.baseUrl + "/api/Profile/" + ticker;

        // Returns Profile as an Observable, to be subscribed to.
        return this.http.get<Profile>(webApiUri);
    }


    getProfileDataViaDb(ticker: string, loginName: string): Observable<Profile> {

        let webApiUri = this.baseUrl + "/api/Profile/" + ticker + "/" + true + "/" + loginName;
        return this.http.get<any>(webApiUri);
    }


    getProfileDividendInfo(searchTicker: string): Observable<any> {

        let webApiUri = this.baseUrl + "/api/DivInfo/" + searchTicker;
        return this.http.get<ProjectionProfile>(webApiUri);
    }


    updateProfile(partialProfileUpdate: Profile): Observable<boolean> {

        let webApiUri = this.baseUrl + "/api/Profile";
        return this.http.put<boolean>(webApiUri, partialProfileUpdate);
    }


    updatePortfolioProfiles(loginName: string): Observable<any> {

        let webApiUri = this.baseUrl + "/api/Profile/" + loginName;
        return this.http.put<boolean>(webApiUri, null); 
    }


    saveNewProfile(newProfile: Profile): Observable<boolean> {

        let webApiUri = this.baseUrl + "/api/Profile";
        return this.http.post<boolean>(webApiUri, newProfile);
    }


    fetchDistributionSchedules(): Observable<any> {

        let webApiUrl = this.baseUrl + "/api/GetDistributionSchedules/" + this.currentInvestorId;
        return this.http.get<any>(webApiUrl);
    }
}
