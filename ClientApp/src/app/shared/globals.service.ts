import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class GlobalsService {
    /* Provides any application-wide global value(s) that may currently,
       or in the future, be necessary.
    */

    constructor() { }

    private baseUrl: string = "https://localhost:44328";  // for development mode !

    public get pimsBaseUrl(): string {
        return this.baseUrl;
    }

}
