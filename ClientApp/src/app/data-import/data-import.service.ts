import { Injectable, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable()

export class DataImportService {

    constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {

    }


    postRevenueData() {


    }

}
