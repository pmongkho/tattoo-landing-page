import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { TattooDeal } from '../models/models';

@Component({
  standalone: true,
  selector: 'app-landing-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './landing-page.component.html'
})
export class LandingPageComponent implements OnInit {
  @ViewChild('consultationSection') consultationSection?: ElementRef<HTMLElement>;
  deals: TattooDeal[] = [];
  loadingDeals = false;
  submitState: 'idle' | 'success' | 'error' = 'idle';

  days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  form = this.fb.group({
    name: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required]],
    preferredArtist: ['No preference'],
    style: ['', [Validators.required]],
    placement: ['', [Validators.required]],
    size: ['', [Validators.required]],
    budget: [''],
    preferredDays: this.fb.array([]),
    description: ['', [Validators.required]],
    agreedToTerms: [false, [Validators.requiredTrue]],
    tattooDealId: ['']
  });

  constructor(private api: ApiService, private fb: FormBuilder) {}

  get preferredDaysArray(): FormArray { return this.form.controls.preferredDays as FormArray; }

  ngOnInit(): void {
    this.loadingDeals = true;
    this.api.getTattooDeals().subscribe({ next: deals => { this.deals = deals; this.loadingDeals = false; }, error: () => this.loadingDeals = false });
  }

  onDayToggle(day: string, checked: boolean): void {
    if (checked) { this.preferredDaysArray.push(this.fb.control(day)); return; }
    const index = this.preferredDaysArray.controls.findIndex((c) => c.value === day);
    if (index >= 0) this.preferredDaysArray.removeAt(index);
  }

  bookDeal(deal: TattooDeal): void {
    this.form.patchValue({ style: deal.style, placement: deal.placement, size: deal.size, budget: String(deal.discountedPrice), tattooDealId: deal.id });
    this.consultationSection?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  submit(): void {
    if (this.form.invalid) return;
    // TODO: send selected reference image files to dedicated storage and persist URLs.
    this.api.submitConsultation(this.form.value).subscribe({
      next: () => { this.submitState = 'success'; this.form.reset({ preferredArtist: 'No preference', agreedToTerms: false }); this.preferredDaysArray.clear(); },
      error: () => this.submitState = 'error'
    });
  }
}
