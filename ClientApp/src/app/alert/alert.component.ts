import { Component, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { Alert, AlertType } from '../alert/alert';
import { AlertService } from '../shared/alert.service';
import { takeUntil } from 'rxjs/operators';
import { BaseUnsubscribeComponent } from '../base-unsubscribe/base-unsubscribe.component';


@Component({
  selector: 'app-alert',
  templateUrl: './alert.component.html',
  styleUrls: ['./alert.component.css']
})

export class AlertComponent extends BaseUnsubscribeComponent implements OnInit, OnDestroy {
    /* Controls adding/removing alerts in the template, and maintains an array of alerts that are rendered by the component template.  */
    alerts: Alert[] = [];
    subscription: Subscription;

    constructor(private alertService: AlertService) {
        super();
    }

    // ngOnInit() subscribes to the Observable returned from the alertService.onAlert() method;
    // this enables the alert component to be notified whenever an alert message is sent to the alert service
    // and adds it to the alerts array for display.Sending an alert with an empty message to the alert service
    // tells the alert component to clear the alerts array.
    ngOnInit() {
        this.subscription = this.alertService.onAlert()
            .pipe(takeUntil(this.getUnsubscribe()))
            .subscribe(alert => {
                if (!alert.message) {
                    this.alerts = [];
                    return;
                }
                this.alerts.push(alert);
            });
    }

    ngOnDestroy() {
        this.subscription.unsubscribe();
    }

    removeAlert(alertDismiss: Alert) {
        // Rebuild collection based on callback predicate.
        this.alerts = this.alerts.filter(alert => alert != alertDismiss);
    }

    alertTypeCssClass(alert: Alert) {

        // Return appropriate UI bootstrap class, based on alert type needed.
        if (alert == null)
            return;

        switch (alert.type) {
            case AlertType.Error:
                return 'alert alert-danger';
            case AlertType.Information:
                return 'alert alert-info';
            case AlertType.Success:
                return 'alert alert-success';
            case AlertType.Warning:
                return 'alert alert-warning';
        }
    }

}
