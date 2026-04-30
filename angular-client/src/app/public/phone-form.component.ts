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

  form = this.fb.group({
    name: ['', [Validators.required]],
    phoneNumber: ['', [Validators.required]],
    smsConsent: [false, [Validators.requiredTrue]]
  });

  constructor(private readonly api: ApiService) {}

  submit(): void {
    if (this.form.invalid) return;

    this.api.submitConsultation({
      name: this.form.value.name,
      phoneNumber: this.form.value.phoneNumber
    }).subscribe({
      next: () => {
        this.submitState = 'success';
        this.form.reset({ smsConsent: false });
      },
      error: () => this.submitState = 'error'
    });
  }
}
