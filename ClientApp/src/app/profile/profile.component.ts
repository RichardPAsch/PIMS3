import { Component, OnInit } from '@angular/core';
import { ProfileService } from '../shared/profile.service';
import { HttpErrorResponse } from '@angular/common/http';
import { Profile } from '../profile/profile';
import { FormGroup, FormControl, Validators, FormBuilder } from '@angular/forms';
import { tap, switchMap } from 'rxjs/operators';
import { forkJoin } from 'rxjs';
import { Pims3Validations } from '../shared/pims3-validations';


@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})

export class ProfileComponent implements OnInit {

    constructor(private profileSvc: ProfileService) {
        this.investor = JSON.parse(sessionStorage.getItem('currentInvestor')); 
    }

    investor: any;
    date1 = new Date();
    currentDateTime: string;
    isReadOnly: boolean = true;
    isReadOnlyPayMonthsAndDay: boolean = false;
    enteredTicker: string;
    assetProfile: Profile = new Profile();
    assetProfileForm = new FormGroup({
        ticker: new FormControl('', [Validators.required, Validators.maxLength(6)]),
        divRate: new FormControl(0, [Validators.required, Validators.min(0.001), Validators.max(30.00), Pims3Validations.isNumberValidator()]),
        divYield: new FormControl(0, [Validators.required, Validators.min(0.5), Validators.max(45), Pims3Validations.isNumberValidator()]),
        tickerDesc: new FormControl('', [Validators.required, Validators.maxLength(50)]),
        divFreq: new FormControl('', [Validators.required, Validators.maxLength(1), Pims3Validations.divFreqValidator()]),
        peRatio: new FormControl(1, [Validators.min(1), Pims3Validations.isNumberValidator()]),
        eps: new FormControl(1, [Validators.min(0.25), Pims3Validations.isNumberValidator()]),
        unitPrice: new FormControl(0, [Validators.required, Validators.min(0.50), Pims3Validations.isNumberValidator()]),
        divPayMonths: new FormControl('1', [Pims3Validations.divPayMonthsValidator(), Validators.maxLength(8)]),
        divPayDay: new FormControl(0, [Validators.required, Validators.min(1), Validators.max(31)])
    });
    assetProfileFreqAndMonths: any;

    // General validation flags.
    btnNewProfileSubmitted: boolean = false;
    btnUpdateProfileSubmitted: boolean = false;
    // Flag for user response cancelling confirmation prompt to new Profile creation.
    cancelledNewProfileCreation: boolean = false;
    divPayMonthsIsDisabled: boolean = true;


    // Button availability based on user events.
    btnCreateProfileIsDisabled: boolean = true;
    btnUpdateProfileIsDisabled: boolean = true;
    btnGetProfileIsDisabled: boolean = true;
    btnGetDbProfileIsDisabled: boolean = true;

    // Convenience getter for easy access to form fields.
    get formFields() { return this.assetProfileForm.controls; }

    ngOnInit() {
        let idx = this.date1.toString().indexOf("GMT");
        this.currentDateTime = this.date1.toString().substr(0, idx);
    }

    enableDisableDivPayMonths(): void {
        this.divPayMonthsIsDisabled = this.assetProfileForm.controls["divFreq"].value == "M" ? true : false;
    }


    enableButtonsForTicker() {
        if (this.assetProfileForm.controls["ticker"].value.length > 0) {
            this.btnGetProfileIsDisabled = false;
            this.btnGetDbProfileIsDisabled = false;
            this.btnCreateProfileIsDisabled = true;
            this.isReadOnly = false;
        }
        
    }


    getProfile(): void {
        // ** Currently using 3rd party API (Tiingo) for Profile data fetches. **

        // Obtain Profile basics.
        let profileData: any = this.profileSvc.getProfileData(this.assetProfileForm.controls["ticker"].value);
        // Obtain dividend 'frequency' & 'months paid' calculations via parsing of 12 month pricing/dividend history.
        let dividendData: any = this.profileSvc.getProfileDividendInfo(this.assetProfileForm.controls["ticker"].value);

        // Using forkJoin(), rather than subscribing x2, in obtaining a single Observable array containing all combined needed data.
        let combined = forkJoin(profileData, dividendData);

        // If no Profile info found for entered ticker, an 'error' condition will be generated upon subscribing, thus prompting the investor
        // for creation of a custom Profile.
        combined.subscribe(profileAndDivInfoArr => {
            let profileElement: any = profileAndDivInfoArr[0];
            // Scenario may arise where we have a valid ticker, but with incomplete or non-existent pricing info: (profileAndDivInfoArr[1]).
            if (profileElement.tickerSymbol == null || profileAndDivInfoArr[1] == null) {
                alert("Insufficient, or no web Profile data found for: \n" + this.assetProfileForm.controls["ticker"].value + "\nPlease check ticker validity.");
                this.initializeView(null, true);
                return;
            }

            let model: Profile = this.mapResponseToModel(profileElement);

            let dividendElement: any = profileAndDivInfoArr[1];
            if (model.divPayMonths == "0" || model.divPayMonths == null)
                model.divPayMonths = dividendElement.DM;

            if (model.divFreq == "" || model.divFreq == null || model.divFreq == "NA")
                model.divFreq = dividendElement.DF;
            
            this.initializeView(model, false);
            this.btnUpdateProfileIsDisabled = true;
            this.isReadOnlyPayMonthsAndDay = false;
        },
        (apiErr: HttpErrorResponse) => {
        if (this.assetProfileForm.controls["ticker"].value == "") {
            alert("A ticker symbol entry is required.")
            this.btnGetProfileIsDisabled = true;
            return;
        }

        let isNewProfile = confirm("No data found via web for: \n" + this.assetProfileForm.controls["ticker"].value + ".\nCreate new Profile?");
        if (isNewProfile) {
            this.isReadOnly = false;
            this.isReadOnlyPayMonthsAndDay = false;
            this.btnCreateProfileIsDisabled = false;
            this.btnUpdateProfileIsDisabled = true;
        } else {
            this.initializeView(null, true);
            this.cancelledNewProfileCreation = true;
        }
        this.btnGetProfileIsDisabled = true;
        this.btnGetDbProfileIsDisabled = true;
        });
    }


