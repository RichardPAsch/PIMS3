import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { Position } from '../positions/position';
import { PositionsService } from '../positions/positions.service';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Component({
  selector: 'app-positions',
  templateUrl: './positions.component.html',
  styleUrls: ['./positions.component.css']
})
export class PositionsComponent extends BaseUnsubscribeComponent implements OnInit {

    constructor(private positionSvc: PositionsService, private alertSvc: AlertService) {
        super();
    }

    positionCount: number;
    includeInactive: boolean = false;
    assetClassDropDownCodes = new Array<string>();
    acctTypeDropDownCodes = new Array<string>();

    @ViewChild('agGridPositions', {static: false})
    agGridPositions: AgGridNg2;

    ngOnInit() {
        this.fetchPositions(false);
        this.processAssetClassDescriptions(true);
        this.processAccountTypesInfo();
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, filter: true, checkboxSelection: true, width: 98, resizable: true, editable: true },
        { headerName: "Description", field: "tickerDescription", width: 160, resizable: true },
        { headerName: "Account", field: "accountTypeDesc", width: 92, editable: true, resizable: true, filter: true, sortable: true,
            cellEditor: "agPopupSelectCellEditor",
            cellEditorParams: {
                values: this.acctTypeDropDownCodes
            }
        },
        { headerName: "Status", field: "status", width: 77, sortable: true, resizable: true, editable: true,
            cellEditor: "agPopupSelectCellEditor",
            cellEditorParams: {
                values: ["A", "I"]
            }
        },
        { headerName: "Asset Class", field: "assetClass", width: 117, sortable: true, editable: true, filter: true,
            filterParams: { applyButton: true },
            resizable: true,
            cellEditor: "agPopupSelectCellEditor",
            cellEditorParams: {
                values: this.assetClassDropDownCodes
            }
        },
        { headerName: "Unpaid",
            field: "pymtDue",
            resizable: true,
            width: 85,
            sortable: true,
            editable: true,
            cellEditor: "agPopupSelectCellEditor",
            cellEditorParams: {
                values: [true, false]
            }
        },
        { headerName: "PositionId", field: "positionId", width: 50, hide: true },
    ];

    rowData: any;
    
    fetchPositions(doWeIncludeInactiveRecs: boolean): void {

        this.positionSvc.BuildPositions(doWeIncludeInactiveRecs)
            .retry(1)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(positionsResponse => {
                this.agGridPositions.api.setRowData(this.mapResponseToGrid(positionsResponse));
                this.positionCount = positionsResponse.length;
            });
    }
    
    mapResponseToGrid(responseData: any): Position[] {

        //let mappedPositions: Position[];
        let mappedPositions = new Array<Position>();

        for (var i = 0; i < responseData.length; i++) {
            let position: Position = new Position();

            position.tickerSymbol = responseData[i].tickerSymbol;
            if (i >= 1) {
                position.tickerDescription = responseData[i - 1].tickerDescription == responseData[i].tickerDescription
                    ? "------"
                    : responseData[i].tickerDescription;
            } else {
                position.tickerDescription = responseData[i].tickerDescription;
            }
            position.accountTypeDesc = responseData[i].account;
            position.assetClass = responseData[i].assetClass;
            position.status = responseData[i].status;
            position.pymtDue = responseData[i].pymtDue;
            // using links reference: https://next.plnkr.co/edit/AcfU8spNR4C5gwWu4vtw?utm_source=legacy&utm_medium=worker&utm_campaign=next&preview
            position.positionId = responseData[i].positionId;

            mappedPositions.push(position);
        }

        return mappedPositions;
    }
    
    processEditedPositions() {

        var selectedNodes = this.agGridPositions.api.getSelectedNodes();
        var selectedPositionEdits = selectedNodes.map(node => node.data);
        let editedPositions = new Array<Position>();

        if (selectedPositionEdits.length == 0) {
            this.alertSvc.info("Unable to process; please select 1 or more Positions for updating.");
            return;
        }

        for (let pos = 0; pos < selectedPositionEdits.length; pos++) {
                        
            let editedPosition = new Position();

            // Capture only necessary column edit(s) for each Id.
            editedPosition.positionId = selectedPositionEdits[pos].positionId;
            editedPosition.status = selectedPositionEdits[pos].status;
            editedPosition.pymtDue = selectedPositionEdits[pos].pymtDue;
            editedPosition.assetClass = selectedPositionEdits[pos].assetClass;
            editedPosition.accountTypeDesc = selectedPositionEdits[pos].accountTypeDesc;
            editedPosition.tickerSymbol = selectedPositionEdits[pos].tickerSymbol;

            editedPositions.push(editedPosition);
        }

        this.positionSvc.UpdateEditedPositions(editedPositions)
            .retry(2)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(updateResponseCount => {
                if (Number(updateResponseCount) > 0) {
                    this.alertSvc.success("Successfully updated " + updateResponseCount + " Position(s).");
                    this.fetchPositions(this.includeInactive);
                }
                else {
                    this.alertSvc.error("Unable to process Position update(s) at this time. Possible connectivity issue(s). Please try again later.");
                }
            }); // end subscribe()

    }
    
    processInactiveData(includeInactive: any) {
        this.fetchPositions(includeInactive);
    }

    processAssetClassDescriptions(initializeDropDown: boolean): void {

        if (initializeDropDown) {
            this.positionSvc.GetAssetClassDescAndCode()
                .retry(1)
                .pipe(takeUntil(this.getUnsubscribe()))
                .subscribe(assetClassesArr => {
                    sessionStorage.setItem('descriptions', JSON.stringify(assetClassesArr));
                    this.buildAssetClassInfo(assetClassesArr, true);
                });
        } else {
            // Show code-descriptions lookup.
            let desc = JSON.parse(sessionStorage.getItem('descriptions'));
            alert(this.buildAssetClassDescriptions(desc));
        }
        
    }
    
    buildAssetClassInfo(assetClassesData: any, forDropDownUse: boolean): string {

        if (!forDropDownUse) {
            let descAndCodes = "";
            for (let i = 1; i < assetClassesData.length; i++) {
                descAndCodes += i + ". " + assetClassesData[i].code + " : " + assetClassesData[i].description + "\n";
            }
            return descAndCodes;
        } else {
            for (let i = 1; i < assetClassesData.length; i++) {
                this.assetClassDropDownCodes.push(assetClassesData[i].code);
            }
            return;
        }
    }
    
    processAccountTypesInfo(): void {

        this.positionSvc.GetAccountTypes()
            .retry(1)
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(acctsResp => {
                this.buildAccountTypesInfo(acctsResp)
            });
    }

    buildAccountTypesInfo(acctTypes: any): string {

        for (let i = 1; i < acctTypes.length; i++) {
            this.acctTypeDropDownCodes.push(acctTypes[i]);
        }
        return;
    }

    buildAssetClassDescriptions(assetClassesInfo: any): string {

        // Due to alert dialog row limits, avoiding "\n" break after each description.
        let descriptions = "";
        for (let i = 1; i < assetClassesInfo.length; i++) {
            descriptions += "[" + assetClassesInfo[i].code + "] - " + assetClassesInfo[i].description + "  ||  ";
        }
        return descriptions;

    }

}
