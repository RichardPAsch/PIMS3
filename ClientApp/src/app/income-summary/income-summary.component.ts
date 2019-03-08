import { Component, Inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import 'rxjs/add/operator/retry'; 

@Component({
  selector: 'income-summary-data',
  templateUrl: './income-summary.component.html'
})

export class IncomeSummaryComponent {
    private currentDate: Date = new Date();
    private currentYear: number = this.currentDate.getFullYear();
    public currentYearHeading: number = this.currentYear;
    public ytdIncomeSummary: IncomeSummary[];
    private apiUrl: string;
    private httpCilentReference: HttpClient;
    public incomeSummaryTotal: any;


    constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string)
    {
        // 'true' required param used only for WebApi :  GetRevenueSummary() vs. GetRevenue().
        this.apiUrl = baseUrl + 'api/Income/' + '0' + '/true';
        this.httpCilentReference = http;
        this.processIncomeSummary();
    }

    onYearSelected(yearsBackDated: number) {
        this.currentYearHeading = this.currentYear - yearsBackDated;
        this.apiUrl = this.apiUrl.substr(0, this.apiUrl.lastIndexOf("/") - 2) + "/" + yearsBackDated.toString() + "/" + true;
        this.processIncomeSummary();
    }

    processIncomeSummary() {
        // TODO: refactor into seperate service component.
        this.httpCilentReference.get<any[]>(this.apiUrl)
            .retry(2)
            .subscribe(result =>
            {
                this.ytdIncomeSummary = result;
                let runningTotal: any = 0.0;
                let ctr: any = 0;

                for (ctr in result) {
                    runningTotal += parseFloat(result[ctr].amountRecvd);
                }
                this.incomeSummaryTotal = runningTotal;
            },
            (error: HttpErrorResponse) => alert("Error retreiving income data for year: \n"
                                                    + this.currentYearHeading
                                                    + "\ndue to " + error.message)
            );
    }

}


interface IncomeSummary {
  MonthRecvd: number;
  AmountRecvd: number;
  YtdAverage: number | CurrencyPipe;
  Rolling3MonthAverage: number | CurrencyPipe; 
}
