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
	templateUrl: './landing-page.component.html',
})
export class LandingPageComponent {

	avatarImageUrl =
		'https://scontent-dfw5-2.cdninstagram.com/v/t51.82787-19/647361603_18083963795211234_4039720718771227224_n.jpg?stp=dst-jpg_s150x150_tt6&_nc_cat=100&ccb=7-5&_nc_sid=f7ccc5&efg=eyJ2ZW5jb2RlX3RhZyI6InByb2ZpbGVfcGljLnd3dy4zMjAuQzMifQ%3D%3D&_nc_ohc=KBdSpMcnR-4Q7kNvwGUMBjy&_nc_oc=AdpsKSseqifIJT3GW8Q5eDPXC2Fa67njR2o6xVExstPc7v5wP9Y5CMfVn4sc8xxZsOU&_nc_zt=24&_nc_ht=scontent-dfw5-2.cdninstagram.com&_nc_gid=eGTkS9gquc02ub2qyYzE8g&_nc_ss=7b6a8&oh=00_Af6XP6PcXEx7QQPqG2TQbdCjoI_RHkh9apCYe-b5HgX7sQ&oe=69FA882C'
	submitState: 'idle' | 'success' | 'error' = 'idle'
	portfolio: PortfolioItem[] = this.buildPortfolio()

	private buildPortfolio(): PortfolioItem[] {
		return Array.from({ length: 10 }, (_, index) => {
			const imageNumber = String(index + 1).padStart(2, '0')
			return {
				imageUrl: `assets/images/portfolio/tattoo-${imageNumber}.jpeg`,
				title: `Portfolio Piece ${index + 1}`,
				caption: 'Custom tattoo work by Wo Hu',
			}
		})
	}

	form!: ReturnType<FormBuilder['group']>

	constructor(
		private api: ApiService,
		private fb: FormBuilder,
		private mediaSettings: MediaSettingsService
	) {
		const settings = this.mediaSettings.get()
		if (settings) {
			this.avatarImageUrl = settings.avatarImageUrl || this.avatarImageUrl
			this.portfolio = settings.portfolio?.length
				? settings.portfolio
				: this.portfolio
		}

		this.form = this.fb.group({
			name: ['', [Validators.required]],
			phoneNumber: ['', [Validators.required]],
			timeline: ['', [Validators.required]],
		})
	}

	submit(): void {
		if (this.form.invalid) return

		this.api.submitConsultation(this.form.value).subscribe({
			next: () => {
				this.submitState = 'success'
				this.form.reset({ timeline: '' })
			},
			error: () => (this.submitState = 'error'),
		})
	}
}
