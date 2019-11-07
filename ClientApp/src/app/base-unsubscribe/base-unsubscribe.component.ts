import { Component, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-base-unsubscribe',
  template: 'No UI necessary'
})
export class BaseUnsubscribeComponent implements OnDestroy {

    /* Base class provides necessary 'unSubscribe' functionality to all
     * referencing componenents, obviating the need of providing ngOnDestroy() and
     * private Subject(s) in each component needing to deal with subscription leaks.
     */
    private unSubscribe$: Subject<void> = null;

    constructor() { }

    //Uses singleton accessor fx to create & return a Subject only when needed.
    getUnsubscribe(): Subject<void> {

        if (!this.unSubscribe$) {
            this.unSubscribe$ = new Subject<void>();
        }
        return this.unSubscribe$;
    }

    ngOnDestroy(): void {
        if (this.unSubscribe$) {
            this.unSubscribe$.next();
            this.unSubscribe$.complete();
        }
    }
    
}
