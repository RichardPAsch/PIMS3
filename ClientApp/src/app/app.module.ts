import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { CounterComponent } from './counter/counter.component';
import { FetchDataComponent } from './fetch-data/fetch-data.component';
import { IncomeSummaryComponent } from './income-summary/income-summary.component';
import { DataImportService } from './data-import/data-import.service';
import { DataImportComponent } from './data-import/data-import.component';
import { ReactiveFormsModule } from '@angular/forms';
import { MessageService } from './message.service';
import { AgGridModule } from 'ag-grid-angular';
import { IncomeProjectionsComponent } from './income-projections/income-projections.component';
import { ProfileService } from './shared/profile.service';

/* Notes:
 *  Medium to large apps should have one or more FEATURE modules. ngModule may have one
 *  or more child modules. Do create an NgModule for each feature area.
 * */

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    CounterComponent,
    FetchDataComponent,
    IncomeSummaryComponent,
    DataImportComponent,
    IncomeProjectionsComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    AgGridModule.withComponents([]),
    HttpClientModule,
    FormsModule,
    ReactiveFormsModule,
      RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full' },
      { path: 'counter', component: CounterComponent },
      { path: 'fetch-data', component: FetchDataComponent },
      { path: 'income-projections', component: IncomeProjectionsComponent },
      { path: 'income-summary', component: IncomeSummaryComponent },
      { path: 'data-import', component: DataImportComponent },
    ])
  ],
  providers: [DataImportService, MessageService, ProfileService], // Creators of services that NgModule contributes to the global collection of services;
    // they become accessible in all parts of the app. (You can also specify providers at the component level,
    // which is often preferred.)

  bootstrap: [AppComponent] // The main application view, called the root component, which HOSTS all other app views.
                            // Only NgModule should set the bootstrap property.
})
export class AppModule { }
