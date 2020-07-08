import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { ProfileService } from '../shared/profile.service';
import { ProjectionProfile } from '../income-projections/projection-profile';
import { HttpErrorResponse } from '@angular/common/http';
import 'rxjs/add/operator/retry';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Component({
  selector: 'app-income-projections',
  templateUrl: './income-projections.component.html',
  styleUrls: ['./income-projections.component.css']
})
export class IncomeProjectionsComponent extends BaseUnsubscribeComponent implements OnInit  {

    constructor(private profileSvc: ProfileService, private alertSvc: AlertService) {
        super(); // Enable reference to base class properties and constructor.
    }

    // Decorator references the child component inside the template.
    @ViewChild('agGrid', {static: false})
    agGrid: AgGridNg2;

    
    columnDefs = [
        { headerName: "Ticker", field: "ticker", sortable: true, filter: true, checkboxSelection: true, width: 108, valueFormatter: upperCaseFormatter },
        { headerName: "Capital ($)", field: "capital", width: 110, type: "numericColumn" },
        { headerName: "Price( $)", field: "unitPrice", width: 100, type: "numericColumn" },
        {
            headerName: "Dividend",
            groupId: "dividendGroup",
            children: [
                { headerName: "Rate ($)", field: "dividendRate", width: 90, type: "numericColumn" },
                { headerName: "Yield (%)", field: "dividendYield", width: 100, type: "numericColumn", editable: false }
            ]
        },
        { headerName: "Income ($)", field: "projectedMonthlyIncome", width: 135, editable: false, type: "numericColumn" },
    ];

    rowData: any;

    defaultColDef = {
        resizable: true,
        sortable: true,
        filter: true,
        editable: true
    };

    getProjections()
    {
        let profiles = new Array<ProjectionProfile>();
        var selectedNodes = this.agGrid.api.getSelectedNodes();
        var selectedData = selectedNodes.map(node => node.data);
        
        for (let gridRow = 0; gridRow < selectedData.length; gridRow++)
        {
            if (selectedData[gridRow].ticker == "" || selectedData[gridRow].capital == 0) {
                this.alertSvc.warn("Unable to process projection(s), please check for missing minimum data: 'ticker' and / or 'capital' entry(ies).");
                return;
            } else {
                // Check for manual entries, bypassing web-based fetched profile data for calculations, e.g., perhaps due 
                // to no, or incomplete data available via web service.
                if (selectedData[gridRow].unitPrice > 0 && selectedData[gridRow].dividendRate > 0) {
                    // Calculate projection based on submitted grid entries.
                    let manualProfile: { [key: string]: any } = {
                        tickerSymbol: selectedData[gridRow].ticker,
                        unitPrice: selectedData[gridRow].unitPrice,
                        dividendRate: selectedData[gridRow].dividendRate,
                        dividendYield: selectedData[gridRow].dividendYield > 0 ? selectedData[gridRow].dividendYield : 0
                    };
                    profiles.push(this.calculateProjections(manualProfile, selectedData[gridRow].capital, false));
                    // Last row ?
                    if (gridRow == selectedData.length - 1 )
                        this.agGrid.api.setRowData(profiles);
                } else {
                    // For efficiency, we'll check for CUSTOM profile(s) before invoking HTTP service call for NYSE profile.
                    let loggedInvestor = JSON.parse(sessionStorage.getItem('currentInvestor'));
                    this.profileSvc.getProfileDataViaDb(selectedData[gridRow].ticker, loggedInvestor.username)
                        .retry(2)
                        .pipe(takeUntil(this.getUnsubscribe()))
                        .subscribe(customProfileResponse => {
                            if (customProfileResponse != null) {
                                profiles.push(this.calculateProjections(customProfileResponse[0], selectedData[gridRow].capital, true));
                                this.agGrid.api.setRowData(profiles);
                            } else {
                                this.getUnsubscribe();
                                this.profileSvc.getProfileData(selectedData[gridRow].ticker)
                                    .retry(2)
                                    .pipe(takeUntil(this.getUnsubscribe()))
                                    .subscribe(responseProfile => {
                                        profiles.push(this.calculateProjections(responseProfile, selectedData[gridRow].capital, false));
                                        this.agGrid.api.setRowData(profiles);
                                    },
                                    (apiErr: HttpErrorResponse) => {
                                        if (apiErr.error instanceof Error) {
                                            this.alertSvc.error("Error processing income projection(s), due to network error. Please try again later.");
                                        }
                                        else {
                                            this.alertSvc.warn("No NYSE, nor custom Profile data was found for 1 or more entered ticker(s). Please recheck ticker validity.")
                                        }
                                    } 
                                );
                            }
                        },
                        (apiErr: HttpErrorResponse) => {
                            if (apiErr.error instanceof Error) {
                                // Client-side or network error encountered.
                                this.alertSvc.error("Error processing income projection(s) for custom Profile due to network error. Please try again later.");
                            }
                        }

                    )  // line 84 subscribe
                }      // line 78 else
            }
        }              // end for
    }


    ngOnInit() {
        this.rowData = [
            { ticker: "XYZ", capital: 0 },
            { ticker: "", capital: 0 },
            { ticker: "", capital: 0 },
            { ticker: "", capital: 0 },
            { ticker: "", capital: 0 }
        ];
    }

   
    calculateProjections(recvdProfile: any, capitalToInvest: number, isCustomProfile: boolean): ProjectionProfile {

        // 'ProjectionProfile' created to accommodate projectedIncome for grid purposes, as income projection
        //  is not a 'Profile.cs' attribute.
        let profileRecord = new ProjectionProfile();
        let calculatedShares: number = +(capitalToInvest / recvdProfile.unitPrice).toFixed(2);
        let calculatedAnnualizedRate: number = 0;
        let calculatedMonthlyRate: number = 0;
               
        profileRecord.ticker = recvdProfile.tickerSymbol;
        profileRecord.capital = capitalToInvest;
        profileRecord.unitPrice = recvdProfile.unitPrice;
        // Rate recv'd via Tiingo is per frequency of dividend distributions.
        profileRecord.dividendRate = recvdProfile.dividendRate;
        profileRecord.dividendYield = recvdProfile.dividendYield;
        // Trap for possible undefined dividendFreq in manual override scenarios.
        profileRecord.dividendFreq = recvdProfile.dividendFreq == undefined ? "M" : recvdProfile.dividendFreq;

       
        if (isCustomProfile) {
            profileRecord.projectedMonthlyIncome = +(calculatedShares * (profileRecord.dividendRate/12)).toFixed(2);
        } else {
            switch (profileRecord.dividendFreq) {
                case "Q":
                    calculatedAnnualizedRate = +(profileRecord.dividendRate * 4).toFixed(4);
                    break;
                case "S":
                    calculatedAnnualizedRate = +(profileRecord.dividendRate * 2).toFixed(4);
                    break;
                case "A":
                    calculatedAnnualizedRate = +(profileRecord.dividendRate * 1).toFixed(4);
                    break;
                case "M":
                    calculatedAnnualizedRate = +(profileRecord.dividendRate * 12).toFixed(4);
                    break;
            }

            calculatedMonthlyRate = +(calculatedAnnualizedRate / 12).toFixed(4);
            profileRecord.projectedMonthlyIncome = +(calculatedShares * calculatedMonthlyRate).toFixed(2);
        }
       
        return profileRecord;
    };

}

function upperCaseFormatter(valueToFormat) {
    return valueToFormat.data.ticker.toUpperCase();
}


