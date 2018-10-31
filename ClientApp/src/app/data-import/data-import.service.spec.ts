import { TestBed, inject } from '@angular/core/testing';

import { DataImportService } from './data-import.service';

describe('DataImportService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [DataImportService]
    });
  });

  it('should be created', inject([DataImportService], (service: DataImportService) => {
    expect(service).toBeTruthy();
  }));
});
