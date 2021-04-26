import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';
import { Distribution } from '../distributions/distribution';
import { ProfileService } from '../shared/profile.service';
import { AlertService } from '../shared/alert.service';
import { GlobalsService } from '../shared/globals.service';

@Component({
    selector: 'app-distributions',
    templateUrl: './distributions.component.html',
    styleUrls: ['./distributions.component.css']
})
export class DistributionsComponent extends BaseUnsubscribeComponent implements OnInit {

    
    constructor(private profileSvc: ProfileService, private alertSvc: AlertService, private globalsSvc: GlobalsService) {
        super();
    }

    @ViewChild('agGridDistributions', { static: false })
    agGridDistributions: AgGridNg2;

    public quarterlyCount: number = 0;
    public monthlyCount: number = 0;
    public semi_annualCount: number = 0;
    public annualCount: number = 0;


    ngOnInit() {
        this.getDistributionSchedules();
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, filter: true, width: 145, resizable: true },
        { headerName: "Frequency", field: "frequency", sortable: true, filter: true, width: 105, resizable: true },
        { headerName: "Month(s) Paid", field: "months", filter: true, width: 125, resizable: true },
        { headerName: "Day", field: "day", sortable: true, filter: true, width: 75, resizable: true },
    ];

    rowData: any; 


    public getDistributionSchedules(): any {
    
        this.profileSvc.fetchDistributionSchedules()
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(responseSchedules => {
                this.calculateDistributionCounts(responseSchedules);
                this.processDivMonthsCollectionSort(responseSchedules); 
                this.agGridDistributions.api.setRowData(this.mapDistributionsForGrid(responseSchedules));
            },
            (apiError: HttpErrorResponse) => {
                if (apiError.error instanceof Error) {
                    this.alertSvc.error("Error retreiving profile dividend schedules; possible network issue.");
                }
                else {
                    this.alertSvc.error("Error retreiving profile dividend schedules. Please try later.");
                }
            }
        )
    }


    processDivMonthsCollectionSort(sourceCollection: any[]): any[] {

        for (let index = 0; index < sourceCollection.length; index++) {
            if (sourceCollection[index].distributionFrequency == "S" || sourceCollection[index].distributionFrequency == "Q") {
                sourceCollection[index].distributionMonths = this.globalsSvc.sortDivPayMonthsValue(sourceCollection[index].distributionMonths);
            }
        }
        return sourceCollection
    }

    mapDistributionsForGrid(recvdDistributions: any) : Distribution[] {

        let mappedDistributions = new Array<Distribution>();
        for (let i = 0; i < recvdDistributions.length; i++) {
            let distributionModelRecord = new Distribution();

            distributionModelRecord.tickerSymbol = recvdDistributions[i].tickerSymbol;
            distributionModelRecord.frequency = recvdDistributions[i].distributionFrequency;
            distributionModelRecord.months = recvdDistributions[i].distributionMonths;
            distributionModelRecord.day = recvdDistributions[i].distributionDay;

            mappedDistributions.push(distributionModelRecord);
        }

        return mappedDistributions;
    }

    calculateDistributionCounts(sourceData: any) {

        for (let i = 0; i < sourceData.length; i++) {

            switch (sourceData[i].distributionFrequency) {
                case "M":
                    this.monthlyCount += 1;
                    break;
                case "Q":
                    this.quarterlyCount += 1;
                    break;
                case "S":
                    this.semi_annualCount += 1;
                    break;
                case "A":
                    this.annualCount += 1;
                    break;
            }
        }

    }

}
