import { Component, ViewChild, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import 'rxjs/add/operator/retry';
import { AgGridNg2 } from 'ag-grid-angular';
import { IncomeSummaryService } from '../income-summary/income-summary.service';
import { IncomeSummary } from '../income-summary/income-summary';
//import { PIMS_ErrorHandler } from '../error-logging/pims-error-handler';
//import { catchError } from 'rxjs/operators';


@Component({
  selector: 'income-summary-data',
  templateUrl: './income-summary.component.html',
  styleUrls: ['./income-summary.component.css']
})

export class IncomeSummaryComponent implements OnInit {
    private currentDate: Date = new Date();
    private currentYear: number = this.currentDate.getFullYear();
    public currentYearHeading: number = this.currentYear;
    public ytdIncomeSummary: IncomeSummary[];
    public incomeSummaryTotal: any;
    


    constructor(private incomeSummarySvc: IncomeSummaryService)
    { }


    @ViewChild('agGridIncomeSummary')
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
            .subscribe(summaryResults => {
                let mappedSummaryResults = this.mapIncomeSummaryForGrid(summaryResults);
                this.agGridIncomeSummary.api.setRowData(mappedSummaryResults);
                let runningTotal: any = 0.0;
                let ctr: any = 0;

                for (ctr in mappedSummaryResults) {
                    runningTotal += parseFloat(mappedSummaryResults[ctr].AmountRecvd.toString());
                }
                this.incomeSummaryTotal = runningTotal;
            },
                (error: HttpErrorResponse) => {  // activated only on http call errors.
                    alert("Error retreiving income summary data.")
                    //let errHndlr = new PIMS_ErrorHandler(null);
                    //if (error == undefined) {
                    //    errHndlr.handleError("no data"); // ok; TODO: populate 'logException' & pass as param.
                    //}

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

  

