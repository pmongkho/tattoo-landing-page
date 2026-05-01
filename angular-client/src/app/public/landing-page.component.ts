import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { RouterLink } from '@angular/router';
import { MediaSettingsService } from '../core/media-settings.service';

type PortfolioItem = {
  imageUrl: string;
  title: string;
  caption: string;
};

@Component({
  standalone: true,
  selector: 'app-landing-page',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './landing-page.component.html'
})
export class LandingPageComponent {
  profileImageUrl = 'https://images.unsplash.com/photo-1542727365-19732a80dcfd?auto=format&fit=crop&w=1200&q=80';
  avatarImageUrl = 'https://images.unsplash.com/photo-1611501275019-9b5cda994e8d?auto=format&fit=crop&w=300&q=80';
  submitState: 'idle' | 'success' | 'error' = 'idle';
  portfolio: PortfolioItem[] = this.buildPortfolio();

  private buildPortfolio(): PortfolioItem[] {
    return Array.from({ length: 10 }, (_, index) => {
      const imageNumber = String(index + 1).padStart(2, '0');
      return {
        imageUrl: `assets/images/portfolio/tattoo-${imageNumber}.jpeg`,
        title: `Portfolio Piece ${index + 1}`,
        caption: 'Custom tattoo work by Wo Hu'
      };
    });
  }

  form!: ReturnType<FormBuilder['group']>;

  constructor(private api: ApiService, private fb: FormBuilder, private mediaSettings: MediaSettingsService) {
    const settings = this.mediaSettings.get();
    if (settings) {
      this.profileImageUrl = settings.profileImageUrl || this.profileImageUrl;
      this.avatarImageUrl = settings.avatarImageUrl || this.avatarImageUrl;
      this.portfolio = settings.portfolio?.length ? settings.portfolio : this.portfolio;
    }

    this.form = this.fb.group({
      name: ['', [Validators.required]],
      phoneNumber: ['', [Validators.required]],
      timeline: ['', [Validators.required]],
    });
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
