'use client';

import { useEffect, useState, useCallback } from 'react';
import Link from 'next/link';
import { EventSummary, Category, PagedResult } from '@/types/events';
import { Navbar } from '@/components/Navbar';
import Footer from '@/components/Footer';

const API = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5001';

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-IN', {
    day: 'numeric', month: 'short', year: 'numeric',
  });
}

function StatusBadge({ status }: { status: string }) {
  const colors: Record<string, string> = {
    Published: 'bg-green-100 text-green-700',
    Draft: 'bg-yellow-100 text-yellow-700',
    Cancelled: 'bg-red-100 text-red-700',
    Completed: 'bg-gray-100 text-gray-500',
  };
  return (
    <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${colors[status] ?? 'bg-gray-100 text-gray-500'}`}>
      {status}
    </span>
  );
}

export default function EventsPage() {
  const [events, setEvents] = useState<EventSummary[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [loading, setLoading] = useState(true);

  const fetchEvents = useCallback(async () => {
    setLoading(true);
    const params = new URLSearchParams({
      pageNumber: String(page),
      pageSize: '12',
      status: 'Published',
    });
    if (search) params.set('searchTerm', search);
    if (categoryId) params.set('categoryId', categoryId);

    const res = await fetch(`${API}/api/events?${params}`);
    if (res.ok) {
      const data: PagedResult<EventSummary> = await res.json();
      setEvents(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    }
    setLoading(false);
  }, [page, search, categoryId]);

  useEffect(() => {
    fetch(`${API}/api/categories?pageSize=50`)
      .then(r => r.json())
      .then((d: PagedResult<Category>) => setCategories(d.items));
  }, []);

  useEffect(() => { fetchEvents(); }, [fetchEvents]);

  function handleSearch(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setPage(1);
    fetchEvents();
  }

  return (
    <>
    <Navbar />
    <main className="min-h-screen px-6 py-12 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-1">Browse Events</h1>
        <p className="text-gray-500">{totalCount} events available</p>
      </div>

      {/* Filters */}
      <form onSubmit={handleSearch} className="flex flex-wrap gap-3 mb-8">
        <input
          type="text"
          placeholder="Search events..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="border border-gray-200 rounded-xl px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 w-64"
        />
        <select
          value={categoryId}
          onChange={e => { setCategoryId(e.target.value); setPage(1); }}
          className="border border-gray-200 rounded-xl px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
        >
          <option value="">All Categories</option>
          {categories.map(c => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>
        <button
          type="submit"
          className="bg-indigo-600 text-white px-5 py-2 rounded-xl text-sm font-medium hover:bg-indigo-700"
        >
          Search
        </button>
        {(search || categoryId) && (
          <button
            type="button"
            onClick={() => { setSearch(''); setCategoryId(''); setPage(1); }}
            className="text-sm text-gray-500 hover:text-gray-700 px-3"
          >
            Clear
          </button>
        )}
      </form>

      {/* Grid */}
      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="rounded-2xl border border-gray-100 overflow-hidden shadow-sm animate-pulse">
              <div className="h-44 bg-gray-100" />
              <div className="p-4 space-y-2">
                <div className="h-4 bg-gray-100 rounded w-3/4" />
                <div className="h-3 bg-gray-100 rounded w-1/2" />
              </div>
            </div>
          ))}
        </div>
      ) : events.length === 0 ? (
        <div className="text-center py-20 text-gray-400">
          <p className="text-lg">No events found.</p>
          <p className="text-sm mt-1">Try adjusting your filters.</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {events.map(event => (
            <Link
              key={event.id}
              href={`/events/${event.id}`}
              className="group rounded-2xl border border-gray-100 overflow-hidden shadow-sm hover:shadow-md transition-shadow"
            >
              <div className="h-44 bg-gray-100 overflow-hidden">
                {event.bannerImageUrl ? (
                  <img
                    src={event.bannerImageUrl}
                    alt={event.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                  />
                ) : (
                  <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-indigo-50 to-purple-50 text-indigo-300 text-4xl">
                    🎟️
                  </div>
                )}
              </div>
              <div className="p-4">
                <div className="flex items-start justify-between gap-2 mb-1">
                  <h3 className="font-semibold text-sm leading-tight line-clamp-2 group-hover:text-indigo-600">
                    {event.title}
                  </h3>
                  <StatusBadge status={event.status} />
                </div>
                <p className="text-xs text-gray-500 mb-1">{event.venueName}, {event.venueCity}</p>
                <p className="text-xs text-gray-400 mb-3">{formatDate(event.startDate)}</p>
                <div className="flex items-center justify-between">
                  <span className="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded-full">
                    {event.categoryName}
                  </span>
                  <span className="text-sm font-bold text-indigo-600">
                    {event.minPrice > 0 ? `₹${event.minPrice.toLocaleString('en-IN')}` : 'Free'}
                  </span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-center gap-2 mt-10">
          <button
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={page === 1}
            className="px-4 py-2 rounded-xl border text-sm disabled:opacity-40 hover:bg-gray-50"
          >
            ← Prev
          </button>
          <span className="px-4 py-2 text-sm text-gray-500">
            Page {page} of {totalPages}
          </span>
          <button
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={page === totalPages}
            className="px-4 py-2 rounded-xl border text-sm disabled:opacity-40 hover:bg-gray-50"
          >
            Next →
          </button>
        </div>
      )}
    </main>
    <Footer />
    </>
  );
}
