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
  error = false;
  form!: ReturnType<FormBuilder['group']>;
  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {
    this.form = this.fb.group({ email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required]] });
  }
  login(): void {
    if (this.form.invalid) return;
    this.auth.login(this.form.value.email!, this.form.value.password!).subscribe({ next: () => this.router.navigateByUrl('/admin/dashboard'), error: () => this.error = true });
  }
}
