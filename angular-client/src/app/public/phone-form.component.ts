import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

@Component({
  standalone: true,
  selector: 'app-phone-form',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './phone-form.component.html'
})
export class PhoneFormComponent {
  private readonly fb = inject(FormBuilder);
  submitState: 'idle' | 'success' | 'error' = 'idle';
  isSubmitting = false;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.pattern(/^\s*[A-Za-z]+(?:[ '-][A-Za-z]+)+\s*$/)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^\+?1?[\s.-]?(?:\([2-9]\d{2}\)|[2-9]\d{2})[\s.-]?\d{3}[\s.-]?\d{4}$/)]],
    smsConsent: [false, [Validators.requiredTrue]]
  });

  constructor(private readonly api: ApiService) {}

  submit(): void {
    if (this.form.invalid || this.isSubmitting) return;

    this.submitState = 'idle';
    this.isSubmitting = true;

    this.api.submitConsultation({
      name: this.form.value.name,
      phoneNumber: this.form.value.phoneNumber
    }).subscribe({
      next: () => {
        this.submitState = 'success';
        this.isSubmitting = false;
        this.form.reset({ smsConsent: false });
      },
      error: () => {
        this.submitState = 'error';
        this.isSubmitting = false;
      }
    });
  }
}
