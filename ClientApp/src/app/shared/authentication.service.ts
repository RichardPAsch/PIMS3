import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Investor } from '../shared/investor';
import { GlobalsService } from '../shared/globals.service';



/* ===== NOTES: =========
 * This authentication service handles investor login/logout of PIMS; during login, it posts the users' credentials to the api,
 * and checks the response for a JWT token. If there is one, it means authentication was successful, and the investor details,
 * including the token, are then added to local or session storage for persistence.
 *
 * Local storage   : investor credentials kept between browser sessions & refreshes, until logged out.
 * Session stroage : investor credentials kept between browser refreshes, until logged out.
*/

@Injectable({
  providedIn: 'root'
})

export class AuthenticationService {

    private currentInvestorSubject: BehaviorSubject<Investor>;
    public currentInvestor$: Observable<Investor>;
    private baseUrl;


    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        //this.currentInvestorSubject = new BehaviorSubject<Investor>(JSON.parse(localStorage.getItem('currentInvestor')));
        this.currentInvestorSubject = new BehaviorSubject<Investor>(JSON.parse(sessionStorage.getItem('currentInvestor')));
        this.currentInvestor$ = this.currentInvestorSubject.asObservable();
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    public get currentInvestorValue(): Investor {
        return this.currentInvestorSubject.value;
    }

   
    login(loginName: string, Password: string): any {

        return this.http.post<any>(this.baseUrl + "/api/investor/authenticateInvestor", { loginName, Password })
            .pipe(map(investor => {
                // login successful if there's a jwt in the response.
                if (investor && investor.token) {
                    // ** Store investor user details and jwt in SESSION storage for now, ONLY while in development/testing mode. **
                    //localStorage.setItem('currentInvestor', JSON.stringify(investor));
                    sessionStorage.setItem('currentInvestor', JSON.stringify(investor));
                    this.currentInvestorSubject.next(investor);
                }
                return investor;
            }));
    }


    logout() {

        // Remove user, via session storage key, from session (or from 'localStorage' at a later point ?) storage.
        sessionStorage.removeItem('currentInvestor');
        //alert("Log out successful for: \n" + this.currentInvestorSubject.value.firstName + " " + this.currentInvestorSubject.value.lastName);
        this.currentInvestorSubject.next(null);
    }

}