    getDbProfile(): void {
        this.profileSvc.getProfileDataViaDb(this.assetProfileForm.controls["ticker"].value, this.investor.username)
            .retry(2)
            .subscribe(profileResponse => {
                if (profileResponse == null) {
                    alert("No saved profile found for: \n" + this.assetProfileForm.controls["ticker"].value);
                    return;
                } else {
                    this.initializeView(this.mapResponseToModel(profileResponse[0]), false);
                    this.btnUpdateProfileIsDisabled = false;
                    this.btnCreateProfileIsDisabled = true;
                    this.btnGetProfileIsDisabled = true;
                    this.btnGetDbProfileIsDisabled = true;
                }
            },
            (apiErr: HttpErrorResponse) => {
                this.btnGetProfileIsDisabled = false;
                this.btnGetDbProfileIsDisabled = false;
                if (apiErr.error instanceof Error) {
                    alert("Error retreiving existing profile: \network or application error. Please try later.");
                }
            }
        ).unsubscribe;
        this.isReadOnlyPayMonthsAndDay = false;
    }


    createProfile(): void {

        // Method applicable to creating a custom Profile.
        let dbProfilePost$;
        let freqAndMonthsReconcile: boolean;

        if (this.assetProfileForm.invalid) {
            return;
        }

        // Dividend pay months are irrelevant for "M" frequency.
        if (this.assetProfileForm.controls["divFreq"].value == "M")  {
            freqAndMonthsReconcile = true;
        }
        else {
            freqAndMonthsReconcile = Pims3Validations.areDivMonthsAndDivFrequencyReconciled(
                this.assetProfileForm.controls["divPayMonths"].value,
                this.assetProfileForm.controls["divFreq"].value);
        }

        if (!freqAndMonthsReconcile) {
            alert("Unable to create Profile: \nentered 'div Pay Months' & 'dividend frequency' must reconcile.");
            this.btnNewProfileSubmitted = true;
            this.initializeView(null, true);
            return;
        }

        let profileToCreate = new Profile();
        profileToCreate.tickerSymbol = this.assetProfileForm.controls["ticker"].value;
        profileToCreate.tickerDesc = this.assetProfileForm.controls["tickerDesc"].value;
        profileToCreate.divPayMonths = this.assetProfileForm.controls["divPayMonths"].value;
        profileToCreate.divPayDay = this.assetProfileForm.controls["divPayDay"].value;
        profileToCreate.divRate = this.assetProfileForm.controls["divRate"].value;
        profileToCreate.divYield = this.assetProfileForm.controls["divYield"].value;
        profileToCreate.divFreq = this.assetProfileForm.controls["divFreq"].value;
        profileToCreate.PE_ratio = this.assetProfileForm.controls["peRatio"].value;
        profileToCreate.EPS = this.assetProfileForm.controls["eps"].value;
        profileToCreate.unitPrice = this.assetProfileForm.controls["unitPrice"].value;
        profileToCreate.investor = this.investor.username;

        try {
            dbProfilePost$ = this.profileSvc.saveNewProfile(profileToCreate);
        } catch (e) {
            alert("Error saving new Profile for : " + profileToCreate.tickerSymbol + "/ndue to " + e.message);
        }

        let posting = dbProfilePost$.pipe(
            tap(newProfileResponse => {
                if (newProfileResponse) {
                    alert("New Profile for : \n" + profileToCreate.tickerSymbol + "\n successfully created!");
                    this.btnGetProfileIsDisabled = true;
                    this.btnGetDbProfileIsDisabled = true;
                    this.btnCreateProfileIsDisabled = true;
                    this.btnUpdateProfileIsDisabled = true;
                } else {
                    this.btnGetProfileIsDisabled = true;
                    this.btnGetDbProfileIsDisabled = true;
                    this.btnCreateProfileIsDisabled = false;
                    this.btnUpdateProfileIsDisabled = true;
                }
                this.btnNewProfileSubmitted = true;
                this.initializeView(null, true);
            })
        );

        posting.subscribe();
    }

       
    mapResponseToModel(webProfile: any): Profile {

        /* available response fields:
             * dividendFreq,dividendMonths,dividendPayDay,dividendRate,dividendYield,earningsPerShare,tickerDescription,
             * tickerSymbol,unitPrice,peRatio,exDividendDate
        */
        let profileModel = new Profile();
        profileModel.tickerSymbol = webProfile.tickerSymbol;
        profileModel.tickerDesc = webProfile.tickerDescription;
        profileModel.divFreq = webProfile.dividendFreq == "NA" || webProfile.dividendFreq == ""
            ? "NA"
            : webProfile.dividendFreq;

        if (profileModel.divFreq != "M") {
            profileModel.divPayMonths = webProfile.dividendMonths == "0" || webProfile.dividendMonths == ""
                ? "0"
                : webProfile.dividendMonths;
        }
        else
            profileModel.divPayMonths = "N/A";

        profileModel.divPayDay = webProfile.dividendPayDay;
        profileModel.divYield = webProfile.dividendYield;
        profileModel.EPS = webProfile.earningsPerShare;
        profileModel.unitPrice = webProfile.unitPrice;
        profileModel.PE_ratio = webProfile.peRatio == null
            ? 0
            : webProfile.peRatio;
        profileModel.divRate = webProfile.dividendRate;

        return profileModel;
    }


