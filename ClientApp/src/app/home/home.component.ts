import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { first } from 'rxjs/operators';

// TODO:
//import { AlertService, AuthenticationService } from '@/_services';

@Component({
  selector: 'app-home',
    templateUrl: './home.component.html',
    styleUrls: ['./home.component.css']
})
    
export class HomeComponent implements OnInit {

    constructor(private router: Router) {
        // TODO: Add as params:
        //  private authenticationSvc AuthenticationService,
        //  private alertSvc: AlertService

        // TODO: redirect to home if already logged in; for PIMS3 -> to this same page?
        //if (this.authenticationService.currentUserValue) {
        //    this.router.navigate(['/']);
        //}
    }

    loading = false;
    submitted = false;
    returnUrl: string;
    loginForm = new FormGroup({
        investorName: new FormControl('', [Validators.required]),
        password: new FormControl('', [Validators.required, Validators.minLength(6)]),
    });

    get formFields() { return this.loginForm.controls; }

    ngOnInit() {
        
    }


    onSubmit() {

        this.submitted = true;
        this.loading = true;

        if (this.loginForm.invalid)
            return;


        // TODO:
        //this.authenticationSvc.login(this.formFields.username.value, this.formFields.password.value)
        //    .pipe(first())
        //    .subscribe(
        //        data => {
        //            this.router.navigate([this.returnUrl]);
        //        },
        //        error => {
        //            this.alertService.error(error);
        //            this.loading = false;
        //        });

    }

}
