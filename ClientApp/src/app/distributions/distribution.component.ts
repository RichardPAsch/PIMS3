import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { HttpErrorResponse } from '@angular/common/http';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';

@Component({
    selector: 'app-distributions',
    templateUrl: './distributions.component.html',
    styleUrls: ['./distributions.component.css']
})
export class DistributionsComponent extends BaseUnsubscribeComponent implements OnInit {

    constructor() {
        super();
    }

    @ViewChild('agGridDistributions', { static: false })
    agGridDistributions: AgGridNg2;

    ngOnInit() {

    }
}
