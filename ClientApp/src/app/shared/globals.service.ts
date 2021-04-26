import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class GlobalsService {

    /* Provides any application-wide global value(s) needed currently, or in the future. */

    constructor() { }

    private baseUrl: string = "https://localhost:44328";  // for development mode !

    public get pimsBaseUrl(): string {
        return this.baseUrl;
    }


    sortDivPayMonthsValue(recvdInfo: string): string {

        if (recvdInfo != "N/A") {
            let monthsStringArray = recvdInfo.split(',');
            let monthsNumberArray: number[] = new Array();
            for (let i = 0; i < monthsStringArray.length; i++) {
                monthsNumberArray[i] = parseInt(monthsStringArray[i]);
            }

            return monthsNumberArray.sort((a, b) => a - b).toString();
        } else {
            return "N/A";
        }
    }

}
