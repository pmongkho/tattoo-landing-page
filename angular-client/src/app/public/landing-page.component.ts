import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';

type PortfolioTab = 'fresh' | 'healed';

type PortfolioItem = {
  imageUrl: string;
  title: string;
  caption: string;
};

@Component({
  standalone: true,
  selector: 'app-landing-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './landing-page.component.html'
})
export class LandingPageComponent {
  submitState: 'idle' | 'success' | 'error' = 'idle';
  activePortfolioTab: PortfolioTab = 'fresh';

  freshPortfolio: PortfolioItem[] = [
    {
      imageUrl: 'https://images.unsplash.com/photo-1519014816548-bf5fe059798b?auto=format&fit=crop&w=1200&q=80',
      title: 'Fresh Raven Sleeve',
      caption: 'Session 1 complete • high-contrast blackwork and texture.'
    },
    {
      imageUrl: 'https://images.unsplash.com/photo-1470309864661-68328b2cd0a5?auto=format&fit=crop&w=1200&q=80',
      title: 'Fresh Portrait Study',
      caption: 'Fine-line facial detail with soft transitions in the shadows.'
    },
    {
      imageUrl: 'https://images.unsplash.com/photo-1562962230-16e4623d36e6?auto=format&fit=crop&w=1200&q=80',
      title: 'Fresh Forearm Concept',
      caption: 'First pass complete with layout balanced for future additions.'
    }
  ];

  healedPortfolio: PortfolioItem[] = [
    {
      imageUrl: 'https://images.unsplash.com/photo-1542727365-19732a80dcfd?auto=format&fit=crop&w=1200&q=80',
      title: 'Healed Backpiece Segment',
      caption: '8 months healed • depth and texture preserved.'
    },
    {
      imageUrl: 'https://images.unsplash.com/photo-1598371839696-5c5bb00bdc28?auto=format&fit=crop&w=1200&q=80',
      title: 'Healed Floral Realism',
      caption: 'Smooth gradients after full healing cycle.'
    },
    {
      imageUrl: 'https://images.unsplash.com/photo-1590246814883-57f3a96fbcf2?auto=format&fit=crop&w=1200&q=80',
      title: 'Healed Chest Piece',
      caption: 'Crisp edge integrity with healed contrast retention.'
    }
  ];

  form!: ReturnType<FormBuilder['group']>;

  constructor(private api: ApiService, private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', [Validators.required]],
      phoneNumber: ['', [Validators.required]],
      timeline: ['', [Validators.required]],
    });
  }

  get displayedPortfolio(): PortfolioItem[] {
    return this.activePortfolioTab === 'fresh' ? this.freshPortfolio : this.healedPortfolio;
  }


  setPortfolioTab(tab: PortfolioTab): void {
    this.activePortfolioTab = tab;
  }

  submit(): void {
    if (this.form.invalid) return;

    this.api.submitConsultation(this.form.value).subscribe({
      next: () => {
        this.submitState = 'success';
        this.form.reset({ timeline: '' });
      },
      error: () => this.submitState = 'error'
    });
  }
}
