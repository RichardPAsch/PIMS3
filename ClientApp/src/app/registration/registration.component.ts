import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { first } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';
import { InvestorService } from '../shared/investor.service';
import { Pims3Validations } from '../shared/pims3-validations';
import { HttpErrorResponse } from '@angular/common/http';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';

/*  Creates a new investor via the investor service when registration is submitted. If the investor is already logged in,
    they are automatically redirected to the 'income summary' page.
 */


@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrls: ['./registration.component.css']
})

export class RegistrationComponent extends BaseUnsubscribeComponent implements OnInit {

    loading = false;
    submitted = false;

    constructor(private router: Router, private authenticationSvc: AuthenticationService, private investorSvc: InvestorService, private alertSvc: AlertService) {
        // Redirect if already logged in.
        super();
        if (this.authenticationSvc.currentInvestorValue) {
            this.router.navigate(['/income-summary']);
        }
    }

    ngOnInit() { }

    registrationForm = new FormGroup({
        firstName: new FormControl('', [Validators.required]),
        lastName: new FormControl('', [Validators.required]),
        loginName: new FormControl('', [Validators.required, Validators.pattern("^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,4}$")]), // email pattern
        password: new FormControl('', [Validators.required, Validators.minLength(6), Pims3Validations.passwordValidator()]),
    });

    get formFields() { return this.registrationForm.controls; }

    onSubmit() {

        this.submitted = true;

        if (this.registrationForm.invalid) {
            this.alertSvc.warn('Unable to process registration; please correct noted entry(ies).');
            return;
        }

        this.loading = true;

        // 'this.registrationForm.value' - For a formGroup, the values of all enabled form controls are encapsulated
        //  into an object and submitted via key:value pairs.
        this.investorSvc.register(this.registrationForm.value)
            .pipe(first(), takeUntil(this.getUnsubscribe()))
            .subscribe(registeredInvestor => {
                this.alertSvc.success('Registration successful for new login : ' + registeredInvestor.loginName);
                // Leverage authentication service for (nav-menu-component - authentication.service) relationship
                // regarding: menu option toggling.
                this.authenticationSvc.register();
                this.router.navigate(['/']);
            },
            (apiError: string) => {
                if (apiError === "Duplicate registration found.")
                    this.alertSvc.warn('Unable to complete registration; duplicate registration ("username") found. Please recheck entry.');
                 else {
                    this.alertSvc.error('Error completing registration; possible network issue. Please retry later.');
                 }

                this.loading = false;
            });
    }

   

}
