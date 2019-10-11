import { Injectable, ErrorHandler, Injector } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { LogException } from '../error-logging/logException';
import { AuthenticationService } from '../shared/authentication.service';
import { GlobalsService } from '../shared/globals.service';

@Injectable({
  providedIn: 'root'
})
export class ErrorService implements ErrorHandler {

    private baseUrl;

    constructor(private http: HttpClient, private injector: Injector, private authenticationSvc: AuthenticationService, globalsSvc: GlobalsService) {
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    logError(logInfo: LogException) {
        return this.http.post<any>(this.baseUrl + '/api/Logging/LogError', logInfo);
    }

    //  A global error handler trapping all *non-Http* related client-side exceptions, via implementation
    //  and registration (app.module) of Angulars' intrinsic error handler.
    handleError(error: any) {
        let idx = error.message.indexOf("not a function");

        // 10.11.19 - ** A temporary hack! Unresolved error upon selecting 'Getting Started' ->
        //              '_co.showGettingStarted is not a function' Why ?? **
        if (idx < 0) {
            const router = this.injector.get(Router);

            let exceptionInfo = new LogException();
            exceptionInfo.eventLevel = "Error";
            exceptionInfo.message = error.message == undefined ? error : error.message;
            exceptionInfo.eventLevel = "Error";
            // Only the first 125 chars are needed now.
            exceptionInfo.stackTrace = error.stack == undefined ? "undefined stack trace" : error.stack.substring(0, 125);
            exceptionInfo.source = router.url.substring(1);

            alert("Sorry!\nAn error has occurred." + "\n\nYou will automatically be logged out. Please try again later.");

            this.logError(exceptionInfo)
                .subscribe(result => {
                    console.log(result)
                },
                    (apiErr: HttpErrorResponse) => {
                        if (apiErr.error instanceof Error) {
                            // Client-side or network error encountered.
                            alert("Application or network error(s) encountered.");
                        }
                        else {
                            alert("Error logging error(s): due to : \n" + exceptionInfo.message + ".");
                        }
                    }
                );

            this.authenticationSvc.logout();
            router.navigate(['/']);
            window.location.reload();
        }
        
    }



}
