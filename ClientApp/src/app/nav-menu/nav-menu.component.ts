import { Component, OnInit, Injectable /*, Optional*/ } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';

// Deferred until dependent Angular components updated! (v.8.0.2)
//import { MatTooltipModule } from '@angular/material/tooltip'; 


@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})

export class NavMenuComponent implements OnInit {

    constructor(private router: Router, private authenticationSvc: AuthenticationService/*, @Optional() private name: string*/) {
    }

    isExpanded = false;
    nameDisplayed: string;
    homeComponentImported;
    showLogOut: boolean = true;
    showLogIn: boolean = true;
    showRegistration: boolean = true;
    showPasswordReset: boolean = false;


    ngOnInit() {
        // Subscribing to BehaviorSubject automatically updates here when value(s) change.
        this.authenticationSvc.loggedIn.subscribe(isLoggedInValue => this.showLogIn = isLoggedInValue);
        this.authenticationSvc.registered.subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.authenticationSvc.loggedOut.subscribe(isLoggedOutValue => {
            this.showLogOut = isLoggedOutValue
        });

        // Avoid display if login not yet completed.
        this.authenticationSvc.investorLoginName.subscribe(login => {
            if (login != "") {
                this.nameDisplayed = "Welcome - " + login;
                this.showPasswordReset = true;
            }
        }); 
    }


    logoutInvestor() {
        this.authenticationSvc.logout();
        this.authenticationSvc.loggedOut.subscribe(isLoggedOutValue => this.showLogOut = isLoggedOutValue);
        this.authenticationSvc.registered.subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.router.navigate(['/']);

        this.nameDisplayed = "";
        this.showPasswordReset = false;
        return;
    }

    
    toggle() {
        this.isExpanded = !this.isExpanded;
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
                "\n 7. Asset profile - Show profile, or enter/edit custom profile for a ticker."
            );
        }
    }

}
