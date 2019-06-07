import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Investor } from '../shared/investor';


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

    constructor(private http: HttpClient) {
        //this.currentInvestorSubject = new BehaviorSubject<Investor>(JSON.parse(localStorage.getItem('currentInvestor')));
        this.currentInvestorSubject = new BehaviorSubject<Investor>(JSON.parse(sessionStorage.getItem('currentInvestor')));
        this.currentInvestor$ = this.currentInvestorSubject.asObservable();
    }

    public get currentInvestorValue(): Investor {
        return this.currentInvestorSubject.value;
    }

    login(loginName: string, Password: string): any {

        const baseUrl = "https://localhost:44328";

        return this.http.post<any>(baseUrl + "/api/investor/authenticateInvestor", { loginName, Password })
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

        // Remove user from session (or from 'localStorage' at a later point ?) storage to log user out.
        sessionStorage.removeItem('currentInvestor');
        this.currentInvestorSubject.next(null);
    }

}
