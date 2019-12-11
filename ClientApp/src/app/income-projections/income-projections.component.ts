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
    @ViewChild('agGrid')
    agGrid: AgGridNg2;

    
    columnDefs = [
        { headerName: "Ticker", field: "ticker", sortable: true, filter: true, checkboxSelection: true, width: 100 },
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
                this.alertSvc.warn("Unable to process projection(s), please check for missing 'ticker' and / or 'capital' entry(ies).");
                return;
            } else {
                // Check for manual entries, bypassing web-based fetched profile data for calculations, e.g., due 
                // to no, or incomplete data available via web service.
                if (selectedData[gridRow].unitPrice > 0 && selectedData[gridRow].dividendRate > 0) {
                    // Calculate projection based on submitted grid entries.
                    let manualProfile: { [key: string]: any } = {
                        tickerSymbol: selectedData[gridRow].ticker,
                        unitPrice: selectedData[gridRow].unitPrice,
                        dividendRate: selectedData[gridRow].dividendRate,
                        dividendYield: selectedData[gridRow].dividendYield > 0 ? selectedData[gridRow].dividendYield : 0
                    };
                    profiles.push(this.initializeGridModel(manualProfile, selectedData[gridRow].capital));
                    // Last row ?
                    if (gridRow == selectedData.length - 1 )
                        this.agGrid.api.setRowData(profiles);
                } else {
                    this.profileSvc.getProfileData(selectedData[gridRow].ticker)
                        .retry(2)     // Retrying request in case of transient errors, e.g, slow network, no internet access.
                        // Ensure no memory leaks via 'BaseUnsubscribeComponent'; 'takeUntil' MUST be last operator within pipe().
                        .pipe(takeUntil(this.getUnsubscribe()))
                        .subscribe(responseProfile => {
                            profiles.push(this.initializeGridModel(responseProfile, selectedData[gridRow].capital));
                            this.agGrid.api.setRowData(profiles);
                        },
                        (apiErr: HttpErrorResponse) => {
                            if (apiErr.error instanceof Error) {
                                // Client-side or network error encountered.
                                this.alertSvc.error("Error processing income projection(s), due to network error. Please try again later.");
                            }
                            else {
                                //API returns unsuccessful response status codes, e.g., 404, 500 etc.
                                this.alertSvc.error("Error processing income projection(s) due to "
                                    + "'" + apiErr.error.errorMsg + "'"
                                    + ". Please check ticker validity");
                            }
                        }
                    ) // end subscribe
                }
            }
        } // end for
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

    // Available via OnDestroy interface & executed upon navigation within the app.
    //ngOnDestroy() {
    //    this.unsubscribe$.next();
    //    this.unsubscribe$.complete();
    //}


    initializeGridModel(recvdProfile: any, capitalToInvest: number): ProjectionProfile {

        // Using specifically created 'ProjectionProfile' to accommodate projectedIncome for grid purposes, as
        // income projection is not a 'Profile.cs' attribute.
        let profileRecord = new ProjectionProfile();
        profileRecord.ticker = recvdProfile.tickerSymbol;
        profileRecord.ticker = profileRecord.ticker.toUpperCase();
        profileRecord.capital = capitalToInvest;
        profileRecord.unitPrice = recvdProfile.unitPrice;
        profileRecord.dividendRate = recvdProfile.dividendRate;
        profileRecord.dividendYield = recvdProfile.dividendYield;
        profileRecord.dividendFreq = recvdProfile.dividendFreq;

        let calculatedIncome = ((capitalToInvest / profileRecord.unitPrice) * profileRecord.dividendRate);

        switch (profileRecord.dividendFreq) {
            case "Q":
                profileRecord.projectedMonthlyIncome = +(calculatedIncome / 3).toFixed(2);
                break;
            case "S":
                profileRecord.projectedMonthlyIncome = +(calculatedIncome / 6).toFixed(2);
                break;
            case "A":
                profileRecord.projectedMonthlyIncome = +(calculatedIncome / 12).toFixed(2);
                break;
            case "M":
                profileRecord.projectedMonthlyIncome = +calculatedIncome.toFixed(2);
                break;
        }
       
        return profileRecord;
    };
    
}
