import { Routes } from '@angular/router';
import { LandingPageComponent } from './public/landing-page.component';
import { AdminLoginComponent } from './admin/admin-login.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { AdminConsultationsComponent } from './admin/admin-consultations.component';
import { AdminTattooDealsComponent } from './admin/admin-tattoo-deals.component';
import { AdminTattooDealEditComponent } from './admin/admin-tattoo-deal-edit.component';
import { AdminMediaManagerComponent } from './admin/admin-media-manager.component';
import { PhoneFormComponent } from './public/phone-form.component';
import { adminGuard } from './core/auth.guard';

export const appRoutes: Routes = [
  { path: '', component: LandingPageComponent },
  { path: 'phone', component: PhoneFormComponent },
  { path: 'admin/login', component: AdminLoginComponent },
  { path: 'admin/dashboard', component: AdminDashboardComponent, canActivate: [adminGuard] },
  { path: 'admin/consultations', component: AdminConsultationsComponent, canActivate: [adminGuard] },
  { path: 'admin/tattoo-deals', component: AdminTattooDealsComponent, canActivate: [adminGuard] },
  { path: 'admin/tattoo-deals/new', component: AdminTattooDealEditComponent, canActivate: [adminGuard] },
  { path: 'admin/tattoo-deals/:id/edit', component: AdminTattooDealEditComponent, canActivate: [adminGuard] },
  { path: 'admin/media', component: AdminMediaManagerComponent, canActivate: [adminGuard] }
];
