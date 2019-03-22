import { Component, OnInit } from '@angular/core';
import { ProfileService } from '../shared/profile.service';
import { HttpErrorResponse } from '@angular/common/http';
import { Profile } from '../profile/profile';
import { FormGroup, FormControl } from '@angular/forms';

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
        divPayMonths: new FormControl('')
    });
    assetProfileFreqAndMonths: any;
    btnNewProfileIsDisabled: boolean = true;

    ngOnInit() {
        let idx = this.date1.toString().indexOf("GMT");
        this.currentDateTime = this.date1.toString().substr(0, idx);
    }

    
    getProfile(): void {
        this.profileSvc.getProfileData(this.assetProfileForm.controls["ticker"].value)
            .retry(2)
            .subscribe(profileResponse => {
                this.initializeView(this.mapResponseToModel(profileResponse), false);
            },
            (apiErr: HttpErrorResponse) => {
                if (apiErr.error instanceof Error) {
                    alert("Error retreiving profile: \network or application error. Please try later.");
                }
                else {
                    let truncatedMsgLength = apiErr.error.errorMsg.indexOf(":") - 7;
                    let isCustom = confirm(apiErr.error.errorMsg.substring(0, truncatedMsgLength) + "\nCreate custom Profile?");
                    if (isCustom) {
                        alert("custom it is"); // enable fields for editing..
                    }
                    else
                        this.initializeView(null, true);
                }
            }
        ).unsubscribe;
        this.getProfileFreqAndMonths(this.assetProfileForm.controls["ticker"].value);
    }


    getProfileFreqAndMonths(ticker: string): void {
        this.profileSvc.getProfileDividendInfo(ticker)
            .retry(2)
            .subscribe(profileResponse => {
                this.assetProfileFreqAndMonths = profileResponse;
            },
                (apiErr: HttpErrorResponse) => {
                    if (apiErr.error instanceof Error) {
                        alert("Error retreiving profile dividend frequency and payment months: \network or application error. Please try later.");
                    }
                    else {
                        let truncatedMsgLength = apiErr.error.errorMsg.indexOf(":") - 7;
                        alert("Error retreiving profile dividend frequency and payment months due to : \n" + apiErr.error.errorMsg.substring(0, truncatedMsgLength)
                            + "."
                            + "\nCheck ticker validity.");
                    }
                }
            );
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
            ? this.assetProfileFreqAndMonths.DF
            : webProfile.dividendFreq;

        if (profileModel.divFreq != "M") {
            profileModel.divPayMonths = webProfile.dividendMonths == "0" || webProfile.dividendMonths == ""
                ? this.assetProfileFreqAndMonths.DM
                : webProfile.dividendMonths;
        }
        else
            profileModel.divPayMonths = "N/A";
        
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
            divPayMonths: refresh ? '' : profileData.divPayMonths
        });

    }

}
