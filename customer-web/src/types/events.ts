export interface TicketTier {
  id: string;
  name: string;
  description?: string;
  price: number;
  totalQuantity: number;
  soldQuantity: number;
  availableQuantity: number;
  eventId: string;
}

export interface EventSummary {
  id: string;
  title: string;
  bannerImageUrl?: string;
  startDate: string;
  endDate: string;
  status: string;
  venueName: string;
  venueCity: string;
  categoryName: string;
  minPrice: number;
  collectionId?: string;
  collectionName?: string;
  ticketTiers?: TicketTier[];
}

export interface EventDetail extends EventSummary {
  description: string;
  venueId: string;
  venueAddress: string;
  venueState: string;
  venueCapacity: number;
  categoryId: string;
  categorySlug?: string;
  createdAt: string;
  ticketTiers: TicketTier[];
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  description?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
