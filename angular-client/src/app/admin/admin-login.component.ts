import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `<div class="p-6 max-w-md mx-auto"><h1 class="text-2xl mb-4">Admin Login</h1>
<form [formGroup]="form" (ngSubmit)="login()" class="grid gap-3">
<input class="bg-zinc-900 p-2" formControlName="email" placeholder="Email" />
<input class="bg-zinc-900 p-2" type="password" formControlName="password" placeholder="Password" />
<button class="bg-white text-black p-2" [disabled]="form.invalid">Login</button>
<p class="text-red-400" *ngIf="error">Login failed.</p></form></div>`
})
export class AdminLoginComponent {
  error = false;
  form = this.fb.group({ email: ['', [Validators.required, Validators.email]], password: ['', [Validators.required]] });
  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {}
  login(): void {
    if (this.form.invalid) return;
    this.auth.login(this.form.value.email!, this.form.value.password!).subscribe({ next: () => this.router.navigateByUrl('/admin/dashboard'), error: () => this.error = true });
  }
}
