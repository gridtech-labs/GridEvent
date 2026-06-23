"use client";

import { useEffect, useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { api } from "@/lib/api";
import { useAuthStore } from "@/store/authStore";
import { OrderDto, PagedResult } from "@/types/orders";
import { Navbar } from "@/components/Navbar";
import Footer from "@/components/Footer";

const PAGE_SIZE = 10;

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

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString("en-IN", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
}

export default function MyOrdersPage() {
  const router = useRouter();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  const [orders, setOrders] = useState<OrderDto[]>([]);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchOrders = useCallback(
    async (pageNum: number) => {
      setLoading(true);
      setError(null);
      try {
        const { data } = await api.get<PagedResult<OrderDto>>("/api/orders/my", {
          params: { pageNumber: pageNum, pageSize: PAGE_SIZE },
        });
        setOrders(data.items);
        setTotalPages(data.totalPages);
        setTotalCount(data.totalCount);
      } catch {
        setError("Failed to load orders. Please try again.");
      } finally {
        setLoading(false);
      }
    },
    []
  );

  useEffect(() => {
    if (!isAuthenticated) {
      router.replace("/auth/login?returnUrl=/orders");
      return;
    }
    fetchOrders(page);
  }, [isAuthenticated, page, fetchOrders, router]);

  return (
    <>
    <Navbar />
    <main className="min-h-screen bg-gray-50 py-10 px-4">
      <div className="max-w-3xl mx-auto">
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <div>
            <h1 className="text-xl font-bold text-gray-900">My Orders</h1>
            {!loading && totalCount > 0 && (
              <p className="text-sm text-gray-400 mt-0.5">{totalCount} booking{totalCount !== 1 ? "s" : ""} found</p>
            )}
          </div>
          <Link
            href="/events"
            className="text-sm text-indigo-600 hover:underline"
          >
            Browse Events
          </Link>
        </div>

        {/* Error */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-xl px-4 py-3 text-sm text-red-600 mb-4">
            {error}
          </div>
        )}

        {/* Loading skeleton */}
        {loading && (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="bg-white rounded-2xl border border-gray-100 p-5 animate-pulse">
                <div className="flex items-center justify-between">
                  <div className="space-y-2">
                    <div className="h-4 bg-gray-100 rounded w-48" />
                    <div className="h-3 bg-gray-50 rounded w-32" />
                  </div>
                  <div className="h-6 bg-gray-100 rounded-full w-20" />
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Empty state */}
        {!loading && !error && orders.length === 0 && (
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-12 text-center">
            <div className="w-14 h-14 bg-indigo-50 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-7 h-7 text-indigo-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 10.5V6a3.75 3.75 0 10-7.5 0v4.5m11.356-1.993l1.263 12c.07.665-.45 1.243-1.119 1.243H4.25a1.125 1.125 0 01-1.12-1.243l1.264-12A1.125 1.125 0 015.513 7.5h12.974c.576 0 1.059.435 1.119 1.007z" />
              </svg>
            </div>
            <h2 className="font-semibold text-gray-700 mb-1">No bookings yet</h2>
            <p className="text-sm text-gray-400 mb-6">Discover events and grab your tickets!</p>
            <Link
              href="/events"
              className="inline-block bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-6 py-2.5 rounded-xl transition-colors"
            >
              Browse Events
            </Link>
          </div>
        )}

        {/* Orders list */}
        {!loading && orders.length > 0 && (
          <div className="space-y-3">
            {orders.map((order) => {
              const shortId = order.id.slice(0, 8).toUpperCase();
              const totalQty = order.items.reduce((s, i) => s + i.quantity, 0);
              return (
                <Link
                  key={order.id}
                  href={`/orders/${order.id}/confirmation`}
                  className="block bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-md hover:border-indigo-100 transition-all p-5"
                >
                  <div className="flex items-start justify-between gap-4">
                    <div className="min-w-0">
                      <div className="flex items-center gap-2 flex-wrap mb-1">
                        <span className="font-mono text-xs text-gray-400">#{shortId}</span>
                        <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors(order.status)}`}>
                          {order.status}
                        </span>
                      </div>
                      <p className="font-semibold text-gray-900 truncate">{order.eventTitle}</p>
                      <p className="text-sm text-gray-400 mt-0.5">{order.eventVenue}</p>
                      <div className="flex items-center gap-3 mt-2 text-xs text-gray-400">
                        <span>{formatDate(order.eventStartDate)}</span>
                        <span>·</span>
                        <span>{totalQty} ticket{totalQty !== 1 ? "s" : ""}</span>
                        <span>·</span>
                        <span>Booked {formatDate(order.createdAt)}</span>
                      </div>
                    </div>
                    <div className="text-right shrink-0">
                      <p className="font-bold text-gray-900">₹{order.grandTotal.toLocaleString("en-IN")}</p>
                      <p className="text-xs text-indigo-500 mt-1">View →</p>
                    </div>
                  </div>
                </Link>
              );
            })}
          </div>
        )}

        {/* Pagination */}
        {!loading && totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 mt-8">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-4 py-2 rounded-lg text-sm border border-gray-200 text-gray-600 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              Previous
            </button>
            <span className="text-sm text-gray-500">
              Page {page} of {totalPages}
            </span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-4 py-2 rounded-lg text-sm border border-gray-200 text-gray-600 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              Next
            </button>
          </div>
        )}
      </div>
    </main>
    <Footer />
    </>
  );
}
