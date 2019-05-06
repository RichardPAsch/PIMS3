import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';


const baseUrl = "https://localhost:44328";

@Injectable({
  providedIn: 'root'
})

export class IncomeSummaryService {

    constructor(private http: HttpClient) { }

    BuildIncomeSummary(yearsBackDated: number): Observable<string> {

        let webApi = baseUrl + "/api/Income/" + yearsBackDated + "/" + true;
        return this.http.get<string>(webApi);
    }
}
