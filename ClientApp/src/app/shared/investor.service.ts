import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Investor } from '../shared/investor';
import { Guid } from "guid-typescript";

const baseUrl = "https://localhost:44328";

/*  Notes:
    This investor service supplies a standard CRUD API for managing investors, and acts as the interface
    between this Angular front-end & our back-end .Net Core api.
*/

@Injectable({
  providedIn: 'root'
})

export class InvestorService {

    constructor(private http: HttpClient) { }

    getAll() {

        let webApiUri = baseUrl + "/api/Investor";
        return this.http.get<Investor[]>(webApiUri);

        // investigate: config.apiUrl ?
        //...get<User[]>(`${config.apiUrl}/users`);
    }

    register(investor: Investor) {
        investor.investorId = Guid.create().toString();
        let webApiUri = baseUrl + "/api/Investor/Register";
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
