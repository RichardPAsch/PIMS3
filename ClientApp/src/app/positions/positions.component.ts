import { Component, OnInit, ViewChild } from '@angular/core';
import { AgGridNg2 } from 'ag-grid-angular';
import { Position } from '../positions/position';
import { PositionsService } from '../positions/positions.service';

@Component({
  selector: 'app-positions',
  templateUrl: './positions.component.html',
  styleUrls: ['./positions.component.css']
})
export class PositionsComponent implements OnInit {

    constructor(private service: PositionsService) {

    }

    positionCount: number;

    @ViewChild('agGridPositions')
    agGridPositions: AgGridNg2;

    ngOnInit() {
        this.fetchPositions();
    }

    columnDefs = [
        { headerName: "Ticker", field: "tickerSymbol", sortable: true, filter: true, checkboxSelection: true, width: 90, resizable: true },
        { headerName: "Description", field: "tickerDescription", width: 160, resizable: true },
        { headerName: "Account", field: "accountTypeDesc", width: 92, editable: true, resizable: true, filter: true },
        { headerName: "Status", field: "status", width: 80,
            sortable: true,
            resizable: true,
            editable: true,
            cellEditor: "agPopupSelectCellEditor",
            cellEditorParams: {
                values: ["A", "I"]
            }
        },
        { headerName: "Last Update", field: "lastUpdate", width: 120, sortable: true, filter: true, resizable: true},
        {   headerName: "Paid",
            field: "pymtDue",
            width: 70,
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
    
    fetchPositions(): void {

        this.service.BuildPositions()
            .retry(1)
            .subscribe(positionsResponse => {
                this.agGridPositions.api.setRowData(this.mapResponseToGrid(positionsResponse));
                this.positionCount = positionsResponse.length;
            });
    }

    // TODO: 3.1.2019 - complete code for processPositions(). Then deal with how to handle revenue records.

    mapResponseToGrid(responseData: any): Position[] {

        //let mappedPositions: Position[];
        let mappedPositions = new Array<Position>();

        for (var i = 0; i < responseData.length; i++) {
            let position: Position = new Position();
            //console.log("ticker : " + responseData[i].tickerSymbol);

            position.tickerSymbol = responseData[i].tickerSymbol;
            if (i >= 1) {
                position.tickerDescription = responseData[i - 1].tickerDescription == responseData[i].tickerDescription
                    ? "------"
                    : responseData[i].tickerDescription;
            } else {
                position.tickerDescription = responseData[i].tickerDescription;
            }
            position.accountTypeDesc = responseData[i].account;
            position.lastUpdate = responseData[i].lastUpdate;
            position.status = responseData[i].status;
            position.pymtDue = responseData[i].pymtDue;
            // reference: https://next.plnkr.co/edit/AcfU8spNR4C5gwWu4vtw?utm_source=legacy&utm_medium=worker&utm_campaign=next&preview
            position.positionId = responseData[i].positionId;

            mappedPositions.push(position);
        }

        return mappedPositions;
    }

    processEditedPositions() {

        var selectedNodes = this.agGridPositions.api.getSelectedNodes();
        var selectedPositionEdits = selectedNodes.map(node => node.data);

        for (let pos = 0; pos < selectedPositionEdits.length; pos++) {
            let editedPositions = new Array<Position>();
            let editedPosition = new Position();
            // Capture just needed edited fields for each id.
            editedPosition.positionId = selectedPositionEdits[pos].positionId;
            editedPosition.status = selectedPositionEdits[pos].status;
            editedPosition.pymtDue = selectedPositionEdits[pos].pymtDue;

            editedPositions.push(editedPosition);
        }
        // -- TO BE CONTINUED -- 3.1.2019


        //this.receivablesSvc.UpdateIncomeReceivables(selectedPositionData)
        //    .retry(2)
        //    .subscribe(updateResponse => {
        //        if (updateResponse) {
        //            alert("Successfully marked : " + selectedPositionData.length + " Position(s) as paid.");
        //            // Refresh.
        //            this.getReceivables();
        //        }
        //        else
        //            alert("Error marking Position(s) as payment received, check Position data.");
        //    },
        //        (apiError: HttpErrorResponse) => {
        //            if (apiError.error instanceof Error)
        //                alert("Error processing Position update(s) : \network or application error. Please try later.");
        //            else {
        //                alert("Error processing Position update(s) due to : \n" + apiError.message);
        //            }
        //        }
        //    )

    }

}
