import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from './auth.service';
@Component({
  selector: 'app-login', imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `<section class="login-card"><p class="eyebrow">QUẢN LÝ KHO CỬA HÀNG</p><h2>Đăng nhập</h2><p>Sử dụng tài khoản được quản trị viên cấp.</p><form [formGroup]="form" (ngSubmit)="submit()"><mat-form-field><mat-label>Email</mat-label><input matInput type="email" formControlName="email" autocomplete="username" /></mat-form-field><mat-form-field><mat-label>Mật khẩu</mat-label><input matInput type="password" formControlName="password" autocomplete="current-password" /></mat-form-field>@if (error()) { <p role="alert" class="error">{{ error() }}</p> }<button mat-flat-button type="submit" [disabled]="form.invalid || busy()">{{ busy() ? 'Đang đăng nhập…' : 'Đăng nhập' }}</button></form></section>`,
  styles: `.login-card{max-width:420px;margin:48px auto;background:white;padding:32px;border-radius:16px;border:1px solid #dce5e7}form{display:grid;gap:12px}.eyebrow{font-size:12px;color:#0f766e;font-weight:700}.error{color:#b42318}`
})
export class Login {
  private readonly auth = inject(AuthService); private readonly router = inject(Router);
  readonly busy = signal(false); readonly error = signal('');
  readonly form = new FormGroup({ email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }), password: new FormControl('', { nonNullable: true, validators: [Validators.required] }) });
  async submit(): Promise<void> {
    if (this.form.invalid || this.busy()) return;
    this.busy.set(true); this.error.set('');
    try { const { email, password } = this.form.getRawValue(); await this.auth.login(email, password); this.form.controls.password.reset(); await this.router.navigateByUrl('/'); }
    catch { this.error.set('Không thể đăng nhập. Kiểm tra kết nối, email và mật khẩu.'); }
    finally { this.busy.set(false); }
  }
}
