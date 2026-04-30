import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../core/api.service';
import { Consultation, ConsultationStatus } from '../models/models';

@Component({
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-consultations.component.html'
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
