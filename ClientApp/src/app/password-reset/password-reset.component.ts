import { Component, OnInit } from '@angular/core';
import { FormGroup, Validators, FormControl } from '@angular/forms';
import { Pims3Validations } from '../shared/pims3-validations';
import { InvestorService } from '../shared/investor.service';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Component({
  selector: 'app-password-reset',
  templateUrl: './password-reset.component.html',
  styleUrls: ['./password-reset.component.css']
})
export class PasswordResetComponent extends BaseUnsubscribeComponent implements OnInit {
   loading = false;
   submitted = false;

    constructor(private investorSvc: InvestorService, private alertSvc: AlertService) {
        super();
    }

  ngOnInit() { }

  /* Notes:
     - using Reactive, rather than template-driven forms, we create FormControl objects explicitly
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
                  .pipe(takeUntil(this.getUnsubscribe()))
                  .subscribe(updatedInvestor => {
                      if (updatedInvestor) {
                          this.alertSvc.success("Password successfully updated for " + updatedInvestor.firstName + " " + updatedInvestor.lastName);
                      } 
                   },
                  () => {
                      this.alertSvc.error("Error updating password at this time.  Please retry update at a later time.");
                  });
          }
          else {
              this.alertSvc.warn("Invalid entry(ies) found for password reset. Please verify old vs.new entry(ies).");
              return;
          }
      }

      this.loading = true;
  }

}
