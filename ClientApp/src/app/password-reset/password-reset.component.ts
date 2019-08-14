import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { Pims3Validations } from '../shared/pims3-validations';
import { AuthenticationService } from '../shared/authentication.service';
import { InvestorService } from '../shared/investor.service';
import { HttpErrorResponse } from '@angular/common/http';
import { PasswordResetService } from '../password-reset/password-reset.service';


@Component({
  selector: 'app-password-reset',
  templateUrl: './password-reset.component.html',
  styleUrls: ['./password-reset.component.css']
})
export class PasswordResetComponent implements OnInit {
   loading = false;
   submitted = false;

    constructor(private investorSvc: InvestorService, private authSvc: AuthenticationService, private pwrdResetSvc: PasswordResetService) {
    }

  ngOnInit() { }

  /* Notes:
     - using Reactive, rather than template-driven forms, thus we create FormControl objects explicitly
     - and bind to HTML via 'formControlName' attr.
     - Validators are defined while creating objects of the FormControl, the first parameter is the
     - initial state of the control to be set, i.e '', while the second parameter is ValidatorFn.
   */
  passwordResetForm = new FormGroup({
      oldPassword: new FormControl('', [Validators.required]),
      newPassword: new FormControl('', [Validators.required, Validators.minLength(6), Pims3Validations.passwordConfirmationValidator()]),
      newPasswordConfirm: new FormControl('', [Validators.required]),
  });

   get formFields() { return this.passwordResetForm.controls; }


  onSubmit() {

      this.submitted = true;
      let fieldValues = this.passwordResetForm.value;

      if (this.passwordResetForm.invalid) {
          return;
      }
      else {
          if (fieldValues.newPassword == fieldValues.newPasswordConfirm && fieldValues.oldPassword != fieldValues.newPassword ) {
              
              let loggedInvestor = JSON.parse(sessionStorage.getItem('currentInvestor'));

              // OldPassword verified, via 'InvestorController.cs', before updated to new value.
              this.investorSvc.updateLogin(loggedInvestor.username, fieldValues.oldPassword, fieldValues.newPassword)
                  .retry(2)
                  .subscribe(updatedInvestor => {
                      if (updatedInvestor) {
                          alert("Password successfully updated for\n" + updatedInvestor.firstName + " " + updatedInvestor.lastName);
                      } 
                   },
                  (apiErr: HttpErrorResponse) => {
                      alert("Unable to update password due to system error.\n Please retry at a later time.");
                  });
          }
          else {
              alert("Invalid entry(ies) found for password reset. \nVerify old vs. new, and/or new vs. confirmed password.");
              return;
          }
      }

      this.loading = true;
  }

}
