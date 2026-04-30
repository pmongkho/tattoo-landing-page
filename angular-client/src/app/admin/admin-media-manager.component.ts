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
  sasUrl = '';
  selectedFile: File | null = null;
  uploadMessage = '';
  uploading = false;
  saved = false;

  draft: MediaSettings;

  constructor(private mediaSettings: MediaSettingsService) {
    this.draft = this.mediaSettings.get() ?? {
      profileImageUrl: '',
      avatarImageUrl: '',
      portfolio: []
    };
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
