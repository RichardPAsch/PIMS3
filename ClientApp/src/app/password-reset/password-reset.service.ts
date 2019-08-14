import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GlobalsService } from '../shared/globals.service';
import { HttpClient } from '@angular/common/http';


@Injectable({
  providedIn: 'root'
})
export class PasswordResetService {

    baseUrl: string;

    constructor(private http: HttpClient, globalsSvc: GlobalsService) {
        let investor = JSON.parse(sessionStorage.getItem('currentInvestor'));
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

   
    
}
