export class Alert {
    type: AlertType;
    message: string;
    keepAfterRouteChange: boolean


    // Typescript 'Partial' interface allows for partial initialization of 'Alert' instances.
    // Input param 'init' is optional.
    constructor(init? : Partial<Alert>) {
        Object.assign(this, init);
    }
}


export enum AlertType {
    Error,
    Warning,
    Information,
    Success
}
