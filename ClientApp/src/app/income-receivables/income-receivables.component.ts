import { Component, OnInit } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';

@Component({
  selector: 'app-income-receivables',
  templateUrl: './income-receivables.component.html',
  styleUrls: ['./income-receivables.component.css']
})
export class IncomeReceivablesComponent implements OnInit {

    constructor() {
    }

    public currentMonth: string;

    ngOnInit() {
        this.currentMonth = this.GetMonthName();
    }

    columnDefs = [
        { headerName: "Ticker", field: "TickerSymbol", sortable: true, filter: true, checkboxSelection: true, width: 100 },
        { headerName: "Account", field: "AccountTypeDesc", width: 100 },
        { headerName: "Div. Day", field: "DividendPayDay", width: 100, type: "numericColumn" },
        { headerName: "Div. Freq.", field: "DividendFreq", width: 100 },
    ];

    rowData: any = [
        {TickerSymbol: "AAPL", AccountTypeDesc: "CMA", DividendPayDay: "15", DividendFreq: "M"}
    ];
        

    public GetMonthName(): string {
        const monthNames = ["January", "February", "March", "April", "May", "June",
                            "July", "August", "September", "October", "November", "December"];

        const currentDate = new Date();
        return monthNames[currentDate.getMonth()];
    };

    public getReceivables() {
        alert("Ok");
    }


}
