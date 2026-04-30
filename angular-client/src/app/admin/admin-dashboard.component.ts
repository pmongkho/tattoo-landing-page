import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-dashboard.component.html'
})
export class AdminDashboardComponent implements OnInit {
  totalLeads = 0; newLeads = 0; availableDeals = 0; bookedDeals = 0;
  constructor(private api: ApiService) {}
  ngOnInit(): void {
    this.api.getAdminConsultations().subscribe(cs => { this.totalLeads = cs.length; this.newLeads = cs.filter(x => x.status === 'New').length; });
    this.api.getAdminTattooDeals().subscribe(ds => { this.availableDeals = ds.filter(x => x.isAvailable).length; this.bookedDeals = ds.filter(x => !x.isAvailable).length; });
  }
}
