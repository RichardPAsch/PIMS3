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
        this.currentInvestorSubject = new BehaviorSubject<Investor>(JSON.parse(localStorage.getItem('currentInvestor')));
        this.currentInvestor$ = this.currentInvestorSubject.asObservable();
    }

    public get currentInvestorValue(): Investor {
        return this.currentInvestorSubject.value;
    }

    login(submittedName: string, submittedPwrd: string ): any {

        // TODO: replace `users/authenticate` with actual API url.
        return this.http.post<any>(`users/authenticate`, { submittedName, submittedPwrd })
            .pipe(map(investor => {
                // login successful if there's a jwt in the response.
                if (investor && investor.token) {
                    // store investor user details and jwt in local storage.
                    localStorage.setItem('currentInvestor', JSON.stringify(investor));
                    this.currentInvestorSubject.next(investor);
                }
                return investor;
            }));
    }


    logout() {
        // remove user from local storage to log user out
        localStorage.removeItem('currentInvestor');
        this.currentInvestorSubject.next(null);
    }

}
