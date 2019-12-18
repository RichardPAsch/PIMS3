import { Injectable, Injector } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';
import { LogException } from '../error-logging/logException';
import { ErrorService } from '../shared/error.service';
import { Router } from '@angular/router';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Injectable(
    { providedIn: 'root' }
)
export class HttpErrorInterceptor extends BaseUnsubscribeComponent implements HttpInterceptor {

    /*
        This Error Interceptor handles failed HTTP requests, e.g., 401 Unauthorized. If a 401, the user is automatically
        logged out, otherwise the error message is extracted from the HTTP error response and then thrown, so it can be caught
        and displayed by the component that initiated the request.
    */

    constructor(private authenticationSvc: AuthenticationService, private ErrSvc: ErrorService, private injector: Injector, private alertSvc: AlertService) {
        super();
    }

    intercept(request: HttpRequest<any>, nextHdlr: HttpHandler): Observable<HttpEvent<any>> {

        // We'll pass the request to the next HttpHandler in the chain, handling errors by piping the observable response through the 'catchError' operator.
        // If a 401, we'll reload the application, which will redirect to the login page.
        // The error message is extracted from either 1) the error response object, or 2) defaults to the response status text, if there is no error message.
        // We'll throw an error with the error, so it can be handled by the calling component.
        return nextHdlr.handle(request)
            .pipe(catchError(errorResponse => {

                if (errorResponse.status === 400) {
                    // May result from data access error, or an anticipated response/status when generating a custom profile e.g., as in
                    // 'profile.component.getProfile()'.
                    return throwError("No profile info found for submitted ticker.");
                }

                if (errorResponse.status === 401) {
                    this.authenticationSvc.logout();
                    location.reload(true);

                    let error = errorResponse.message || errorResponse.statusText;
                    return throwError(error);
                }

                if (errorResponse.status === 404 || errorResponse.status === 500) {
                    const router = this.injector.get(Router);

                    // e.g. bad url (not found) or no service connectivity; 
                    let exceptionInfo = new LogException();
                    exceptionInfo.eventLevel = "Error";
                    exceptionInfo.message = errorResponse.message == undefined ? errorResponse : errorResponse.message;
                    exceptionInfo.eventLevel = "Error";
                    exceptionInfo.status = errorResponse.status == undefined ? "undefined status" : errorResponse.status;
                    // Only the first 125 chars are needed now.
                    exceptionInfo.stackTrace = errorResponse.stack == undefined ? "undefined stack trace" : errorResponse.stack.substring(0, 125);
                    exceptionInfo.source = errorResponse.url;
                    exceptionInfo.investorLogin = this.authenticationSvc == undefined ? "Unsuccessful authentication attempt" : this.authenticationSvc.investorLoginEMailName.value;

                    this.ErrSvc.logError(exceptionInfo)
                        .pipe(takeUntil(this.getUnsubscribe()))
                        .subscribe(() => 
                        {
                            // 400 status (authentication error) caught & broadcast via 'home.component.onSubmit().'
                            if (errorResponse.status == 404) {
                                this.alertSvc.warn("Unable to authenticate '" + this.authenticationSvc.investorLoginEMailName.value + "'.  Please try again later.");
                            }
                        
                            this.authenticationSvc.logout();
                            router.navigate(['/']);
                            window.location.reload();
                        });

                    // The callback for catchError() requires returning a stream of some sort, (e.g., Promise, Array, Observable, etc.) to avoid a TypeError.
                    return new Array();
                }

            }))

    }
}
