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

    /* Notes on 'BehaviorSubject', per RxJS docs:
         A variant of 'Subjects', and has a notion of "the current value". It stores the *latest* value emitted to its consumers,
         and whenever a new Observer SUBSCRIBES, it will IMMEDIATELY receive the "current value" from the BehaviorSubject.

        BehaviorSubjects represent "values over time". For instance:
        An event stream of birthdays is a Subject, but the stream of a person's age would be a BehaviorSubject.
    */
    private currentInvestorSubject: BehaviorSubject<Investor>;
    public currentInvestor$: Observable<Investor>;
    private baseUrl;

    // Initialize 'nav-menu' boolean flag emitters, i.e., showLogIn, showLogOut, showRegistration.
    public loggedIn: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(true);
    public registered: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(true);
    public loggedOut: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);


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
                // Login successful if there's a jwt in the response.
                if (investor && investor.token) {
                    // ** Store investor user details and jwt in SESSION storage for now, ONLY while in development/testing mode. **
                    //localStorage.setItem('currentInvestor', JSON.stringify(investor));
                    sessionStorage.setItem('currentInvestor', JSON.stringify(investor));

                    // Observable executions, sending 'Next' type of value notifications. Other notification types are: (Error | Complete).
                    this.loggedIn.next(false);
                    this.registered.next(false);
                    this.loggedOut.next(true);
                    this.currentInvestorSubject.next(investor);
                }
                return investor;
            }));
    }


    logout() {

        // Remove user, via session storage key, from session (or from 'localStorage' at a later point ?) storage.
        sessionStorage.removeItem('currentInvestor');
        this.currentInvestorSubject.next(null);
        this.loggedIn.next(true);
        this.loggedOut.next(false);
        this.registered.next(true);
    }


    register() {
        this.registered.next(false);
        this.loggedIn.next(true);
        this.loggedOut.next(false);
    }

}
