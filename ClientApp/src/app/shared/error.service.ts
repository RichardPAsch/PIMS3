import { Injectable, ErrorHandler, Injector } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { LogException } from '../error-logging/logException';
import { AuthenticationService } from '../shared/authentication.service';
import { GlobalsService } from '../shared/globals.service';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Injectable({
  providedIn: 'root'
})
export class ErrorService extends BaseUnsubscribeComponent implements ErrorHandler {

    private baseUrl;

    constructor(private http: HttpClient, private injector: Injector, private authenticationSvc: AuthenticationService,
        globalsSvc: GlobalsService, private alertSvc: AlertService) {
        super();
        this.baseUrl = globalsSvc.pimsBaseUrl;
    }

    logError(logInfo: LogException) {
        return this.http.post<any>(this.baseUrl + '/api/Logging/LogError', logInfo);
    }

    //  A GLOBAL error handler trapping all *non-Http* related client-side exceptions, via implementation
    //  and registration (app.module) of Angulars' intrinsic error handler.
    handleError(error: any) {
        let idx = error.message.indexOf("not a function");

        // ** A temporary hack! Unresolved error upon selecting 'Getting Started' menu ->  '_co.showGettingStarted is not a function' Why ?? **
        if (idx < 0) {
            const router = this.injector.get(Router);

            // Refactor ? HttpErrorInterceptor duplicate ?
            let exceptionInfo = new LogException();
            exceptionInfo.eventLevel = "Error";
            exceptionInfo.message = error.message == undefined ? error : error.message;
            exceptionInfo.eventLevel = "Error";
            // Only the first 125 chars are needed now.
            exceptionInfo.stackTrace = error.stack == undefined ? "undefined stack trace" : error.stack.substring(0, 125);
            exceptionInfo.source = router.url.substring(1);
            exceptionInfo.investorLogin = this.authenticationSvc.investorLoginEMailName.value;

            this.alertSvc.warn("Sorry! An error has occurred. You will be automatically logged out. Please try again later.");

            this.logError(exceptionInfo)
                .pipe(takeUntil(this.getUnsubscribe()))
                .subscribe(result => {
                    console.log(result)
                },
                    (apiErr: HttpErrorResponse) => {
                        if (apiErr.error instanceof Error) {
                            // Client-side or network error encountered.
                            this.alertSvc.error("An application or network error(s) has  occurred. Please retry later.");
                        }
                        else {
                            this.alertSvc.error("Error logging error(s): due to : '" + exceptionInfo.message + "'.");
                        }
                    }
                );

            this.authenticationSvc.logout();
            router.navigate(['/']);
            window.location.reload();
        }
        
    }



}
