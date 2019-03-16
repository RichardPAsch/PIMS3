import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {

    constructor() { }

    currentDateTime = new Date;
    isReadOnly: boolean = true;

    ngOnInit() {
    }

    getProfile(): any {
        alert("in getProfile()");
    }

}
