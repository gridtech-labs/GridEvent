export interface OrderItemRequest {
  ticketTierId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  eventId: string;
  items: OrderItemRequest[];
  customerName: string;
  customerEmail: string;
  customerPhone: string;
}

export interface OrderItemDto {
  id: string;
  ticketTierId: string;
  tierName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  eventId: string;
  eventTitle: string;
  eventVenue: string;
  eventStartDate: string;
  status: string;
  subTotal: number;
  bookingFee: number;
  grandTotal: number;
  razorpayOrderId?: string;
  bookingReference?: string;
  expiresAt: string;
  createdAt: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  items: OrderItemDto[];
}

// Re-export the canonical PagedResult from events.ts to keep one definition
export type { PagedResult } from './events';
