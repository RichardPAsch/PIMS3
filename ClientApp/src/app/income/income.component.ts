import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { IncomeService } from '../income/income.service';
import { Income } from '../income/income';
import { HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';

@Component({
  selector: 'app-income',
  templateUrl: './income.component.html',
  styleUrls: ['./income.component.css']
})
export class IncomeComponent extends BaseUnsubscribeComponent implements OnInit {

    constructor(private incomeSvc: IncomeService, private alertSvc: AlertService) {
        super();
    }

    @ViewChild('agGridRevenue', { static: false })
    agGridRevenue: AgGridNg2;

    yearsOfIncomeToDisplay: number = 0;
    incomeRecordCount: number;       // tally after possible filtering.
    incomeRecordTotal: number | CurrencyPipe;
    initialRecordCount: number = 0;  // initial tally w/o filtering, e.g., page load.

    ngOnInit() {
        this.fetchRevenue();
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, checkboxSelection: true, width: 100, resizable: true,
            filter: "agTextColumnFilter",
            filterParams: {
                applyButton: true,
                clearButton: true,
                apply: true
            }
        },
        { headerName: "Div.Freq.", field: "dividendFreq", width: 97, resizable: true, filter: true, sortable: true,
            filterParams: {
                applyButton: true,
                clearButton: true,
                apply: true
            }
        },
        { headerName: "Account", field: "accountTypeDesc", width: 92, resizable: true, sortable: true, filter: "agTextColumnFilter",
            filterParams: {
                applyButton: true,
                clearButton: true,
                apply: true
            }
        },
        { headerName: "Date Recvd", field: "dateRecvd", width: 120, resizable: true, editable: true, sortable: true,
            filter: "agDateColumnFilter",
            filterParams: {
                comparator: function (filterLocalDateAtMidnight, cellValue) {
                    // Dates stored as mm/dd/yyyy - we create a Date object for comparison against the filter date.
                    var dateParts = cellValue.split("-");
                    var month = Number(dateParts[1]);
                    var day = Number(dateParts[2].substring(0, 2));
                    var year = Number(dateParts[0]);
                    var cellDate = new Date(year, month - 1, day);

                    // Compare date objects.
                    if (cellDate < filterLocalDateAtMidnight) {
                        return -1;
                    } else if (cellDate > filterLocalDateAtMidnight) {
                        return 1;
                    } else {
                        return 0;
                    }
                },
                browserDatePicker: true,
                applyButton: true,
                clearButton: true,
                apply: true
            },
            cellStyle: { textAlign: "right" },
            cellRenderer: (data) => { return data.value ? (new Date(data.value)).toLocaleDateString() : ''; }
        },
        { headerName: "Amount", field: "amountRecvd", width: 92, editable: true, sortable: true, resizable: true, cellStyle: { textAlign: "right" } },
        { headerName: "IncomeId", field: "incomeId", width: 50, hide: true }
    ];

    rowData: any;
    
    onFilterChanged() {

        let filterInstanceTicker = this.agGridRevenue.api.getFilterInstance("tickerSymbol");
        let filterInstanceAccount = this.agGridRevenue.api.getFilterInstance("accountTypeDesc");
        let filterInstanceDate = this.agGridRevenue.api.getFilterInstance("dateRecvd");
        let filterInstanceFreq = this.agGridRevenue.api.getFilterInstance("dividendFreq");

        // Trap for entered filtering.
        if (filterInstanceTicker.isFilterActive() || filterInstanceAccount.isFilterActive() || filterInstanceDate.isFilterActive()
                                                  || filterInstanceFreq.isFilterActive())
        {
            let filteredRowTotal = 0;
            let filteredRowCount = 0;
            this.agGridRevenue.api.forEachNodeAfterFilter((node) => {
                filteredRowCount++;
                filteredRowTotal = filteredRowTotal + parseFloat(node.data.amountRecvd);
            });

            this.incomeRecordCount = filteredRowCount;
            this.incomeRecordTotal = filteredRowTotal;
        } else {
            // Trap for 'Clear filter' applied.
            if (this.initialRecordCount != this.incomeRecordCount)
                this.fetchRevenue();
        }
    }


    fetchRevenue(): any {
        this.incomeSvc.GetRevenue(this.yearsOfIncomeToDisplay)
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(incomeResponse => {
                this.incomeRecordCount = incomeResponse.length;
                // Can't use 'reduce' on type string ?? / incomeResponse.reduce((sum, current) => sum + current.total, 0);
                this.incomeRecordTotal = this.calculateRecordSum(incomeResponse);
                this.agGridRevenue.api.setRowData(this.mapRevenueForGrid(incomeResponse));
                this.initialRecordCount = incomeResponse.length;
            },
            (apiError: HttpErrorResponse) => {
                if (apiError.error instanceof Error) {
                    // Client-side or network error encountered.
                    this.alertSvc.error("Error fetching revenue due to possible network error. Please try again later.");
                }
                else {
                    //API returns unsuccessful response status codes, e.g., 404, 500 etc.
                    let truncatedMsgLength = apiError.error.errorMsg.indexOf(":") - 7;
                    this.alertSvc.error("Error fetching revenue due to "
                        + "'" + apiError.error.errorMsg.substring(0, truncatedMsgLength) + "'."
                        + " Please try again later.");
                }
            })

    }

    onYearsToShow(years : number) {
        this.yearsOfIncomeToDisplay = years;
        this.fetchRevenue();
    }
    
    mapRevenueForGrid(recvdRevenue: any): Income[]
    {
        let mappedRevenue = new Array<Income>();
        for (let idx = 0; idx < recvdRevenue.length; idx++) {
            let modelRecord = new Income();

            modelRecord.tickerSymbol = recvdRevenue[idx].tickerSymbol;
            modelRecord.dividendFreq = recvdRevenue[idx].dividendFreq;
            modelRecord.accountTypeDesc = recvdRevenue[idx].accountTypeDesc;
            modelRecord.dateRecvd = recvdRevenue[idx].dateRecvd;
            modelRecord.amountRecvd = (recvdRevenue[idx].amountReceived).toFixed(2),  
            modelRecord.incomeId = recvdRevenue[idx].incomeId;

            mappedRevenue.push(modelRecord);
        }

        return mappedRevenue;
    }

    processEditedRevenue() {

        var selectedNodes = this.agGridRevenue.api.getSelectedNodes();
        var selectedIncomeData = selectedNodes.map(node => node.data);

        this.incomeSvc.UpdateIncome(selectedIncomeData)
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(updateResponse => {
                this.alertSvc.success("Successfully updated "
                    + updateResponse + " income record(s).");
                this.fetchRevenue();
            },
            (apiError: HttpErrorResponse) => {
                this.alertSvc.error("Error updating revenue due to "
                    + "'" + apiError.message + "'. Please try again later.");
            }
        )
    }

    calculateRecordSum(sourceData: any): number {
        let runningTotal: number = 0;
        for (let i = 0; i < sourceData.length; i++) {
            runningTotal = runningTotal + sourceData[i].amountReceived;
        }
        return runningTotal;
    }

}
