import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'income-summary-data',
  templateUrl: './income-summary.component.html'
})

export class IncomeSummaryComponent {
  private currentDate: Date = new Date();
  currentYear: number = this.currentDate.getFullYear();
  public ytdIncomeSummary: IncomeSummary[];


  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    http.get<IncomeSummary[]>(baseUrl + 'api/Income/GetRevenueSummary')
      .subscribe(result => { this.ytdIncomeSummary = result; },
        error => console.error("IncomeSummaryComponent ctor error: ", error));
  }
}


interface IncomeSummary {
  MonthRecvd: number;
  AmountRecvd: number | CurrencyPipe;
  YtdAverage: number | CurrencyPipe;
  Rolling3MonthAverage: number | CurrencyPipe; 
}
