import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-login.component.html'
})
export class AdminLoginComponent {
  errorMessage = '';
  submitting = false;
  form!: ReturnType<FormBuilder['group']>;
  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({ email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required]] });
  }
  login(): void {
    if (this.form.invalid || this.submitting) return;
    this.errorMessage = '';
    this.submitting = true;
    this.auth.login(this.form.value.email!, this.form.value.password!).subscribe({ next: () => this.router.navigateByUrl('/admin/dashboard'), error: error => {
      this.submitting = false;
      this.errorMessage = error.status === 401 ? 'Invalid email or password, or the account is temporarily locked.'
        : error.status === 503 ? 'Admin login is not configured. Check the backend environment settings.'
        : 'Unable to sign in. Check the backend connection and database setup.';
    } });
  }
}
