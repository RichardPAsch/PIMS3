import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { IncomeService } from '../income/income.service';
import { Income } from '../income/income';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-income',
  templateUrl: './income.component.html',
  styleUrls: ['./income.component.css']
})
export class IncomeComponent implements OnInit {

    constructor(private incomeSvc: IncomeService) { }

    @ViewChild('agGridRevenue')
    agGridRevenue: AgGridNg2;

    ngOnInit() {
        this.fetchRevenue();
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, filter: true, checkboxSelection: true, width: 100, resizable: true },
        { headerName: "Div.Freq.", field: "dividendFreq", width: 97, resizable: true, filter: true, sortable: true },
        { headerName: "Account", field: "accountTypeDesc", width: 92, resizable: true, filter: true, sortable: true },
        { headerName: "Date Recvd", field: "dateRecvd", width: 120, resizable: true, editable: true, sortable: true },
        { headerName: "Amount", field: "amountRecvd", width: 92,
            editable: true, resizable: true,
            cellStyle: { textAlign: "right" }
        },
        { headerName: "IncomeId", field: "incomeId", width: 50, hide: true }
    ];

    rowData: any;

    //rowData = [
    //    { tickerSymbol: 'AAPL', dividendFreq: 'M', accountTypeDesc: 'CMA', dateRecvd: '3/6/2019', amtRecvd: '125.68', incomeId: 'abc722yt' },
    //    { tickerSymbol: 'MSFT', dividendFreq: 'Q', accountTypeDesc: 'IRA', dateRecvd: '3/1/2019', amtRecvd: '287.11', incomeId: 'mks8sw6d' }
    //];

    fetchRevenue(): any {
        this.incomeSvc.GetRevenue(0)
            .retry(2)
            .subscribe(incomeResponse => {
                this.agGridRevenue.api.setRowData(this.mapRevenueForGrid(incomeResponse));
            },
            (apiError: HttpErrorResponse) => {
                if (apiError.error instanceof Error) {
                    // Client-side or network error encountered.
                    alert("Error processing income  : \network or application error. Please try later.");
                }
                else {
                    //API returns unsuccessful response status codes, e.g., 404, 500 etc.
                    let truncatedMsgLength = apiError.error.errorMsg.indexOf(":") - 7;
                    alert("Error processing income: due to : \n" + apiError.error.errorMsg.substring(0, truncatedMsgLength) + ".");
                }
            });

    }


    mapRevenueForGrid(recvdRevenue: any): Income[] {

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

}