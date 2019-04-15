import { ValidatorFn, AbstractControl } from '@angular/forms';

export class Pims3Validations {

     static divFreqValidator(): ValidatorFn {

        // Returns a ValidatorFn, which takes a control, & returns either an object or null.
        // returns object, if applicable which consists of key(type string) & value (type 'any')
        return (control: AbstractControl): { [key: string]: any } | null => {
            if (control != null) {
                return (control.value == "A" || control.value == "S" || control.value == "Q" || control.value == "M")
                    ? null                                  // validation Ok. 
                    : { divFreq: { value: control.value } } // validation error
            }
            return null;
        };
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






      
