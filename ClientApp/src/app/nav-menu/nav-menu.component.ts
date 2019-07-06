import { Component, OnInit, Injectable /*, Optional*/ } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';

// Deferred until dependent Angular components updated!
//import { MatTooltipModule } from '@angular/material/tooltip'; 


@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})

// Enable DI into HomeComponent to accommodate displaying investors' login.
 // TODO: unnecessary due to use of 'BehaviorSubject' ?
@Injectable({
    providedIn: 'root'
})
export class NavMenuComponent implements OnInit {

    constructor(private router: Router, private authenticationSvc: AuthenticationService/*, @Optional() private name: string*/) {
    }


    isExpanded = false;
    //nameDisplayed: string; // deferred until hack eliminated.
    homeComponentImported;
    showLogOut: boolean = true;
    showLogIn: boolean = true;
    showRegistration: boolean = true;


    public set initializeDisplayName(investorLogin: string) {
        // ** A hack. Could NOT get interpolation to work for this simple login name update!
        let spanElement = document.getElementById("loginDisplay");
        spanElement.innerHTML = "Welcome - " + investorLogin;
        //this.nameDisplayed = investorLogin;
    }
    

    ngOnInit() {
        this.authenticationSvc.loggedIn.subscribe(isLoggedInValue => this.showLogIn = isLoggedInValue);
        this.authenticationSvc.registered.subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.authenticationSvc.loggedOut.subscribe(isLoggedOutValue => {
            this.showLogOut = isLoggedOutValue
        });
    }


    logoutInvestor() {
        this.authenticationSvc.logout();
        this.authenticationSvc.loggedOut.subscribe(isLoggedOutValue => this.showLogOut = isLoggedOutValue);
        this.authenticationSvc.registered.subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.router.navigate(['/']);

        let spanElement = document.getElementById("loginDisplay");
        spanElement.innerHTML = "";
        return;
    }

    
    toggle() {
        this.isExpanded = !this.isExpanded;
    }


    aboutThis() {
        // 7.6.19 - Temporary workaround until tooltips are implemented, pending necessary Angular component upgrades (v8.0).
        if (this.showLogIn) {
            alert("                              -- Menu Summary -- " +
                "\n 1. Income summary - YTD summary of income by month." +
                "\n 2. Income projections - Calculate monthly income for up to 5 tickers." +
                "\n 3. Income due - Show outstanding payment(s) for the month." +
                "\n 4. Income recorded - Show payment(s) received for up to last 5 years." +
                "\n 5. Data import - Import new income and/or position(s) into system." +
                "\n 6. Positions - Show both existing and old Position(s)." +
                "\n 7. Asset profile - Show profile or enter/edit custom profile."
            );
        }
    }

}
