import { Component, OnInit } from '@angular/core';
import { ProfileService } from '../shared/profile.service';
import { HttpErrorResponse } from '@angular/common/http';
import { Profile } from '../profile/profile';
import { FormGroup, FormControl } from '@angular/forms';
import { tap, switchMap } from 'rxjs/operators';
import { Observable } from 'rxjs/Observable';
import { forkJoin } from 'rxjs';



@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})

export class ProfileComponent implements OnInit {

    constructor(private profileSvc: ProfileService) { }

    date1 = new Date();
    currentDateTime: string;
    isReadOnly: boolean = true;
    isReadOnlyPayMonthsAndDay: boolean = true;
    enteredTicker: string;
    assetProfile: Profile = new Profile();
    assetProfileForm = new FormGroup({
        ticker: new FormControl(''),
        divRate: new FormControl(0),
        divYield: new FormControl(0),
        tickerDesc: new FormControl(''),
        divFreq: new FormControl(''),
        peRatio: new FormControl(0),
        eps: new FormControl(0),
        unitPrice: new FormControl(0),
        divPayMonths: new FormControl(''),
        divPayDay: new FormControl(0)
    });
    assetProfileFreqAndMonths: any;
    btnNewProfileIsDisabled: boolean = true;
    btnUpdateProfileIsDisabled: boolean = true;

    ngOnInit() {
        let idx = this.date1.toString().indexOf("GMT");
        this.currentDateTime = this.date1.toString().substr(0, idx);
        const ctx = this;
    }

    getProfile(): void {
        // ** Currently using 'Tiingo' as 3rd party API for Profile data fetches. **
        // Obtain Profile basics.
        let profileData: any = this.profileSvc.getProfileData(this.assetProfileForm.controls["ticker"].value);
        // Obtain dividend 'frequency' & 'months paid' calculations via parsing of 12 month pricing history.
        let dividendData: any = this.profileSvc.getProfileDividendInfo(this.assetProfileForm.controls["ticker"].value);

        // Using forkJoin(), rather than subscribing x2, in obtaining a single Observable array for all needed data.
        let combined = forkJoin(profileData, dividendData);

        combined.subscribe(profileAndDivInfoArr => {
            let profileElement: any = profileAndDivInfoArr[0];
            if (profileElement.tickerSymbol == null) {
                alert("No Profile data found for: \n" + this.assetProfileForm.controls["ticker"].value + "\nPlease check ticker validity.");
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
            this.btnUpdateProfileIsDisabled = false;
            this.isReadOnlyPayMonthsAndDay = false;
        },
        (apiErr: HttpErrorResponse) => {
            alert("No Profile found via web for: \n" + this.assetProfileForm.controls["ticker"].value + "\nCheck ticker symbol alternatives.");
            this.initializeView(null, true);
        });
               
    }


    getDbProfile(): void {

        this.profileSvc.getProfileDataViaDb(this.assetProfileForm.controls["ticker"].value)
            .retry(2)
            .subscribe(profileResponse => {
                this.initializeView(this.mapResponseToModel(profileResponse[0]), false);
                this.btnUpdateProfileIsDisabled = true;
            },
            (apiErr: HttpErrorResponse) => {
                if (apiErr.error instanceof Error) {
                    alert("Error retreiving existing profile: \network or application error. Please try later.");
                }
                else {
                    // to be completed...
                    //let truncatedMsgLength = apiErr.error.errorMsg.indexOf(":") - 7;
                    //let isCustom = confirm(apiErr.error.errorMsg.substring(0, truncatedMsgLength) + "\nCreate custom Profile?");
                    //if (isCustom) {
                    //    alert("custom it is"); // enable fields for editing..
                    //}
                    //else
                    //    this.initializeView(null, true);
                }
            }
        ).unsubscribe;
        this.isReadOnlyPayMonthsAndDay = false;
    }


    createProfile(): void {
        alert("is disabled");
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

        try {
            dbProfileGet$ = this.profileSvc.getProfileDataViaDb(profileToUpdate.tickerSymbol);
            dbProfilePut$ = this.profileSvc.updateProfile(profileToUpdate);
        } catch (e) {
            alert("Error obtaining existing Profile for : " + profileToUpdate.tickerSymbol);
        }

        // RxJS : tap() - returned value(s) untouchable, as opposed to edit/transform capability via map().
        let combined = dbProfileGet$.pipe(
            switchMap(profileInfo => {
                return dbProfilePut$.pipe(
                        tap(profileUpdate => {
                            if (profileUpdate) {
                                alert("Existing Profile for : \n" + profileInfo[0].tickerSymbol + "\n successfully updated!");
                            }
                            else {
                                // TODO: Logging ?
                                alert("No existing Profile found for update; check portfolio for ticker validity.");
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
