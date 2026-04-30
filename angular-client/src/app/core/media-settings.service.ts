import { Injectable } from '@angular/core';

export type PortfolioItem = {
  imageUrl: string;
  title: string;
  caption: string;
};

export type MediaSettings = {
  profileImageUrl: string;
  avatarImageUrl: string;
  freshPortfolio: PortfolioItem[];
  healedPortfolio: PortfolioItem[];
};

@Injectable({ providedIn: 'root' })
export class MediaSettingsService {
  private readonly key = 'wohu_media_settings_v1';

  get(): MediaSettings | null {
    const raw = localStorage.getItem(this.key);
    if (!raw) return null;

    try {
      return JSON.parse(raw) as MediaSettings;
    } catch {
      return null;
    }
  }

  save(settings: MediaSettings): void {
    localStorage.setItem(this.key, JSON.stringify(settings));
  }
}
