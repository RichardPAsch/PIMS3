import { Injectable } from '@angular/core';
import { HttpClient} from '@angular/common/http';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';



@Injectable({
  providedIn: 'root'
})
export class IncomeReceivablesService {

    currentInvestorId: string;
    baseUrl: string;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor')); 
        this.currentInvestorId = investor.id;
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    BuildIncomeReceivables(): Observable<string> {

        let webApiUri = this.baseUrl + "/api/Income/GetMissingIncomeSchedule/" + this.currentInvestorId;
        return this.http.get<string>(webApiUri);
    }

    UpdateIncomeReceivables(positionsToUpdate: any[]): Observable<any> {

        let webApiUri = this.baseUrl + "/api/Position/UpdatePymtDueFlags/";
        return this.http.put<any>(webApiUri, positionsToUpdate);

    }
}
