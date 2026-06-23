"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import Link from "next/link";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/authStore";
import { OrderDto } from "@/types/orders";
import { Navbar } from "@/components/Navbar";
import Footer from "@/components/Footer";
import { TicketCard } from "@/components/TicketCard";

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString("en-IN", {
    weekday: "short",
    day: "numeric",
    month: "long",
    year: "numeric",
  });
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString("en-IN", {
    day: "numeric",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function statusColors(status: string) {
  switch (status) {
    case "Confirmed": return "bg-green-100 text-green-700";
    case "Pending": return "bg-amber-100 text-amber-700";
    case "Cancelled":
    case "Expired": return "bg-gray-100 text-gray-500";
    case "Failed": return "bg-red-100 text-red-600";
    default: return "bg-gray-100 text-gray-500";
  }
}

export default function OrderConfirmationPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  // Zustand-persist hydrates asynchronously from localStorage. We must wait
  // for at least one client-side render before trusting `isAuthenticated`,
  // otherwise a logged-in user is immediately redirected to /auth/login.
  const [hydrated, setHydrated] = useState(false);
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    if (!isAuthenticated) {
      router.replace(`/auth/login?returnUrl=/orders/${id}/confirmation`);
      return;
    }
    api
      .get<OrderDto>(`/api/orders/${id}`)
      .then((res) => setOrder(res.data))
      .catch(() => setError("Order not found or you don't have access to it."))
      .finally(() => setLoading(false));
  }, [id, isAuthenticated, hydrated, router]);

  if (loading) {
    return (
      <>
        <Navbar />
        <div className="min-h-screen flex items-center justify-center">
          <div className="text-gray-400 text-sm animate-pulse">Loading order details…</div>
        </div>
        <Footer />
      </>
    );
  }

  if (error || !order) {
    return (
      <>
        <Navbar />
        <div className="min-h-screen flex items-center justify-center px-4">
          <div className="text-center">
            <p className="text-red-500 mb-4">{error ?? "Something went wrong."}</p>
            <Link href="/events" className="text-indigo-600 text-sm hover:underline">
              Browse Events
            </Link>
          </div>
        </div>
        <Footer />
      </>
    );
  }

  const shortId = order.id.slice(0, 8).toUpperCase();
  const isConfirmed = order.status === "Confirmed";

  return (
    <>
    <Navbar />
    <main className="min-h-screen bg-gray-50 py-12 px-4">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="text-center mb-8">
          {isConfirmed ? (
            <>
              <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <svg className="w-8 h-8 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <h1 className="text-2xl font-bold text-gray-900 mb-1">Booking Confirmed!</h1>
              <p className="text-gray-500 text-sm">
                Your tickets are booked. See you there!
              </p>
            </>
          ) : (
            <>
              <div className="w-16 h-16 bg-amber-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <svg className="w-8 h-8 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v2m0 4h.01M12 3a9 9 0 100 18A9 9 0 0012 3z" />
                </svg>
              </div>
              <h1 className="text-2xl font-bold text-gray-900 mb-1">Order #{shortId}</h1>
              <p className="text-gray-500 text-sm">
                Status:{" "}
                <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${statusColors(order.status)}`}>
                  {order.status}
                </span>
              </p>
            </>
          )}
        </div>

        {/* QR Ticket — only when confirmed */}
        {isConfirmed && (
          <div className="mb-6">
            <TicketCard order={order} />
          </div>
        )}

        {/* Order Card */}
        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden mb-4">
          {/* Order ID + Status */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-50">
            <div>
              <p className="text-xs text-gray-400 uppercase tracking-wide font-medium">Order ID</p>
              <p className="font-mono font-semibold text-gray-800">#{shortId}</p>
            </div>
            <span className={`px-3 py-1 rounded-full text-xs font-semibold ${statusColors(order.status)}`}>
              {order.status}
            </span>
          </div>

          {/* Event Info */}
          <div className="px-6 py-4 border-b border-gray-50">
            <p className="text-xs text-gray-400 uppercase tracking-wide font-medium mb-2">Event</p>
            <p className="font-semibold text-gray-900">{order.eventTitle}</p>
            <p className="text-sm text-gray-500 mt-0.5">{order.eventVenue}</p>
            <p className="text-sm text-gray-400 mt-0.5">
              {formatDate(order.eventStartDate)}
            </p>
          </div>

          {/* Tickets */}
          <div className="px-6 py-4 border-b border-gray-50">
            <p className="text-xs text-gray-400 uppercase tracking-wide font-medium mb-3">Tickets</p>
            <div className="space-y-2">
              {order.items.map((item) => (
                <div key={item.id} className="flex justify-between text-sm">
                  <span className="text-gray-700">
                    {item.tierName}
                    <span className="text-gray-400 ml-1">× {item.quantity}</span>
                  </span>
                  <span className="font-medium text-gray-800">
                    ₹{item.lineTotal.toLocaleString("en-IN")}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Totals */}
          <div className="px-6 py-4 border-b border-gray-50 space-y-1.5">
            <div className="flex justify-between text-sm text-gray-500">
              <span>Subtotal</span>
              <span>₹{order.subTotal.toLocaleString("en-IN")}</span>
            </div>
            <div className="flex justify-between text-sm text-gray-500">
              <span>Booking Fee</span>
              <span>₹{order.bookingFee.toLocaleString("en-IN", { minimumFractionDigits: 2 })}</span>
            </div>
            <div className="flex justify-between font-semibold text-gray-900 pt-1 border-t border-gray-100">
              <span>Grand Total</span>
              <span>₹{order.grandTotal.toLocaleString("en-IN")}</span>
            </div>
          </div>

          {/* Customer */}
          <div className="px-6 py-4 border-b border-gray-50">
            <p className="text-xs text-gray-400 uppercase tracking-wide font-medium mb-2">Customer</p>
            <p className="text-sm font-medium text-gray-800">{order.customerName}</p>
            <p className="text-sm text-gray-500">{order.customerEmail}</p>
            <p className="text-sm text-gray-500">{order.customerPhone}</p>
          </div>

          {/* Payment IDs (if present) */}
          {order.razorpayOrderId && (
            <div className="px-6 py-4 border-b border-gray-50">
              <p className="text-xs text-gray-400 uppercase tracking-wide font-medium mb-2">Payment Reference</p>
              <p className="text-xs font-mono text-gray-600 break-all">{order.razorpayOrderId}</p>
            </div>
          )}

          {/* Timeline */}
          <div className="px-6 py-4">
            <p className="text-xs text-gray-400 uppercase tracking-wide font-medium mb-2">Booked On</p>
            <p className="text-sm text-gray-600">{formatDateTime(order.createdAt)}</p>
          </div>
        </div>

        {/* Actions */}
        <div className="flex flex-col sm:flex-row gap-3">
          <Link
            href="/orders"
            className="flex-1 text-center bg-indigo-600 hover:bg-indigo-700 text-white font-medium py-3 rounded-xl text-sm transition-colors"
          >
            View My Orders
          </Link>
          <Link
            href="/events"
            className="flex-1 text-center bg-white border border-gray-200 hover:bg-gray-50 text-gray-700 font-medium py-3 rounded-xl text-sm transition-colors"
          >
            Browse More Events
          </Link>
        </div>
      </div>
    </main>
    <Footer />
    </>
  );
}
