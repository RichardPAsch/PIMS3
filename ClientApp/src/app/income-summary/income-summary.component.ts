import { Component, ViewChild, OnInit } from '@angular/core';
import 'rxjs/add/operator/retry';
import { AgGridNg2 } from 'ag-grid-angular';
import { IncomeSummaryService } from '../income-summary/income-summary.service';
import { IncomeSummary } from '../income-summary/income-summary';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Component({
  selector: 'income-summary-data',
  templateUrl: './income-summary.component.html',
  styleUrls: ['./income-summary.component.css']
})

export class IncomeSummaryComponent extends BaseUnsubscribeComponent implements OnInit {

    private currentDate: Date = new Date();
    private currentYear: number = this.currentDate.getFullYear();
    public currentYearHeading: number = this.currentYear;
    public ytdIncomeSummary: IncomeSummary[];
    public incomeSummaryTotal: any;
    

    constructor(private incomeSummarySvc: IncomeSummaryService)
    {
        super();
    }


    @ViewChild('agGridIncomeSummary', { static: false })
    agGridIncomeSummary: AgGridNg2;


    ngOnInit() {
        this.processIncomeSummary();
    }
    
    columnDefs = [
        { headerName: "Month recv'd", field: "MonthRecvd", sortable: true, checkboxSelection: true, width: 118, resizable: true },
        { headerName: "Amount recv'd", field: "AmountRecvd", width: 130, resizable: true, cellStyle: { textAlign: "right" },
            filter: "agNumberColumnFilter",
            filterParams: {
                applyButton: true,
                clearButton: true,
                apply: true
            }
        },
        { headerName: "YTD Avg.", field: "YtdAverage", width: 97, resizable: true, cellStyle: { textAlign: "right" } },
        { headerName: "Rolling 3 Mo. Avg.", field: "Rolling3MonthAverage", width: 152, resizable: true, cellStyle: { textAlign: "right" } },
    ];

    rowData: any;

    onYearSelected(yearsBackDated: number) {
        this.currentYearHeading = this.currentYear - yearsBackDated;
        this.processIncomeSummary(yearsBackDated);
    }


    processIncomeSummary(backDatedYears: number = 0) {
        this.incomeSummarySvc.BuildIncomeSummary(backDatedYears)
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(summaryResults => {
                let mappedSummaryResults = this.mapIncomeSummaryForGrid(summaryResults);
                this.agGridIncomeSummary.api.setRowData(mappedSummaryResults);
                let runningTotal: any = 0.0;
                let ctr: any = 0;

                for (ctr in mappedSummaryResults) {
                    runningTotal += parseFloat(mappedSummaryResults[ctr].AmountRecvd.toString());
                }
                this.incomeSummaryTotal = runningTotal;
            }
        );
    }


    mapIncomeSummaryForGrid(recvdSummary: any): IncomeSummary[] {

        if (recvdSummary != null) {
            let mappedSummaryArr = new Array<IncomeSummary>();
            for (let idx = 0; idx < recvdSummary.length; idx++) {
                let modelRecord = new IncomeSummary();
                modelRecord.MonthRecvd = recvdSummary[idx].monthRecvd;
                modelRecord.AmountRecvd = (recvdSummary[idx].amountRecvd).toFixed(2);
                modelRecord.YtdAverage = (recvdSummary[idx].ytdAverage).toFixed(2);
                modelRecord.Rolling3MonthAverage = (recvdSummary[idx].rolling3MonthAverage).toFixed(2);

                mappedSummaryArr.push(modelRecord);
            }
            return mappedSummaryArr;
        } else
            return null;
    }


}

  

