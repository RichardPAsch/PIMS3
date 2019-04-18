import { ValidatorFn, AbstractControl } from '@angular/forms';

export class Pims3Validations {

     static divFreqValidator(): ValidatorFn {

        // Returns a ValidatorFn, which takes a control, & returns either an object or null.
        // returns object, if applicable which consists of key(type string) & value (type 'any')
        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null) {
                //let x = 2;
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


    static divPayMonthsValidator(): ValidatorFn {

        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null) {
                let validationResult = this.validateDividendPaymentMonths(control.value);
                return validationResult ? null : { divPayMonths: { value: "bad month(s)" } };
            };
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
   



}






      
