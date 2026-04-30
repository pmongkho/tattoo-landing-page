import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MediaSettings, MediaSettingsService, PortfolioItem } from '../core/media-settings.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="p-6 max-w-5xl mx-auto space-y-6">
      <h1 class="text-2xl">Media Dashboard (Azure Blob)</h1>

      <div class="bg-zinc-900 p-4 rounded space-y-3">
        <h2 class="text-xl">Azure Blob Upload</h2>
        <p class="text-sm text-zinc-300">Paste a blob SAS upload URL, pick a file, and upload. Then copy the final blob URL into the fields below.</p>
        <input [(ngModel)]="sasUrl" placeholder="https://...blob.core.windows.net/.../file.jpg?...SAS..." class="w-full p-2 bg-black border border-zinc-700" />
        <input type="file" (change)="onFileSelected($event)" class="block" />
        <button (click)="uploadToAzure()" [disabled]="!sasUrl || !selectedFile || uploading" class="px-4 py-2 bg-white text-black rounded">
          {{ uploading ? 'Uploading...' : 'Upload File' }}
        </button>
        <p *ngIf="uploadMessage" class="text-sm">{{ uploadMessage }}</p>
      </div>

      <div class="bg-zinc-900 p-4 rounded space-y-4">
        <h2 class="text-xl">Profile Images</h2>
        <input [(ngModel)]="draft.profileImageUrl" placeholder="Profile image URL" class="w-full p-2 bg-black border border-zinc-700" />
        <input [(ngModel)]="draft.avatarImageUrl" placeholder="Avatar image URL" class="w-full p-2 bg-black border border-zinc-700" />
      </div>

      <div class="bg-zinc-900 p-4 rounded space-y-4">
        <h2 class="text-xl">Fresh Portfolio</h2>
        <div *ngFor="let item of draft.freshPortfolio; let i = index" class="grid md:grid-cols-3 gap-2 border border-zinc-700 p-3">
          <input [(ngModel)]="item.imageUrl" placeholder="Image URL" class="p-2 bg-black border border-zinc-700" />
          <input [(ngModel)]="item.title" placeholder="Title" class="p-2 bg-black border border-zinc-700" />
          <input [(ngModel)]="item.caption" placeholder="Caption" class="p-2 bg-black border border-zinc-700" />
          <button (click)="removeItem('fresh', i)" class="underline text-left">Remove</button>
        </div>
        <button (click)="addItem('fresh')" class="underline">+ Add fresh item</button>
      </div>

      <div class="bg-zinc-900 p-4 rounded space-y-4">
        <h2 class="text-xl">Healed Portfolio</h2>
        <div *ngFor="let item of draft.healedPortfolio; let i = index" class="grid md:grid-cols-3 gap-2 border border-zinc-700 p-3">
          <input [(ngModel)]="item.imageUrl" placeholder="Image URL" class="p-2 bg-black border border-zinc-700" />
          <input [(ngModel)]="item.title" placeholder="Title" class="p-2 bg-black border border-zinc-700" />
          <input [(ngModel)]="item.caption" placeholder="Caption" class="p-2 bg-black border border-zinc-700" />
          <button (click)="removeItem('healed', i)" class="underline text-left">Remove</button>
        </div>
        <button (click)="addItem('healed')" class="underline">+ Add healed item</button>
      </div>

      <button (click)="save()" class="px-4 py-2 bg-white text-black rounded">Save Settings</button>
      <p *ngIf="saved" class="text-green-400">Saved. Refresh landing page to verify updates.</p>
    </div>
  `
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
      freshPortfolio: [],
      healedPortfolio: []
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

  addItem(kind: 'fresh' | 'healed'): void {
    const newItem: PortfolioItem = { imageUrl: '', title: '', caption: '' };
    if (kind === 'fresh') this.draft.freshPortfolio.push(newItem);
    else this.draft.healedPortfolio.push(newItem);
  }

  removeItem(kind: 'fresh' | 'healed', index: number): void {
    if (kind === 'fresh') this.draft.freshPortfolio.splice(index, 1);
    else this.draft.healedPortfolio.splice(index, 1);
  }

  save(): void {
    this.mediaSettings.save(this.draft);
    this.saved = true;
  }
}
