import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { Consultation, ConsultationStatus } from '../models/models';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `<div class="p-6"><h1 class="text-2xl mb-4">Consultations</h1>
<table class="w-full text-sm"><thead><tr><th>Name</th><th>Phone</th><th>Email</th><th>Style</th><th>Placement</th><th>Size</th><th>Budget</th><th>Preferred Days</th><th>Description</th><th>Status</th><th>Created</th></tr></thead>
<tbody><tr *ngFor="let c of consultations" class="border-t border-zinc-800"><td>{{ c.name }}</td><td>{{ c.phoneNumber }}</td><td>{{ c.email }}</td><td>{{ c.style }}</td><td>{{ c.placement }}</td><td>{{ c.size }}</td><td>{{ c.budget }}</td><td>{{ preferredDaysDisplay(c) }}</td><td>{{ c.description }}</td><td>
<select [value]="c.status" (change)="setStatus(c, $any($event.target).value)"><option *ngFor="let s of statuses" [value]="s">{{ s }}</option></select></td><td>{{ c.createdAt | date:'short' }}</td></tr></tbody></table></div>`
})
export class AdminConsultationsComponent implements OnInit {
  consultations: Consultation[] = [];
  statuses: ConsultationStatus[] = ['New', 'Contacted', 'Booked', 'Declined'];
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.api.getAdminConsultations().subscribe(x => this.consultations = x); }
  preferredDaysDisplay(consultation: Consultation): string {
    return consultation.preferredDays.map((day) => day.day).join(', ');
  }
  setStatus(consultation: Consultation, status: ConsultationStatus): void {
    this.api.updateConsultationStatus(consultation.id, status).subscribe(() => consultation.status = status);
  }
}
