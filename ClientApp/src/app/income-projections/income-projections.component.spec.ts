import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { IncomeProjectionsComponent } from './income-projections.component';

describe('IncomeProjectionsComponent', () => {
  let component: IncomeProjectionsComponent;
  let fixture: ComponentFixture<IncomeProjectionsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ IncomeProjectionsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(IncomeProjectionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
