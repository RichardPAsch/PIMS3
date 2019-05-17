import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { first } from 'rxjs/operators';

// TODO:
//import { AlertService, UserService, AuthenticationService } from '../_services';


@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrls: ['./registration.component.css']
})

export class RegistrationComponent implements OnInit {

    loading = false;
    submitted = false;

    constructor() {
    }

    ngOnInit() {
       
    }

    registrationForm = new FormGroup({
        firstName: new FormControl('', [Validators.required]),
        lastName: new FormControl('', [Validators.required]),
        investorName: new FormControl('', [Validators.required]),
        password: new FormControl('', [Validators.required, Validators.minLength(6)]),
    });

    get formFields() { return this.registrationForm.controls; }

    onSubmit() {

        this.submitted = true;

        if (this.registrationForm.invalid) {
            return;
        }

        this.loading = true;
        // TODO:
        //this.userService.register(this.registrationForm.value)
        //    .pipe(first())
        //    .subscribe(
        //        data => {
        //            this.alertService.success('Registration successful', true);
        //            this.router.navigate(['/login']);
        //        },
        //        error => {
        //            this.alertService.error(error);
        //            this.loading = false;
        //        });

    }

   

}
