import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { first } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';
// TODO: import { AlertService } from '@/_services';
import { NavMenuComponent } from '../nav-menu/nav-menu.component';

@Component({
  selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
    
export class HomeComponent implements OnInit {

    constructor(private router: Router, private authenticationSvc : AuthenticationService, private navMenuComp: NavMenuComponent) {
        // TODO: Add as params:   private alertSvc: AlertService

        if (this.authenticationSvc.currentInvestorValue) {
            alert("Login already exist for: \n" + this.authenticationSvc.currentInvestorValue.firstName
                                                + " "
                                                + this.authenticationSvc.currentInvestorValue.lastName);
            this.router.navigate(['/income-summary']);
        }
    }
 
    // Original investorId for rpasch@rpclassics.net: CF256A53-6DCD-431D-BC0B-A810010F5B88
    loading = false;
    submitted = false;
    returnUrl: string;
    loginForm = new FormGroup({
        investorName: new FormControl('rpasch@rpclassics.net', [Validators.required]),  // ** TEMPORARY ONLY! **
        password: new FormControl('', [Validators.required, Validators.minLength(6)]),
    });
    investorName: string;

    get formFields() { return this.loginForm.controls; }

    get investorFName() { return this.investorFName; }


    ngOnInit() { }


    onSubmit() {

        if (this.loginForm.invalid) {
            alert("Invalid login. \nLogin name and/or password required.");
            this.router.navigate(['/']);
            return;
        }

        this.submitted = true;
        this.loading = true;

        this.authenticationSvc.login(this.formFields.investorName.value, this.formFields.password.value)
            .pipe(first())
            .subscribe(investorModel =>
            {
                this.navMenuComp.initializeDisplayName = investorModel.firstName; 
                this.investorName = investorModel.firstName;
                this.router.navigate(['/income-summary']);
            },
            error => {
                alert("Error with login, due to: " + error(error));
                //this.alertService.error(error);
                this.loading = false;
            });
    }

}
