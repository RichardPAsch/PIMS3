
import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthenticationService } from '../shared/authentication.service';

/*  Note:
    This authorization check guards against unauthorized investors from accessing restricted routes, via CanActivate(). Checks if a route can be activated,
    returning true, allowing access to proceed, if ok, otherwise returning false, blocking route access.
    This uses the authentication service to check if the investor is logged in, and if so, checks if their role is authorized to access the requested route.
    If a investor is logged in and authorized, canActivate() returns true, otherwise it returns false, redirecting to the login page.
*/

@Injectable({ providedIn: 'root' })
export class AuthorizationGuard implements CanActivate {
    constructor(private router: Router, private authSvc: AuthenticationService) {}

    canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot) {

        // True if investor currently logged in.
        let currentInvestor = this.authSvc.currentInvestorValue;

        if (currentInvestor) {
            // Route restricted by role?
            if (route.data.roles && route.data.roles.indexOf(currentInvestor.role) == -1) {
                 // Role unauthorized.
                this.router.navigate(['/']);
                return false;
            }
            return true;
        }

        alert("No login credentials found, please login for access.");
        this.router.navigate(['/']);
        return false;
    }
}
