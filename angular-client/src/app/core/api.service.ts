import { PortfolioItem } from './media-settings.service';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Consultation, ConsultationStatus, TattooDeal } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly api = `${environment.apiBaseUrl}/api`;

  constructor(private http: HttpClient) {}

  getPortfolio() { return this.http.get<{ enabled: boolean; items: PortfolioItem[] }>(`${this.api}/portfolio`); }
  getInstagramMedia(key: string) {
    return this.http.get<{ configured: boolean; items: InstagramAdminItem[] }>(`${this.api}/portfolio/manage`, { headers: { 'X-Media-Admin-Key': key } });
  }
  syncInstagram(key: string) {
    return this.http.post<{ synced: number }>(`${this.api}/portfolio/sync`, {}, { headers: { 'X-Media-Admin-Key': key } });
  }
  updateInstagramMedia(key: string, item: InstagramAdminItem) {
    return this.http.patch(`${this.api}/portfolio/manage/${encodeURIComponent(item.id)}`, {
      visible: item.visible, pinned: item.pinned, displayOrder: item.displayOrder
    }, { headers: { 'X-Media-Admin-Key': key } });
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
