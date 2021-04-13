import { Component, OnInit, Injectable /*, Optional*/ } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';
import { HttpClient } from '@angular/common/http';
import { GlobalsService } from '../shared/globals.service';
import { LogNonException } from '../error-logging/log-non-exception';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


// Deferred until dependent Angular components updated! (v.8.0.2)
//import { MatTooltipModule } from '@angular/material/tooltip'; 


@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})

export class NavMenuComponent extends BaseUnsubscribeComponent implements OnInit {

    private baseUrl;


    constructor(private router: Router, private authenticationSvc: AuthenticationService, private http: HttpClient, globalsSvc: GlobalsService/*, @Optional() private name: string*/) {
        super();
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    isExpanded = false;
    nameDisplayed: string;
    homeComponentImported;
    showLogOut: boolean = true;
    showLogIn: boolean = true;
    showRegistration: boolean = true;
    showPasswordReset: boolean = false;
    isCollapsed: boolean = true;  // for navBar menu collapsing; pending fix


    ngOnInit() {
        // Subscribing to BehaviorSubject automatically updates here when value(s) change.
        this.authenticationSvc.loggedIn.pipe(takeUntil(this.getUnsubscribe())).subscribe(isLoggedInValue => this.showLogIn = isLoggedInValue);
        this.authenticationSvc.registered.pipe(takeUntil(this.getUnsubscribe())).subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.authenticationSvc.loggedOut.pipe(takeUntil(this.getUnsubscribe())).subscribe(isLoggedOutValue => {
            this.showLogOut = isLoggedOutValue
        });

        // Avoid display if login not yet completed.
        this.authenticationSvc.investorLoginName
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(login => {
            if (login != "") {
                this.nameDisplayed = "Welcome,  " + login + ".";
                this.showPasswordReset = true;
            }
        }); 
    }


    logoutInvestor() {
        this.authenticationSvc.logout();
        this.authenticationSvc.loggedOut.pipe(takeUntil(this.getUnsubscribe())).subscribe(isLoggedOutValue => this.showLogOut = isLoggedOutValue);
        this.authenticationSvc.registered.pipe(takeUntil(this.getUnsubscribe())).subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.router.navigate(['/']);

        this.showPasswordReset = false;
        this.logLogOut("Logout successful for: " + this.authenticationSvc.investorLoginEMailName.value)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(result => {
                console.log(result)
            })
            .unsubscribe;

        this.nameDisplayed = "";
        return;
    }

    logLogOut(message: string) {
        let informationalLogModel = new LogNonException();
        informationalLogModel.message1 = message;
        return this.http.post<string>(this.baseUrl + '/api/Logging/LogNonError', informationalLogModel);
    }

    
    toggle() {
        this.isExpanded = !this.isExpanded;
    }

    toggleCollapsed() {
        this.isCollapsed = !this.isCollapsed;
    }


    aboutThis() {
        // 7.6.19 - Temporary workaround until tooltips are implemented, pending necessary Angular component upgrades (v8.0.2).
        if (this.showLogIn) {
            alert("                              -- Menu Summary -- " +
                "\n 1. Income summary - YTD summary of income by month." +
                "\n 2. Income projections - Calculate monthly income for up to 5 tickers." +
                "\n 3. Income due - Show outstanding payment(s) for the month." +
                "\n 4. Income recorded - Show payment(s) received for up to last 5 years." +
                "\n 5. Data import - Import new income and/or position(s) into system." +
                "\n 6. Positions - Show both existing and old Position(s)." +
                "\n 7. Asset profile - Show profile, or enter/edit custom profile for a ticker." +
                "\n 8. Distributions - Show asset dividend distributions information profile."
            );
        }
    }

}