    initializeView(profileData: Profile, refresh: boolean): void {

        this.assetProfileForm.setValue({
            ticker: refresh ? '' : profileData.tickerSymbol,
            tickerDesc: refresh ? '' : profileData.tickerDesc,
            divRate: refresh ? 0 : profileData.divRate,
            divYield: refresh ? 0 : profileData.divYield,
            divFreq: refresh ? '' : profileData.divFreq,
            peRatio: refresh ? 0 : profileData.PE_ratio,
            eps: refresh ? 0 : profileData.EPS,
            unitPrice: refresh ? 0 : profileData.unitPrice,
            divPayMonths: refresh ? '' : profileData.divPayMonths,
            divPayDay: refresh ? '' : profileData.divPayDay
        });

        this.btnUpdateProfileIsDisabled = refresh ? true : false;
    }


    updateProfile(): void { 

        let profileToUpdate = new Profile();
        let dbProfileGet$;
        let dbProfilePut$

        profileToUpdate.tickerSymbol = this.assetProfileForm.controls["ticker"].value;
        profileToUpdate.divPayMonths = this.assetProfileForm.controls["divPayMonths"].value;
        profileToUpdate.divPayDay = this.assetProfileForm.controls["divPayDay"].value;
        profileToUpdate.tickerDesc = this.assetProfileForm.controls["tickerDesc"].value;
        profileToUpdate.divYield = this.assetProfileForm.controls["divYield"].value;
        profileToUpdate.divRate = this.assetProfileForm.controls["divRate"].value;
        profileToUpdate.unitPrice = this.assetProfileForm.controls["unitPrice"].value;

        try {
            dbProfileGet$ = this.profileSvc.getProfileDataViaDb(profileToUpdate.tickerSymbol, this.investor.username);
            dbProfilePut$ = this.profileSvc.updateProfile(profileToUpdate);
        } catch (e) {
            alert("Error obtaining existing Profile for : " + profileToUpdate.tickerSymbol);
        }

        // RxJS : tap() - returned value(s) are untouchable, as opposed to edit/transform capability available via map().
        let combined = dbProfileGet$.pipe(
            switchMap(profileInfo => {
                return dbProfilePut$.pipe(
                        tap(profileUpdate => {
                            if (profileUpdate) {
                                alert("Existing Profile for : \n" + profileInfo[0].tickerSymbol + "\n successfully updated!");
                                this.btnGetProfileIsDisabled = true;
                                this.btnGetDbProfileIsDisabled = true;
                                this.btnCreateProfileIsDisabled = true;
                                this.btnUpdateProfileIsDisabled = true;
                                this.btnUpdateProfileSubmitted = true;
                            }
                            else {
                                // TODO: Logging ?
                                alert("Error updating existing local Profile for \n" + profileInfo[0].tickerSymbol + "\nRetry later?");
                                this.btnGetProfileIsDisabled = false;
                                this.btnGetDbProfileIsDisabled = true;
                                this.btnCreateProfileIsDisabled = true;
                                this.btnUpdateProfileIsDisabled = true;
                                this.btnUpdateProfileSubmitted = false;
                            }
                            this.initializeView(null, true);
                            this.btnUpdateProfileIsDisabled = true;
                        })
                   );
            })
        );

        combined.subscribe();
    }


    


}
