import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';

import { MarketsPageComponent } from './markets-page.component';

describe('MarketsPageComponent', () => {
  let component: MarketsPageComponent;
  let fixture: ComponentFixture<MarketsPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketsPageComponent],
      providers: [provideZonelessChangeDetection(), 
        provideHttpClient(),
        provideRouter([]),
        provideTranslateService({ fallbackLang: 'en', lang: 'en' }),
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(MarketsPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
