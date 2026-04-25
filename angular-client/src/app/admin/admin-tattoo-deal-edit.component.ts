import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../core/api.service';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `<div class="p-6 max-w-2xl"><h1 class="text-2xl mb-4">{{isEdit ? 'Edit' : 'New'}} Tattoo Deal</h1>
<form [formGroup]="form" (ngSubmit)="save()" class="grid gap-3">
<input class="bg-zinc-900 p-2" placeholder="Title" formControlName="title" />
<input class="bg-zinc-900 p-2" placeholder="Style" formControlName="style" />
<input class="bg-zinc-900 p-2" placeholder="Placement" formControlName="placement" />
<input class="bg-zinc-900 p-2" placeholder="Size" formControlName="size" />
<input class="bg-zinc-900 p-2" type="number" placeholder="OriginalPrice" formControlName="originalPrice" />
<input class="bg-zinc-900 p-2" type="number" placeholder="DiscountedPrice" formControlName="discountedPrice" />
<textarea class="bg-zinc-900 p-2" placeholder="Description" formControlName="description"></textarea>
<input class="bg-zinc-900 p-2" placeholder="Reference image URL" formControlName="referenceImageUrl" />
<label><input type="checkbox" formControlName="isAvailable"/> Is available</label>
<label><input type="checkbox" formControlName="isFeatured"/> Is featured</label>
<button class="bg-white text-black p-2" [disabled]="form.invalid">Save</button></form></div>`
})
export class AdminTattooDealEditComponent implements OnInit {
  isEdit = false;
  id: string | null = null;
  form = this.fb.group({ title: ['', Validators.required], style: ['', Validators.required], placement: ['', Validators.required], size: ['', Validators.required], originalPrice: [null as number | null], discountedPrice: [0, Validators.required], description: ['', Validators.required], referenceImageUrl: [''], isAvailable: [true], isFeatured: [false] });
  constructor(private fb: FormBuilder, private route: ActivatedRoute, private api: ApiService, private router: Router) {}
  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.id;
    if (this.id) {
      this.api.getAdminTattooDeals().subscribe(deals => {
        const found = deals.find(d => d.id === this.id);
        if (found) this.form.patchValue(found);
      });
    }
  }
  save(): void {
    if (this.form.invalid) return;
    const action = this.id ? this.api.updateTattooDeal(this.id, this.form.value) : this.api.createTattooDeal(this.form.value);
    action.subscribe(() => this.router.navigateByUrl('/admin/tattoo-deals'));
  }
}
