import { PortfolioItem } from './media-settings.service';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Consultation, ConsultationStatus, TattooDeal } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly api = `${environment.apiBaseUrl}/api`;

  constructor(private http: HttpClient) {}

  getInstagramReels() { return this.http.get<PortfolioItem[]>(`${this.api}/portfolio/reels`); }
  getPortfolio() { return this.http.get<{ enabled: boolean; items: PortfolioItem[] }>(`${this.api}/portfolio`); }
  getInstagramMedia() {
    return this.http.get<{ configured: boolean; items: InstagramAdminItem[] }>(`${this.api}/portfolio/manage`);
  }
  syncInstagram() {
    return this.http.post<{ synced: number }>(`${this.api}/portfolio/sync`, {});
  }
  updateInstagramMedia(item: InstagramAdminItem) {
    return this.http.patch(`${this.api}/portfolio/manage/${encodeURIComponent(item.id)}`, {
      visible: item.visible, pinned: item.pinned, displayOrder: item.displayOrder
    });
  }

  getTattooDeals() { return this.http.get<TattooDeal[]>(`${this.api}/tattoo-deals`); }
  submitConsultation(payload: unknown) { return this.http.post(`${this.api}/consultations`, payload); }

  getAdminConsultations() { return this.http.get<Consultation[]>(`${this.api}/admin/consultations`); }
  updateConsultationStatus(id: string, status: ConsultationStatus) {
    return this.http.patch(`${this.api}/admin/consultations/${id}/status`, { status });
  }

  getAdminTattooDeals() { return this.http.get<TattooDeal[]>(`${this.api}/admin/tattoo-deals`); }
  createTattooDeal(payload: Partial<TattooDeal>) { return this.http.post(`${this.api}/admin/tattoo-deals`, payload); }
  updateTattooDeal(id: string, payload: Partial<TattooDeal>) { return this.http.put(`${this.api}/admin/tattoo-deals/${id}`, payload); }
  disableTattooDeal(id: string) { return this.http.delete(`${this.api}/admin/tattoo-deals/${id}`); }
}

export interface InstagramAdminItem {
  isReel: boolean;
  id: string;
  mediaUrl: string;
  thumbnailUrl?: string;
  mediaType: string;
  caption: string;
  permalink: string;
  visible: boolean;
  pinned: boolean;
  displayOrder: number;
  syncedAt: string;
}
