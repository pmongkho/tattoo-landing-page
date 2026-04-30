import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `<div class="p-6"><h1 class="text-2xl mb-4">Admin Dashboard</h1>
<div class="grid md:grid-cols-4 gap-4">
<div class="bg-zinc-900 p-4">Total consultation leads: {{ totalLeads }}</div>
<div class="bg-zinc-900 p-4">New leads: {{ newLeads }}</div>
<div class="bg-zinc-900 p-4">Available tattoo deals: {{ availableDeals }}</div>
<div class="bg-zinc-900 p-4">Booked/claimed tattoo deals: {{ bookedDeals }}</div>
</div><div class="mt-6"><a routerLink="/admin/media" class="underline">Manage portfolio/profile media</a></div></div>`
})
export class AdminDashboardComponent implements OnInit {
  totalLeads = 0; newLeads = 0; availableDeals = 0; bookedDeals = 0;
  constructor(private api: ApiService) {}
  ngOnInit(): void {
    this.api.getAdminConsultations().subscribe(cs => { this.totalLeads = cs.length; this.newLeads = cs.filter(x => x.status === 'New').length; });
    this.api.getAdminTattooDeals().subscribe(ds => { this.availableDeals = ds.filter(x => x.isAvailable).length; this.bookedDeals = ds.filter(x => !x.isAvailable).length; });
  }
}
