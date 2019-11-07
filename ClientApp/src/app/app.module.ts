import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { IncomeSummaryComponent } from './income-summary/income-summary.component';
import { DataImportService } from './data-import/data-import.service';
import { DataImportComponent } from './data-import/data-import.component';
import { ReactiveFormsModule } from '@angular/forms';
import { AgGridModule } from 'ag-grid-angular';
import { IncomeProjectionsComponent } from './income-projections/income-projections.component';
import { ProfileService } from './shared/profile.service';
import { IncomeReceivablesComponent } from './income-receivables/income-receivables.component';
import { PositionsComponent } from './positions/positions.component';
import { IncomeComponent } from './income/income.component';
import { ProfileComponent } from './profile/profile.component';
import { RegistrationComponent } from './registration/registration.component';
import { AuthenticationService } from '../app/shared/authentication.service';
import { InvestorService } from '../app/shared/investor.service';
import { AuthorizationGuard } from '../app/authorization/authorization.guard';
import { JwtInterceptor } from '../app/shared/jwt.interceptor';
import { HttpErrorInterceptor } from '../app/shared/http.error.interceptor';
import { GlobalsService } from '../app/shared/globals.service';
import { GettingStartedComponent } from './getting-started/getting-started.component';
import { PasswordResetComponent } from './password-reset/password-reset.component';
import { ErrorService } from '../app/shared/error.service';
import { ErrorHandler } from '@angular/core';
import { AlertComponent } from './alert/alert.component';
import { BaseUnsubscribeComponent } from './base-unsubscribe/base-unsubscribe.component';


/* Notes:
 *  Medium to large apps should have one or more FEATURE modules. ngModule may have one
 *  or more child modules. Do create an NgModule for each feature area.
 *  Routing: each route contains a path and associated component.
 *  == Routes are secured by: == 
 *  1) passing the 'AuthorizationGuard' to the canActivate property of the route, and
 *  2) annotating controllers with '[Authorize]'. **
 *  see: https://jasonwatmore.com/post/2018/11/22/angular-7-role-based-authorization-tutorial-with-example#app-routing-ts
 */

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    IncomeSummaryComponent,
    DataImportComponent,
    IncomeProjectionsComponent,
    IncomeReceivablesComponent,
    PositionsComponent,
    IncomeComponent,
    ProfileComponent,
    RegistrationComponent,
    GettingStartedComponent,
    PasswordResetComponent,
    AlertComponent,
    BaseUnsubscribeComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    AgGridModule.withComponents([]),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
      RouterModule.forRoot([
        // Access to a route is controlled via adding 'AuthorizationGuard' to canActivate property array. The route guards
        // in the array are run by Angular to decide if the route can be "activated". If all of the route
        // guards return true, then navigation is allowed to continue, otherwise, navigation is cancelled.
        { path: '', component: HomeComponent, pathMatch: 'full' }, // default Login - Home page.
        { path: '', component: HomeComponent, pathMatch: 'full' }, // log out
        { path: 'income-projections', component: IncomeProjectionsComponent, canActivate: [AuthorizationGuard] },
        { path: 'registration', component: RegistrationComponent },
        { path: 'income-summary', component: IncomeSummaryComponent, canActivate: [AuthorizationGuard] },
        { path: 'income-receivables', component: IncomeReceivablesComponent, canActivate: [AuthorizationGuard] },  // 'income due'
        { path: 'income', component: IncomeComponent, canActivate: [AuthorizationGuard] },                         // 'income recorded'
        { path: 'data-import', component: DataImportComponent, canActivate: [AuthorizationGuard] },
        { path: 'positions', component: PositionsComponent, canActivate: [AuthorizationGuard] },
        { path: 'profile', component: ProfileComponent, canActivate: [AuthorizationGuard] },
        { path: 'getting-started', component: GettingStartedComponent },  // informational only
        { path: 'password-reset', component: PasswordResetComponent },

         // Otherwise redirect to home
         { path: '**', redirectTo: '' }
        ])
    ],

    /* ===== Notes:
     Creators of services, which NgModule contributes to the global collection of services, provide functionality that is accessible to
     all parts of PIMS.
     'Providers' enable Angular Dependency Injection (DI) to get value(s) for dependency(ies).
     The JWT and Error interceptors hook into the HTTP request pipeline via the Angular built-in injection token HTTP_INTERCEPTORS.
     Angular has several built in injection tokens that enable hooking into different parts of the framework and application lifecycle
     events. The 'multi: true' argument option tells Angular to ADD the provider to the collection of HTTP_INTERCEPTORS, rather than
     replacing the collection with a single provider. This allows adding multiple HTTP interceptors to the request pipeline for handling different tasks.

     Providing services at a component level leads to multiple service instances ( one per component ), therefore, we're declaring
     them at the module level.
    */
    providers: [DataImportService, ProfileService, 
                AuthenticationService, InvestorService, GlobalsService,
        { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
        { provide: ErrorHandler, useClass: ErrorService },
        { provide: HTTP_INTERCEPTORS, useClass: HttpErrorInterceptor, multi: true }
    ], 

    bootstrap: [AppComponent] // The main application view, called the root component, which HOSTS all other app views.
                              // Only NgModule should set the bootstrap property.
})
export class AppModule { }
