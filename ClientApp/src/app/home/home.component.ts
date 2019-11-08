import { Component, OnInit} from '@angular/core';
import { Router } from '@angular/router';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { first } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';

@Component({
    selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
    
export class HomeComponent extends BaseUnsubscribeComponent implements OnInit {

    constructor(private router: Router, private authenticationSvc: AuthenticationService, private alertSvc: AlertService) {
        super();
        if (this.authenticationSvc.currentInvestorValue) {
            alertSvc.warn("Login already exist for: " +  this.authenticationSvc.currentInvestorValue.firstName 
                + " "
                + this.authenticationSvc.currentInvestorValue.lastName) + ".";
            this.router.navigate(['/income-summary']);
        }
    }

 
    // Original investorId for rpasch@rpclassics.net: CF256A53-6DCD-431D-BC0B-A810010F5B88
    loading = false;
    submitted = false;
    returnUrl: string;
    loginForm = new FormGroup({
        investorName: new FormControl('rpasch@rpclassics.net', [Validators.required]), 
        password: new FormControl('rich25102', [Validators.required, Validators.minLength(6)]),
    });
    //loginForm = new FormGroup({
    //    investorName: new FormControl('jbrahms@gmail.com', [Validators.required]),
    //    password: new FormControl('classical1', [Validators.required, Validators.minLength(6)]),
    //});
    investorName: string;

    get formFields() { return this.loginForm.controls; }

    get investorFName() { return this.investorFName; }


    ngOnInit() { }


    onSubmit() {

        if (this.loginForm.invalid) {
            this.alertSvc.warn("Invalid login. Login name and/or password required.");
            this.router.navigate(['/']);
            return;
        }

        this.submitted = true;
        this.loading = true;

        this.authenticationSvc.login(this.formFields.investorName.value, this.formFields.password.value)
            .pipe(first(), takeUntil(this.getUnsubscribe()))
            .subscribe( () =>
            {
                this.router.navigate(['/income-summary']);
            },
            () => {
                this.alertSvc.error("Error verifying login credentials for: " + "'" + this.formFields.investorName.value + "'" +
                                    ". Please check entry(ies), or try again later.");
                this.loading = false;
            });

    }
    
}
