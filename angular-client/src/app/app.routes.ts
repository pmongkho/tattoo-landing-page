import { Routes } from '@angular/router';
import { LandingPageComponent } from './public/landing-page.component';
import { AdminLoginComponent } from './admin/admin-login.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { AdminConsultationsComponent } from './admin/admin-consultations.component';
import { AdminMediaManagerComponent } from './admin/admin-media-manager.component';
import { PhoneFormComponent } from './public/phone-form.component';
import { adminGuard } from './core/auth.guard';

export const appRoutes: Routes = [
  { path: '', component: LandingPageComponent },
  { path: 'phone', component: PhoneFormComponent },
  { path: 'admin/login', component: AdminLoginComponent },
  { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [adminGuard] },
  { path: 'admin/consultations', component: AdminConsultationsComponent, canActivate: [adminGuard] },
  // Media management authenticates each server request with its separate media admin key.
  { path: 'admin/media', component: AdminMediaManagerComponent }
];
