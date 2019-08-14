import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Investor } from '../shared/investor';
import { Guid } from "guid-typescript";
import { GlobalsService } from '../shared/globals.service';


/*  Notes:
    This investor service supplies a standard CRUD API for managing investors, and acts as the interface
    between this Angular front-end & our back-end .Net Core api.
*/

@Injectable({
  providedIn: 'root'
})

export class InvestorService {
    private baseUrl;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    getAll() {

        let webApiUri = this.baseUrl + "/api/Investor";
        return this.http.get<Investor[]>(webApiUri);

        // investigate: config.apiUrl ?
        //...get<User[]>(`${config.apiUrl}/users`);
    }

    updateLogin(login: string, oldPwrd: string, newPwrd: string) {
        let webApiUri = this.baseUrl + "/api/Investor/" + login + "/" + oldPwrd + "/" + newPwrd;
        return this.http.get<Investor>(webApiUri);
    }

    register(investor: Investor) {
        investor.investorId = Guid.create().toString();
        let webApiUri = this.baseUrl + "/api/Investor/Register";
        return this.http.post<Investor>(webApiUri, investor);
    }

    /* ====== Fiddler test url & data: ======
     
        https://localhost:44328/api/Investor/Register
        {
          "investorId": "34022871-763f-49da-a72a-c2689ddb63ce",
          "loginname": "js@yahoo.com",
          "password": "password5",
          "firstName": "joe",
          "lastName": "smith",
          "token": ""
        }

    */ 


}
