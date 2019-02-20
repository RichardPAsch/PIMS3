import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { IncomeReceivablesComponent } from './income-receivables.component';

describe('IncomeReceivablesComponent', () => {
  let component: IncomeReceivablesComponent;
  let fixture: ComponentFixture<IncomeReceivablesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ IncomeReceivablesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(IncomeReceivablesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
