export type ConsultationStatus = 'New' | 'Contacted' | 'Booked' | 'Declined';

export interface TattooDeal {
  id: string;
  title: string;
  style: string;
  placement: string;
  size: string;
  originalPrice?: number;
  discountedPrice: number;
  description: string;
  referenceImageUrl?: string;
  isAvailable: boolean;
  isFeatured: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Consultation {
  id: string;
  name: string;
  email: string;
  phoneNumber: string;
  preferredArtist: string;
  style: string;
  placement: string;
  size: string;
  budget?: string;
  description: string;
  preferredDays: { day: string }[];
  status: ConsultationStatus;
  createdAt: string;
}
