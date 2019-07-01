import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { first } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';
import { InvestorService } from '../shared/investor.service';

// TODO:
//import { AlertService } from '../_services';


/*  Creates a new investor via the investor service when the registration is submitted. If the investor is already logged in,
    they are automatically redirected to their 'income summary' page.
 */


@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrls: ['./registration.component.css']
})

export class RegistrationComponent implements OnInit {

    loading = false;
    submitted = false;

    constructor(private router: Router, private authenticationSvc: AuthenticationService, private investorSvc: InvestorService) {
        // TODO: params
        //private alertService: AlertService

        // Redirect if already logged in.
        if (this.authenticationSvc.currentInvestorValue) {
            this.router.navigate(['/income-summary']);
        }
    }

    ngOnInit() { }

    registrationForm = new FormGroup({
        firstName: new FormControl('', [Validators.required]),
        lastName: new FormControl('', [Validators.required]),
        loginName: new FormControl('', [Validators.required]),
        password: new FormControl('', [Validators.required, Validators.minLength(6)]),
    });

    get formFields() { return this.registrationForm.controls; }

    onSubmit() {

        this.submitted = true;

        if (this.registrationForm.invalid) {
            return;
        }

        this.loading = true;

        // 'this.registrationForm.value' - For a formGroup, the values of all enabled form controls are encapsulated
        //  into an object and submitted via key:value pairs.
        this.investorSvc.register(this.registrationForm.value)
            .pipe(first())
            .subscribe(registeredInvestor => {
                alert('Registration successful for login : \n' + registeredInvestor.loginName);
                // Leverage authentication service for nav-menu-component to authentication.service relationship
                // re: menu option toggling.
                this.authenticationSvc.register();
                //this.alertService.success('Registration successful', true);  // TODO.
                this.router.navigate(['/']);
            },
            error => {  // TODO: error = undefined here.
                alert('Unable to complete registration. \nPossible duplicate login name or system error, retry using an alternative login.');
                //this.alertService.error(error);  // TODO.
                this.loading = false;
            });

    }

   

}
