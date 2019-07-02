import { Component, OnInit, Injectable /*, Optional*/ } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';


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
        spanElement.innerHTML = "Welcome:" + "&nbsp&nbsp&nbsp" + investorLogin;
        //this.nameDisplayed = investorLogin;
    }
    

    ngOnInit() {
        this.authenticationSvc.loggedIn.subscribe(isLoggedInValue => this.showLogIn = isLoggedInValue);
        this.authenticationSvc.registered.subscribe(isRegisteredValue => this.showRegistration = isRegisteredValue);
        this.authenticationSvc.loggedOut.subscribe(isLoggedOutValue => {
            //alert("showLogdOut : " + isLoggedOutValue);
            this.showLogOut = isLoggedOutValue
        });
    }


    collapse(isLoginOrRegistrationOption: boolean = false) {

        if (!isLoginOrRegistrationOption && sessionStorage.length == 0) {
            alert("No login credentials found, please login/register for application access.");
            this.router.navigate(['/']);
            return;
        } 
        this.isExpanded = false;
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

}
