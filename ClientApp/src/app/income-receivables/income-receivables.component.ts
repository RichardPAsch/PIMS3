import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { IncomeReceivablesService } from '../income-receivables/income-receivables.service';
import { HttpErrorResponse } from '@angular/common/http';
import { Receivable } from '../income-receivables/receivable';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';

@Component({
  selector: 'app-income-receivables',
  templateUrl: './income-receivables.component.html',
  styleUrls: ['./income-receivables.component.css']
})
export class IncomeReceivablesComponent extends BaseUnsubscribeComponent implements OnInit {

    constructor(private receivablesSvc: IncomeReceivablesService, private alertSvc: AlertService) {
        super();
    }

    @ViewChild('agGridReceivables', { static: false })
    agGridReceivables: AgGridNg2;

    public currentMonth: string;
    public positionCount: number;

    ngOnInit() {
        this.currentMonth = this.GetMonthName();
        this.getReceivables();
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, filter: true, checkboxSelection: true, width: 100, resizable: true },
        { headerName: "Account", field: "accountTypeDesc", width: 100 },
        { headerName: "Div. Day", field: "dividendPayDay", width: 90, type: "numericColumn", sortable: true, filter: true },
        { headerName: "Div. Freq.", field: "dividendFreq", width: 100, sortable: true },
        { headerName: "PositionId.", field: "positionId", width: 100, hide: true },
    ];

    rowData: any; 
        

    public GetMonthName(): string {
        const monthNames = ["January", "February", "March", "April", "May", "June",
                            "July", "August", "September", "October", "November", "December"];

        const currentDate = new Date();
        return monthNames[currentDate.getMonth()];
    };

    public getReceivables() {
        this.receivablesSvc.BuildIncomeReceivables()
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(responsePositions => {
                this.agGridReceivables.api.setRowData(this.mapReceivablesForGrid(responsePositions));
                this.positionCount = responsePositions.length;
            },
                (apiErr: HttpErrorResponse) => {
                    if (apiErr.error instanceof Error) {
                        this.alertSvc.error("Error processing income schedule, due to possible network error. Please try again later.");
                    }
                    else {
                        //API returns unsuccessful response status codes, e.g., 404, 500 etc.
                        let truncatedMsgLength = apiErr.error.errorMsg.indexOf(":") - 7;
                        this.alertSvc.error("Error processing income schedule, due to "
                            + "'" + apiErr.error.errorMsg.substring(0, truncatedMsgLength) + "'."
                            + " Please try again later.");
                    }
                }
            )
    }

    mapReceivablesForGrid(recvdReceivables: any): Receivable[] {

        let mappedReceivables = new Array<Receivable>();
        for (let idx = 0; idx < recvdReceivables.length; idx++) {
            let modelRecord = new Receivable();
            modelRecord.positionId = recvdReceivables[idx].positionId;
            modelRecord.tickerSymbol = recvdReceivables[idx].tickerSymbol;
            modelRecord.accountTypeDesc = recvdReceivables[idx].accountTypeDesc;
            modelRecord.dividendPayDay = recvdReceivables[idx].dividendPayDay;
            modelRecord.dividendFreq = recvdReceivables[idx].dividendFreq;

            mappedReceivables.push(modelRecord);
        }

        return mappedReceivables;
    }


    public processPositionUpdates() {

        var selectedNodes = this.agGridReceivables.api.getSelectedNodes();
        var selectedPositionData = selectedNodes.map(node => node.data.positionId);

        this.receivablesSvc.UpdateIncomeReceivables(selectedPositionData)
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(updateResponse => {
                if (updateResponse) {
                    this.alertSvc.success("Successfully marked : " + selectedPositionData.length + " position(s) as paid.");
                    // Refresh.
                    this.getReceivables();
                }
                else
                    this.alertSvc.warn("Unable to update position(s) with payment received, please check position(s) data.");
            },
                (apiError: HttpErrorResponse) => {
                    if (apiError.error instanceof Error)
                        this.alertSvc.error("Error processing position update(s), due to possible network error. Please try again later.");
                    else {
                        this.alertSvc.error("Error processing position update(s), due to "
                            + "'" + apiError.message + "'."
                            + " Please try again later.");
                    }
                }
            )

    }


}
