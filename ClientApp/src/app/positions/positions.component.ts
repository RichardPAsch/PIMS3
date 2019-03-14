import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { Position } from '../positions/position';
import { PositionsService } from '../positions/positions.service';
import { HttpErrorResponse } from '@angular/common/http';
//import { FormControl } from '@angular/forms';

@Component({
  selector: 'app-positions',
  templateUrl: './positions.component.html',
  styleUrls: ['./positions.component.css']
})
export class PositionsComponent implements OnInit {

    constructor(private positionSvc: PositionsService) {

    }

    positionCount: number;
    includeInactive: boolean = false;

    @ViewChild('agGridPositions')
    agGridPositions: AgGridNg2;

    ngOnInit() {
        this.fetchPositions(false);
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, filter: true, checkboxSelection: true, width: 98, resizable: true, editable: true },
        { headerName: "Description", field: "tickerDescription", width: 160, resizable: true },
        { headerName: "Account", field: "accountTypeDesc", width: 92, editable: true, resizable: true, filter: true },
        { headerName: "Status", field: "status", width: 77,
            sortable: true,
            resizable: true,
            editable: true,
            cellEditor: "agPopupSelectCellEditor",
            cellEditorParams: {
                values: ["A", "I"]
            }
        },
        { headerName: "Asset Class", field: "assetClass", width: 117, sortable: true, filter: true, filterParams: { applyButton: true}, resizable: true},
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

        for (let pos = 0; pos < selectedPositionEdits.length; pos++) {
                        
            let editedPosition = new Position();

            // Capture only necessary column edit(s) for each Id.
            editedPosition.positionId = selectedPositionEdits[pos].positionId;
            editedPosition.status = selectedPositionEdits[pos].status;
            editedPosition.pymtDue = selectedPositionEdits[pos].pymtDue;

            editedPositions.push(editedPosition);
        }
        
        this.positionSvc.UpdateEditedPositions(editedPositions)
            .retry(2)
            .subscribe(updateResponseCount => {
                if (Number(updateResponseCount) > 0) {
                    alert("Successfully updated " + updateResponseCount + " Position(s).");
                    this.fetchPositions(this.includeInactive);
                }
                else
                    alert("Error updating Position(s).");
            },
            (apiError: HttpErrorResponse) => {
                if (apiError.error instanceof Error)
                    alert("Error processing Position update(s) : \network or application error. Please try later.");
                else {
                    alert("Error processing Position update(s) due to : \n" + apiError.message);
                }
            }
         ) // end subscribe()

    }


    processInactiveData(includeInactive: any) {
        this.fetchPositions(includeInactive);
    }

}
