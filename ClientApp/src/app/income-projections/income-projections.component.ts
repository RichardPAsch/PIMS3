import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { ProfileService } from '../shared/profile.service';
import { Profile } from '../income-projections/profile';
//import { map } from 'rxjs/operators';

@Component({
  selector: 'app-income-projections',
  templateUrl: './income-projections.component.html',
  styleUrls: ['./income-projections.component.css']
})
export class IncomeProjectionsComponent implements OnInit {

    constructor(private profileSvc : ProfileService) { }

    // Decorator references the child component inside the template.
    @ViewChild('agGrid')
    agGrid: AgGridNg2;


    columnDefs = [
        { headerName: "Ticker", field: "ticker", sortable: true, filter: true, checkboxSelection: true, width: 100 },
        { headerName: "Capital ($)", field: "capital", width: 110, type: "numericColumn" },
        { headerName: "Price( $)", field: "unitPrice", width: 100, type: "numericColumn" },
        {
            headerName: "Dividend",
            groupId: "dividendGroup",
            children: [
                { headerName: "Rate ($)", field: "dividendRate", width: 90, type: "numericColumn" },
                { headerName: "Yield (%)", field: "dividendYield", width: 100, type: "numericColumn" }
            ]
        },
        { headerName: "Income ($)", field: "projectedMonthlyIncome", width: 135, editable: false, type: "numericColumn" },
    ];

    rowData: any;

    defaultColDef = {
        resizable: true,
        sortable: true,
        filter: true,
        editable: true
    };

    getProjections() {
        let profiles = new Array<Profile>();
        var selectedNodes = this.agGrid.api.getSelectedNodes();
        var selectedData = selectedNodes.map(node => node.data);
        //var selectedRowRepresentation = selectedData.map(selRow => selRow.ticker + '/' + selRow.capital).join(' , ');

        for (let gridRow = 0; gridRow < selectedData.length; gridRow++) {
            this.profileSvc.getProfileData(selectedData[gridRow].ticker) //, selectedData[gridRow].capital)
                .subscribe(responseProfile => {
                    profiles.push(this.initializeGridModel(responseProfile, selectedData[gridRow].capital));
                    this.agGrid.api.setRowData(profiles);
                });
        }
    }

    ngOnInit() {
        this.rowData = [
            { ticker: "XYZ", capital: 0 },
            { ticker: "", capital: 0 },
            { ticker: "", capital: 0 },
            { ticker: "", capital: 0 },
            { ticker: "", capital: 0 }
        ];
    }


    initializeGridModel(recvdProfile: any, capitalToInvest: number): Profile {

        // Calculating projected income (bus. logic) done here, as projectedIncome is not a 'Profile.cs' attribute.
        let profileRecord = new Profile();
        profileRecord.ticker = recvdProfile.tickerSymbol;
        profileRecord.capital = capitalToInvest;
        profileRecord.unitPrice = recvdProfile.unitPrice;
        profileRecord.dividendRate = recvdProfile.dividendRate;
        profileRecord.dividendYield = recvdProfile.dividendYield;
        profileRecord.projectedMonthlyIncome = this.calculateProjectedMonthlyIncome(profileRecord);

        return profileRecord;
    };

    calculateProjectedMonthlyIncome(incompleteProfile: Profile): number {
        // ** Received 'divCash' (aka dividendRate) amount(s), from web service API, is/are always given as MONTHLY rates,
        //    despite distribution frequencies, and are in accordance/verified with the 'last announced dividend' shown
        //    via 'Seeking Alpha'. **
        let calculatedIncome = ((incompleteProfile.capital / incompleteProfile.unitPrice) * incompleteProfile.dividendRate);
        return (+calculatedIncome.toFixed(2));
    }


}
