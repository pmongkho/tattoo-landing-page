import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../core/api.service';
import { TattooDeal } from '../models/models';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-tattoo-deal-edit.component.html'
})
export class AdminTattooDealEditComponent implements OnInit {
  isEdit = false;
  id: string | null = null;
  form!: ReturnType<FormBuilder['group']>;
  constructor(private fb: FormBuilder, private route: ActivatedRoute, private api: ApiService, private router: Router) {
    this.form = this.fb.group({
      title: ['', Validators.required],
      style: ['', Validators.required],
      placement: ['', Validators.required],
      size: ['', Validators.required],
      originalPrice: [null as number | null],
      discountedPrice: [0, Validators.required],
      description: ['', Validators.required],
      referenceImageUrl: [''],
      isAvailable: [true],
      isFeatured: [false]
    });
  }
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
    const value = this.form.getRawValue();
    const payload: Partial<TattooDeal> = {
      title: value.title ?? undefined,
      style: value.style ?? undefined,
      placement: value.placement ?? undefined,
      size: value.size ?? undefined,
      originalPrice: value.originalPrice ?? undefined,
      discountedPrice: value.discountedPrice ?? undefined,
      description: value.description ?? undefined,
      referenceImageUrl: value.referenceImageUrl ?? undefined,
      isAvailable: value.isAvailable ?? undefined,
      isFeatured: value.isFeatured ?? undefined
    };
    const action = this.id ? this.api.updateTattooDeal(this.id, payload) : this.api.createTattooDeal(payload);
    action.subscribe(() => this.router.navigateByUrl('/admin/tattoo-deals'));
  }
}
