"use client";

import { useEffect, useState } from "react";
import { useCityStore } from "@/store/cityStore";
import { CityOption } from "@/types/cities";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5175";

export function CitySelectOverlay() {
  const { city, setCity } = useCityStore();
  const [cities, setCities] = useState<CityOption[]>([]);
  const [show, setShow] = useState(false);
  const [hydrated, setHydrated] = useState(false);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  // Wait for zustand hydration before checking city
  useEffect(() => {
    setHydrated(true);
  }, []);

  useEffect(() => {
    if (!hydrated) return;
    if (!city) {
      setShow(true);
      fetch(`${API}/api/cities`)
        .then((r) => (r.ok ? r.json() : []))
        .then((data) => setCities(data))
        .catch(() => setCities([]))
        .finally(() => setLoading(false));
    }
  }, [hydrated, city]);

  const handleSelect = (c: CityOption) => {
    setCity(c);
    setShow(false);
  };

  const handleSkip = () => setShow(false);

  if (!show || city) return null;

  const filtered = search
    ? cities.filter((c) => c.name.toLowerCase().includes(search.toLowerCase()) || c.state.toLowerCase().includes(search.toLowerCase()))
    : cities;

  return (
    <div className="fixed inset-0 z-[200] bg-white overflow-auto">
      {/* Header */}
      <div className="border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <span className="text-xl font-black">
          Grid<span className="text-violet-600">Tickets</span>
        </span>
        <button
          onClick={handleSkip}
          className="text-sm text-gray-500 hover:text-gray-800 transition-colors flex items-center gap-1"
        >
          Browse all events
          <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M13 7l5 5m0 0l-5 5m5-5H6" />
          </svg>
        </button>
      </div>

      <div className="max-w-4xl mx-auto px-6 py-12">
        {/* Heading */}
        <div className="mb-8">
          <h1 className="text-3xl sm:text-4xl font-black text-gray-900 mb-2">
            Explore events around you
          </h1>
          <p className="text-gray-500">Select your city to discover live events near you.</p>
        </div>

        {/* Search */}
        <div className="relative max-w-sm mb-8">
          <svg className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search city of your choice"
            className="w-full pl-9 pr-4 py-2.5 rounded-xl text-sm border border-gray-200 focus:outline-none focus:ring-2 focus:ring-violet-400 focus:border-transparent"
          />
        </div>

        {/* Loading */}
        {loading && (
          <div className="flex items-center justify-center py-20">
            <div className="w-8 h-8 border-4 border-violet-600 border-t-transparent rounded-full animate-spin" />
          </div>
        )}

        {/* No cities configured */}
        {!loading && cities.length === 0 && (
          <div className="text-center py-20">
            <div className="text-6xl mb-4">🏙️</div>
            <p className="text-gray-500 font-medium">No cities available yet.</p>
            <p className="text-gray-400 text-sm mt-1 mb-6">Cities will appear here once an admin adds them.</p>
            <button
              onClick={handleSkip}
              className="inline-flex items-center gap-2 bg-violet-600 text-white font-semibold px-6 py-2.5 rounded-full hover:bg-violet-700 transition-colors text-sm"
            >
              Browse all events anyway
              <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M13 7l5 5m0 0l-5 5m5-5H6" />
              </svg>
            </button>
          </div>
        )}

        {/* City grid */}
        {!loading && filtered.length > 0 && (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
            {filtered.map((c) => (
              <button
                key={c.id}
                onClick={() => handleSelect(c)}
                className="group relative rounded-2xl overflow-hidden bg-gray-100 hover:shadow-xl transition-all duration-300 hover:-translate-y-1 focus:outline-none focus:ring-2 focus:ring-violet-500"
                style={{ aspectRatio: "1 / 1" }}
              >
                {c.imageUrl ? (
                  <img
                    src={c.imageUrl}
                    alt={c.name}
                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  />
                ) : (
                  <div className="w-full h-full bg-gradient-to-br from-violet-500 to-purple-700 flex items-center justify-center">
                    <span className="text-4xl opacity-40">🏙️</span>
                  </div>
                )}
                {/* Gradient overlay */}
                <div className="absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent group-hover:from-violet-900/70 transition-all duration-300" />
                {/* Text */}
                <div className="absolute bottom-0 left-0 right-0 p-3 text-left">
                  <p className="text-white font-bold text-sm leading-tight">{c.name}</p>
                  <p className="text-white/60 text-xs">{c.state}</p>
                </div>
              </button>
            ))}
          </div>
        )}

        {/* No results from search */}
        {!loading && cities.length > 0 && filtered.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-400">No cities match &ldquo;{search}&rdquo;</p>
          </div>
        )}
      </div>

      {/* Indian city skyline illustration at bottom */}
      <div className="w-full overflow-hidden opacity-5 pointer-events-none select-none text-center text-9xl pb-8">
        🏛️🕌🏯🕍🏰
      </div>
    </div>
  );
}
