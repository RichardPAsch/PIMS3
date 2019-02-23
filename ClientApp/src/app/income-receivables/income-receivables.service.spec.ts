import { TestBed } from '@angular/core/testing';

import { IncomeReceivablesService } from './income-receivables.service';

describe('IncomeReceivablesService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: IncomeReceivablesService = TestBed.get(IncomeReceivablesService);
    expect(service).toBeTruthy();
  });
});
