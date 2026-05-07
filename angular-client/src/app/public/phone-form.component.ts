import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

const obviousFakeNumbers = new Set([
  '1234567890',
  '0123456789',
  '9999999999',
  '0000000000',
  '1111111111'
]);

const usPhoneValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const rawValue = `${control.value ?? ''}`.trim();
  if (!rawValue) return null;

  let digits = rawValue.replace(/\D/g, '');
  if (digits.length === 11 && digits.startsWith('1')) {
    digits = digits.slice(1);
  }

  if (digits.length !== 10) {
    return { phoneFormat: true };
  }

  const areaCode = digits.slice(0, 3);
  const exchangeCode = digits.slice(3, 6);

  if (!/^[2-9]\d{2}$/.test(areaCode) || !/^[2-9]\d{2}$/.test(exchangeCode)) {
    return { phoneFormat: true };
  }

  if (new Set(digits).size === 1 || obviousFakeNumbers.has(digits)) {
    return { phoneFake: true };
  }

  return null;
};

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
    phoneNumber: ['', [Validators.required, usPhoneValidator]],
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
