import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { TattooDeal } from '../models/models';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './admin-tattoo-deals.component.html'
})
export class AdminTattooDealsComponent implements OnInit {
  deals: TattooDeal[] = [];
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.api.getAdminTattooDeals().subscribe(x => this.deals = x); }
  disable(id: string): void { this.api.disableTattooDeal(id).subscribe(() => this.load()); }
}
