import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';

import { PricesPageComponent } from './prices-page.component';

describe('PricesPageComponent', () => {
  let component: PricesPageComponent;
  let fixture: ComponentFixture<PricesPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PricesPageComponent],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(PricesPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
