import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { Consultation, ConsultationStatus } from '../models/models';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `<div class="p-6"><h1 class="text-2xl mb-4">Consultations</h1>
<table class="w-full text-sm"><thead><tr><th>Name</th><th>Phone</th><th>Timeline</th><th>Status</th><th>Created</th></tr></thead>
<tbody><tr *ngFor="let c of consultations" class="border-t border-zinc-800"><td>{{ c.name }}</td><td>{{ c.phoneNumber }}</td><td>{{ c.timeline }}</td><td>
<select [value]="c.status" (change)="setStatus(c, $any($event.target).value)"><option *ngFor="let s of statuses" [value]="s">{{ statusLabels[s] }}</option></select></td><td>{{ c.createdAt | date:'short' }}</td></tr></tbody></table></div>`
})
export class AdminConsultationsComponent implements OnInit {
  consultations: Consultation[] = [];
  statuses: ConsultationStatus[] = ['New', 'Contacted', 'ConsultScheduled', 'ConsultCompleted', 'DesignInProgress', 'DepositRequested', 'DepositPaid', 'Booked', 'Completed', 'FollowUp'];
  readonly statusLabels: Record<ConsultationStatus, string> = {
    New: 'New',
    Contacted: 'Contacted',
    ConsultScheduled: 'Consult Scheduled',
    ConsultCompleted: 'Consult Completed',
    DesignInProgress: 'Design In Progress',
    DepositRequested: 'Deposit Requested',
    DepositPaid: 'Deposit Paid',
    Booked: 'Booked',
    Completed: 'Completed',
    FollowUp: 'Follow Up'
  };
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.api.getAdminConsultations().subscribe(x => this.consultations = x); }
  setStatus(consultation: Consultation, status: ConsultationStatus): void {
    this.api.updateConsultationStatus(consultation.id, status).subscribe(() => consultation.status = status);
  }
}
