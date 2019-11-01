import { Injectable } from '@angular/core';
import { Router, NavigationStart } from '@angular/router';
import { Observable, Subject } from 'rxjs';
import { Alert, AlertType } from '../alert/alert';

@Injectable({
  providedIn: 'root'
})
export class AlertService {
    /*
     * Serves as interface between any Angular component and the Alert component--which displays alert / toaster messages.
     * This service contains methods for sending and clearing alert messages, and also subscribes to the router NavigationStart event to
     * automatically clear alert messages on a route change, unless the keepAfterRouteChange flag is set to true. If true, the alert messages
     * survive a single route change and are cleared on the next route change.
    */
    private subject = new Subject<Alert>(); // subjects subscribe to an Observable. May have > 1.
    private keepAfterRouteChange: boolean = false;

    constructor(private router: Router) {

        this.router.events.subscribe(event => {
            if (event instanceof NavigationStart) {
                if (this.keepAfterRouteChange) {
                    // Display alert messages for a single route change only.
                    this.keepAfterRouteChange = false;
                } else {
                    this.clear();
                }
            }
        });
    }

    onAlert(): Observable<Alert> {
        // Create new Observable with this service as a Subject & source. Allows for
        // creating customized Observer side-logic of the Subject, and concealing
        // it from code the uses the Observable.
        // Enables subscribing to alerts observable
        return this.subject.asObservable();
    }

    alert(alert: Alert) {
        this.keepAfterRouteChange = alert.keepAfterRouteChange;
        this.subject.next(alert);
    }


    /* == Convenience methods == */
    clear() {
        this.subject.next(new Alert());
    }

    success(messageToDisplay: string) {
        this.alert(new Alert({
            message: messageToDisplay,
            type: AlertType.Success
        }));
    }

    error(messageToDisplay: string) {
        this.alert(new Alert({
            message: messageToDisplay,
            type: AlertType.Error
        }));
    }

    info(messageToDisplay: string) {
        this.alert(new Alert({
            message: messageToDisplay,
            type: AlertType.Information
        }));
    }

    warn(messageToDisplay: string) {
        this.alert(new Alert({
            message: messageToDisplay,
            type: AlertType.Warning
        }));
    }

}
