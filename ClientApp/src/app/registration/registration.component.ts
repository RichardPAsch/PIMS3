import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { first } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';
import { InvestorService } from '../shared/investor.service';
import { Pims3Validations } from '../shared/pims3-validations';
import { HttpErrorResponse } from '@angular/common/http';
import { AlertService } from '../shared/alert.service';

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

    constructor(private router: Router, private authenticationSvc: AuthenticationService, private investorSvc: InvestorService, private alertSvc: AlertService) {
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
        password: new FormControl('', [Validators.required, Validators.minLength(6), Pims3Validations.passwordValidator()]),
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
                this.alertSvc.success('Registration successful for new login : ' + registeredInvestor.loginName);
                // Leverage authentication service for (nav-menu-component - authentication.service) relationship
                // regarding: menu option toggling.
                this.authenticationSvc.register();
                this.router.navigate(['/']);
            },
            (apiError: HttpErrorResponse) => {
                if (apiError.error instanceof Error) {
                    this.alertSvc.error('Error completing registration; possible network issue due to: ' + apiError.error.message);
                } else {
                    this.alertSvc.warn('Unable to complete registration: possible duplicate login name or system error. Retry using an alternative login. ');
                }

                this.loading = false;
            });

    }

   

}
