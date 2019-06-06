import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthenticationService } from '../shared/authentication.service';


@Injectable(
    { providedIn: 'root' }
)
export class JwtInterceptor implements HttpInterceptor {

    /*  Jason Web Token interceptor notes:
        Allows for intercepting HTTP requests before being sent to an API, and can be used to modify requests
        before they are sent, as well as handling/transforming responses. The interceptor can return a response directly when it's done,
        or pass control to the next handler in the chain by calling next.handle(request). The last handler in the chain is
        the built-in Angular 'HttpBackend', which sends the request via the browser to the API.

        * The Interceptor adds an HTTP Authorization header with a JWT to headers of all requests for authenticated users. *
    */

    constructor(private AuthenticationSvc: AuthenticationService) { }

    // Interface method called for all requests; adds authorization header with jwt token if available.
    intercept(currentReq: HttpRequest<any>, nextHandler: HttpHandler): Observable<HttpEvent<any>> {

        let currentInvestor = this.AuthenticationSvc.currentInvestorValue;
        if (currentInvestor && currentInvestor.token) {
            // Clones the request and adds the Authorization header along with the current investors' JWT token, using the
            // 'Bearer ' prefix to indicate that it's a bearer token (required for JWT). The request object is immutable, and
            // must be cloned to add the authorization header.
            currentReq = currentReq.clone({
                setHeaders: {Authorization: `Bearer ${currentInvestor.token}`}
            });
        }

        // Pass the request to the next handler in the chain.
        return nextHandler.handle(currentReq);
    }

}
