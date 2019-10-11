import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthenticationService } from '../shared/authentication.service';


@Injectable(
    { providedIn: 'root' }
)
export class HttpErrorInterceptor implements HttpInterceptor {

    /*
        This Error Interceptor handles failed HTTP requests, e.g., 401 Unauthorized. If a 401, the user is automatically
        logged out, otherwise the error message is extracted from the HTTP error response and then thrown, so it can be caught
        and displayed by the component that initiated the request.
    */

    constructor(private authenticationSvc: AuthenticationService) { }

    intercept(request: HttpRequest<any>, nextHdlr: HttpHandler): Observable<HttpEvent<any>> {

        // We'll pass the request to the next HttpHandler in the chain, handling errors by piping the observable response through the 'catchError' operator.
        // If a 401, we'll automatically log the investor out, and reload the application, which will redirect to the login page.
        // The error message is extracted from either 1) the error response object, or 2) defaults to the response status text, if there is no error message.
        // We'll throw an error with the error, so it can be handled by the calling component.
        return nextHdlr.handle(request)
            .pipe(catchError(errorResponse => {
                if (errorResponse.status === 401) {
                    this.authenticationSvc.logout();
                    location.reload(true);
                }

                let error = errorResponse.message || errorResponse.statusText;
                return throwError(error);
            }))

        

      // see: https://jasonwatmore.com/post/2019/05/17/angular-7-tutorial-part-4-login-form-authentication-service-route-guard#create-jwt-interceptor
    }
}
