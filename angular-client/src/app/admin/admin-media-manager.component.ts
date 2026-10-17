import { ApiService, InstagramAdminItem } from '../core/api.service';
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MediaSettings, MediaSettingsService, PortfolioItem } from '../core/media-settings.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-media-manager.component.html'
})
export class AdminMediaManagerComponent {
  instagramItems: InstagramAdminItem[] = [];
  instagramMessage = '';
  instagramBusy = false;
  instagramConfigured = false;

  loadInstagram(): void {
    this.instagramBusy = true;
    this.instagramMessage = '';
    this.api.getInstagramMedia().subscribe({
      next: result => {
        this.instagramItems = result.items.filter(item => item.isReel);
        this.instagramConfigured = result.configured;
        this.instagramMessage = result.configured ? 'Connected. Choose which Reels to show.' : 'Set Instagram__AccessToken on your backend and restart it.';
        this.instagramBusy = false;
      },
      error: error => this.instagramError(error)
    });
  }

  syncInstagram(): void {
    this.instagramBusy = true;
    this.api.syncInstagram().subscribe({
      next: () => this.loadInstagram(),
      error: error => this.instagramError(error)
    });
  }

  saveInstagram(item: InstagramAdminItem): void {
    this.instagramBusy = true;
    this.api.updateInstagramMedia(item).subscribe({
      next: () => { this.instagramBusy = false; this.instagramMessage = 'Saved for all visitors. Refresh the gallery to see changes.'; },
      error: error => this.instagramError(error)
    });
  }

  private instagramError(error: { status: number; error?: { detail?: string } }): void {
    this.instagramBusy = false;
    this.instagramMessage = error.status === 401
      ? 'Your session expired. Log in again.'
      : error.error?.detail || 'Request failed. Check the backend connection and database migrations.';
  }

  sasUrl = '';
  selectedFile: File | null = null;
  uploadMessage = '';
  uploading = false;
  saved = false;

  draft: MediaSettings;

  constructor(private mediaSettings: MediaSettingsService, private api: ApiService) {
    this.draft = this.mediaSettings.get() ?? {
      profileImageUrl: '',
      avatarImageUrl: '',
      portfolio: []
    };
    this.loadInstagram();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  async uploadToAzure(): Promise<void> {
    if (!this.selectedFile || !this.sasUrl) return;

    this.uploading = true;
    this.uploadMessage = '';

    const response = await fetch(this.sasUrl, {
      method: 'PUT',
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': this.selectedFile.type || 'application/octet-stream'
      },
      body: this.selectedFile
    });

    this.uploading = false;

    if (!response.ok) {
      this.uploadMessage = `Upload failed (${response.status}). Check SAS permissions (Write/Create).`;
      return;
    }

    const cleanBlobUrl = this.sasUrl.split('?')[0];
    this.uploadMessage = `Upload successful. Blob URL: ${cleanBlobUrl}`;
  }

  addItem(): void {
    const newItem: PortfolioItem = { imageUrl: '', title: '', caption: '' };
    this.draft.portfolio.push(newItem);
  }

  removeItem(index: number): void {
    this.draft.portfolio.splice(index, 1);
  }

  save(): void {
    this.mediaSettings.save(this.draft);
    this.saved = true;
  }
}
