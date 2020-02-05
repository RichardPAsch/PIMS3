import { ValidatorFn, AbstractControl, FormGroup } from '@angular/forms';

export class Pims3Validations {

    static divFreqValidator(): ValidatorFn {

        // Returns a ValidatorFn, which takes a control, & returns either an object or null;
        // returns object, if applicable, which consists of key(type string) & value (type 'any')
        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null) {
                return (control.value == "A" || control.value == "S" || control.value == "Q" || control.value == "M")
                    ? null                                  // validation Ok. 
                    : { divFreq: { value: control.value } } // validation error
            }
            return null;
        };
    }


    static isNumberValidator(): ValidatorFn {

        // Applicable to: 'divRate', 'divYield', 'peRatio', 'eps', & 'unitPrice' for 'Asset Profile' inputs.
        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null) {
                return (isNaN(control.value))
                    ? { isNumber: { value: control.value } } // validation error
                    : null                                   // validation Ok.
            }
            return null;
        };
    }


    static areDivMonthsAndDivFrequencyReconciled(monthsEntered: string, freqEntered: string): boolean {

        // Ensure entered dividend payment months match entered dividend payment frequency.
        // "M"onthly freq checks via 'divPayMonthsIsDisabled' flag.
        let monthsArr;
        let result: boolean = false;

        if (monthsEntered.indexOf(',') > -1) {
            monthsArr = monthsEntered.split(',');
            if (freqEntered == "A" && monthsArr.length == 1)
                result = true;
            else if (freqEntered == "S" && monthsArr.length == 2)
                result = true;
            else if (freqEntered == "Q" && monthsArr.length == 4)
                result = true;
        } else {
            result = (freqEntered != "A" && (parseInt(monthsEntered) >= 1 && parseInt(monthsEntered) <= 12 )) ? false : true;
        }
        return result;
    }


    static divPayMonthsValidator(): ValidatorFn {

        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null) {
                let validationResult = this.validateDividendPaymentMonths(control.value);
                return validationResult ? null : { divPayMonths: { value: "bad month(s)" } };
            };
            return null;
        }
    }


    static passwordValidator(): ValidatorFn {

        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null && control.value != "") {
                let passwordValidationResult = this.validatePassword(control.value);
                return passwordValidationResult ? null : { pwrd: { value: "bad password" } };
            }
        };
    }


    static passwordConfirmationValidator(): ValidatorFn {

        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null && control.value != "") {
                let passwordValidationResult = this.validatePassword(control.value);
                return passwordValidationResult
                    ? null
                    : { newPwrd: { value: "bad password confirmation" } };
            }
        };
    }


    private static validateDividendPaymentMonths(pymtMonths: string): boolean {

        let isValidMonths: boolean;
        let months: string = "";

        if (pymtMonths.length == 1) {
            if (isNaN(parseInt(pymtMonths)))
                return false;
            else {
                return parseInt(pymtMonths) == 0 ? false : true;
            }
        }

        // Character checks.
        for (var i = 0; i < pymtMonths.length; i++) {

            // Terminate iteration if already invalid at this point.
            if (isValidMonths == false)
                return false;

            if (!isNaN(parseInt(pymtMonths.charAt(i))) || pymtMonths.charAt(i) == ',') {
                months += pymtMonths.charAt(i);
                isValidMonths = true;
            }
            else
                isValidMonths = false;
        }


        // Format checks.
        if (pymtMonths.indexOf(",") == -1) {
            // No comma-seperated value, eg, "10" for October.
            if (months.length == 2 && parseInt(months) <= 12) {
                isValidMonths = true;
            }
            else
                isValidMonths = false;
        }
        else {
            // String value with comma(s).
            let monthsArr = months.split(',');
            // Valid months?
            for (var i = 0; i < monthsArr.length; i++) {
                if (parseInt(monthsArr[i]) > 12 || parseInt(monthsArr[i]) <= 0) {
                    isValidMonths = false;
                } else {
                    isValidMonths = true;
                }
            }
        }
        return isValidMonths;
    }


    private static validatePassword(pwrd: string): boolean {

        let hasLower = false;
        let hasUpper = false;
        let hasNum = false;
        let hasSpecialChar = false;

        // At minimum, apply the following:
        // One lower case letter.
        const lowercaseRegex = new RegExp("(?=.*[a-z])");
        if (lowercaseRegex.test(pwrd)) { hasLower = true; }

        // One upper case letter.
        const uppercaseRegex = new RegExp("(?=.*[A-Z])");
        if (uppercaseRegex.test(pwrd)) { hasUpper = true; }

        // One number.
        const numRegex = new RegExp("(?=.*\\d)");
        if (numRegex.test(pwrd)) { hasNum = true; }

        // One special char.
        const specialcharRegex = new RegExp("[!@#$%^&*(),.?\":{}|<>]");
        if (specialcharRegex.test(pwrd)) { hasSpecialChar = true; }

        let counter = 0;
        let checks = [hasLower, hasUpper, hasNum, hasSpecialChar];
        for (let i = 0; i < checks.length; i++) {
            if (checks[i]) { counter += 1; }
        }

        if (counter < 4) {
            return false;
        } else {
            return true;
        }
    }


        


    // ** Example1 from https://codinglatte.com/posts/angular/angular-building-custom-validators/
    //static ageLimitValidator(minAge: number, maxAge: number): ValidatorFn {
    //    return (control: AbstractControl): { [key: string]: any } | null => {
    //        // if control value is not null and is a number
    //        if (control.value !== null) {
    //            // return null  if it's in between the minAge and maxAge and is A valid Number
    //            return isNaN(control.value) || // checks if its a valid number
    //                control.value < minAge || // checks if its below the minimum age
    //                control.value > maxAge // checks if its above the maximum age
    //                ? { ageLimit: true } // return this incase of error
    //                : null; // there was not error
    //        }
    //        return null;
    //    };
    //}

    // Example2 from https://angular.io/guide/form-validation
    //export function forbiddenNameValidator(nameRe: RegExp): ValidatorFn {
    //    return (control: AbstractControl): { [key: string]: any } | null => {
    //        const forbidden = nameRe.test(control.value);
    //        return forbidden ? { 'forbiddenName': { value: control.value } } : null;
    //    };
    //}


    /* Sample validator for passwords from SO
     * // https://stackoverflow.com/questions/52043495/custom-password-validation-in-angular-5
            validate(ctrl: AbstractControl): {[key: string]: any} {
            let password = ctrl.value;

            let hasLower = false;
            let hasUpper = false;
            let hasNum = false;
            let hasSpecial = false;

            const lowercaseRegex = new RegExp("(?=.*[a-z])");// has at least one lower case letter
            if (lowercaseRegex.test(password)) {
              hasLower = true;
            }

            const uppercaseRegex = new RegExp("(?=.*[A-Z])"); //has at least one upper case letter
            if (uppercaseRegex.test(password)) {
              hasUpper = true;
            }

            const numRegex = new RegExp("(?=.*\\d)"); // has at least one number
            if (numRegex.test(password)) {
              hasNum = true;
            }

            const specialcharRegex = new RegExp("[!@#$%^&*(),.?\":{}|<>]");
            if (specialcharRegex.test(password)) {
              hasSpecial = true;
            }

            let counter = 0;
            let checks = [hasLower, hasUpper, hasNum, hasSpecial];
            for (let i = 0; i < checks.length; i++) {
              if (checks[i]) {
                counter += 1;
              }
            }

            if (counter < 2) {
              return { invalidPassword: true }
            } else {
              return null;
            }

          }

    */
   



}






      
