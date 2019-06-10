//import { enableProdMode } from '@angular/core';
//import { environment } from './environments/environment';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';
import { GlobalsService } from '../src/app/shared/globals.service';


export function getBaseUrl() {
    let globalsSvc = new GlobalsService();
    return globalsSvc.pimsBaseUrl;
}

const providers = [
    {
        provide: 'BASE_URL',
        useFactory: getBaseUrl, deps: []
    }
];

// Deferred.
//if (environment.production) {
//  enableProdMode();
//}

// Re-evaluate ?
platformBrowserDynamic(providers).bootstrapModule(AppModule)
  .catch(err => console.log(err));
