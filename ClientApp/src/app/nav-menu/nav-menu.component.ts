import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';


@Component({
  selector: 'app-nav-menu',
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.css']
})
export class NavMenuComponent {
    constructor(private router: Router, private authenticationSvc: AuthenticationService) {
    }

    isExpanded = false;


  collapse(isLoginOrRegistrationOption : boolean = false) {
      if (!isLoginOrRegistrationOption && sessionStorage.length == 0) {
          alert("No login credentials found, please login/register for application access.");
          this.router.navigate(['/']);
          return;
      } 
    this.isExpanded = false;
    }


    logoutInvestor() {
        this.authenticationSvc.logout();
        this.router.navigate(['/']);
        return;
    }


  toggle() {
    this.isExpanded = !this.isExpanded;
  }
}
