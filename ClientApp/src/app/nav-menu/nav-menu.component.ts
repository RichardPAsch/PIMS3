import { Component, OnInit, Injectable /*, Optional*/ } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';
import { HttpClient } from '@angular/common/http';
import { GlobalsService } from '../shared/globals.service';
import { LogNonException } from '../error-logging/log-non-exception';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


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

    // Menu tooltips.
    ttIncomeSummary = "YTD summary of income by month";
    ttIncomeProjections = "Calculate monthly income - up to 5 tickers";
    ttIncomeDue = "Show outstanding payment(s) for the month";
    ttIncomeRecorded = "Show payment(s) received for last 5 years";
    ttDistSchedules = "Show payment schedule(s) for position(s)";
    ttDataImport = "Import new income and/or position(s) into system";
    ttPositions = "Show active and/or inactive Position(s)";
    ttAssetProfile = "Show/enter, custom or standard profile for ticker";
    ttStart = "Detailed info re: application usage";

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
    


}
