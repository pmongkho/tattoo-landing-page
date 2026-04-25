import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { TattooDeal } from '../models/models';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `<div class="p-6"><h1 class="text-2xl mb-4">Tattoo Deals</h1>
<a routerLink="/admin/tattoo-deals/new" class="underline">Create new deal</a>
<div class="mt-4 grid gap-3"><div class="bg-zinc-900 p-4" *ngFor="let d of deals"><h3>{{ d.title }}</h3><p>{{ d.description }}</p><p>${{ d.discountedPrice }}</p>
<a [routerLink]="['/admin/tattoo-deals', d.id, 'edit']" class="mr-3 underline">Edit</a><button (click)="disable(d.id)" class="underline">Disable</button>
</div></div></div>`
})
export class AdminTattooDealsComponent implements OnInit {
  deals: TattooDeal[] = [];
  constructor(private api: ApiService) {}
  ngOnInit(): void { this.load(); }
  load(): void { this.api.getAdminTattooDeals().subscribe(x => this.deals = x); }
  disable(id: string): void { this.api.disableTattooDeal(id).subscribe(() => this.load()); }
}
