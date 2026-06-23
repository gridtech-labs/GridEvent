"use client";

import { useEffect, useState, useCallback } from "react";
import Link from "next/link";
import { EventSummary } from "@/types/events";
import { Collection } from "@/types/collections";
import { useAuthStore } from "@/store/authStore";
import { useCityStore } from "@/store/cityStore";
import { Navbar } from "@/components/Navbar";


const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5175";

function formatDate(iso: string) {
  const d = new Date(iso);
  return {
    short: d.toLocaleDateString("en-IN", { weekday: "short", day: "numeric", month: "short" }),
    time: d.toLocaleTimeString("en-IN", { hour: "2-digit", minute: "2-digit" }),
  };
}

/* ─── Event Card ─────────────────────────────────────────────────────────────── */
function EventCard({ event }: { event: EventSummary }) {
  const { short, time } = formatDate(event.startDate);
  const tiers = event.ticketTiers ?? [];
  const minPrice = tiers.length > 0 ? Math.min(...tiers.map((t) => t.price)) : event.minPrice;
  const isSoldOut = tiers.length > 0 && tiers.every((t) => t.availableQuantity === 0);

  return (
    <Link href={`/events/${event.id}`} className="group block w-full">
      {/* Poster */}
      <div className="w-full rounded-xl overflow-hidden bg-gray-200" style={{ aspectRatio: "3/4" }}>
        {event.bannerImageUrl ? (
          <img
            src={event.bannerImageUrl}
            alt={event.title}
            className="w-full h-full object-cover group-hover:scale-[1.03] transition-transform duration-300"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center bg-gradient-to-br from-violet-100 to-purple-200">
            <span className="text-5xl opacity-20">🎭</span>
          </div>
        )}
      </div>

      {/* Info — mirrors District: flex-col p-[12px] gap-[2px] */}
      <div className="flex flex-col items-start p-3 gap-[2px]">
        {/* dds-text-sm dds-font-medium color:var(--color-sub-title-text) */}
        <span className="text-sm font-medium text-gray-500">
          {short}, {time}
        </span>
        {/* dds-text-lg dds-font-semibold dds-tracking-tight dds-line-clamp-2 line-height:22px */}
        <h5 className="text-lg font-semibold tracking-tight line-clamp-2 leading-[22px] text-gray-900 m-0 w-full group-hover:text-violet-700 transition-colors">
          {event.title}
        </h5>
        {/* dds-text-sm dds-font-medium dds-text-primary dds-line-clamp-1 */}
        <span className="text-sm font-medium text-gray-900 line-clamp-1 w-full">
          {event.venueName}{event.venueCity ? `, ${event.venueCity}` : ""}
        </span>
        {/* dds-text-sm dds-font-medium dds-text-secondary dds-line-clamp-1 */}
        <span className={`text-sm font-medium line-clamp-1 ${isSoldOut ? "text-red-400" : "text-gray-500"}`}>
          {isSoldOut ? "Sold Out" : minPrice === 0 ? "Free" : `₹${minPrice.toLocaleString("en-IN")} onwards`}
        </span>
      </div>
    </Link>
  );
}

/* ─── Collection Section ─────────────────────────────────────────────────────── */
function Section({
  title,
  description,
  seeAllHref,
  events,
}: {
  title: string;
  description?: string;
  seeAllHref: string;
  events: EventSummary[];
}) {
  if (events.length === 0) return null;

  return (
    <section className="mb-10">
      {/*
       * Title lives inside max-w-7xl mx-auto so on wide screens the
       * auto-margin pushes it inward — matching District's section indent.
       */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-10 flex items-end justify-between mb-4">
        <div>
          <h2 className="text-gray-900 tracking-tight" style={{ fontSize: "1.75rem", fontWeight: 600 }}>{title}</h2>
          {description && <p className="text-sm text-gray-400 mt-0.5 font-normal">{description}</p>}
        </div>
        <Link href={seeAllHref} className="text-sm font-semibold text-violet-600 hover:underline shrink-0 ml-4">
          See all →
        </Link>
      </div>

      <div className="dist-scroll-row flex overflow-x-auto no-scrollbar" style={{ gap: "12px" }}>
        {events.map((ev) => (
          <div key={ev.id} className="flex-shrink-0 snap-start dist-card-w">
            <EventCard event={ev} />
          </div>
        ))}
        {/* trailing spacer so last card doesn't sit against right edge on end-scroll */}
        <div className="flex-shrink-0 dist-trail-w" />
      </div>
    </section>
  );
}

/* ─── Page ───────────────────────────────────────────────────────────────────── */
export default function HomePage() {
  const { isAuthenticated } = useAuthStore();
  const { city } = useCityStore();
  const [events, setEvents] = useState<EventSummary[]>([]);
  const [collections, setCollections] = useState<Collection[]>([]);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const p = new URLSearchParams({ pageSize: "60", status: "Published" });
      if (city?.name) p.set("city", city.name);
      const [evRes, colRes] = await Promise.all([
        fetch(`${API}/api/events?${p}`),
        fetch(`${API}/api/collections`),
      ]);
      if (evRes.ok) setEvents((await evRes.json()).items ?? []);
      if (colRes.ok) setCollections(await colRes.json());
    } finally {
      setLoading(false);
    }
  }, [city]);

  useEffect(() => { fetchData(); }, [fetchData]);

  const filtered = search
    ? events.filter((e) =>
        e.title.toLowerCase().includes(search.toLowerCase()) ||
        e.venueName.toLowerCase().includes(search.toLowerCase()) ||
        e.venueCity.toLowerCase().includes(search.toLowerCase())
      )
    : events;

  const byCol = new Map<string, EventSummary[]>();
  const uncollected: EventSummary[] = [];
  for (const ev of filtered) {
    if (ev.collectionId) {
      if (!byCol.has(ev.collectionId)) byCol.set(ev.collectionId, []);
      byCol.get(ev.collectionId)!.push(ev);
    } else {
      uncollected.push(ev);
    }
  }
  const hasColEvents = collections.some((c) => (byCol.get(c.id)?.length ?? 0) > 0);

  return (
    <div className="min-h-screen" style={{ backgroundColor: "#f5f3ff" }}>
      <Navbar showSearch search={search} onSearch={setSearch} />

      <main className="pt-8 pb-20">
        {loading ? (
          <div className="flex items-center justify-center py-40">
            <div className="w-8 h-8 border-4 border-violet-600 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : filtered.length === 0 ? (
          <div className="max-w-7xl mx-auto px-4 text-center py-40">
            <p className="text-5xl mb-4">🎭</p>
            <p className="text-gray-500 font-medium">No events found</p>
            <p className="text-gray-400 text-sm mt-1">
              {search ? "Try a different search" : city ? `No events in ${city.name} yet` : "Check back soon"}
            </p>
          </div>
        ) : (
          <>
            {collections.map((col) => (
              <Section
                key={col.id}
                title={col.name}
                description={col.description ?? undefined}
                seeAllHref={`/events?collection=${col.id}`}
                events={byCol.get(col.id) ?? []}
              />
            ))}
            {(!hasColEvents || uncollected.length > 0) && (
              <Section
                title="All Events"
                seeAllHref="/events"
                events={hasColEvents ? uncollected : filtered}
              />
            )}
          </>
        )}
      </main>

      <footer className="bg-gray-950 text-gray-400">
        <div className="max-w-7xl mx-auto px-4 py-10">
          <div className="flex flex-col sm:flex-row items-start justify-between gap-8">
            <div>
              <p className="text-white text-base font-black mb-1">
                Grid<span className="text-violet-400">Tickets</span>
              </p>
              <p className="text-sm text-gray-500 max-w-xs">
                Book live events — music, comedy, art, theatre and more.
              </p>
            </div>
            <div className="flex gap-10 text-sm">
              <div className="space-y-2">
                <p className="text-white font-semibold">Events</p>
                <Link href="/events" className="block hover:text-white transition-colors">Browse All</Link>
              </div>
              <div className="space-y-2">
                <p className="text-white font-semibold">Account</p>
                {isAuthenticated ? (
                  <Link href="/orders" className="block hover:text-white transition-colors">My Orders</Link>
                ) : (
                  <>
                    <Link href="/auth/login" className="block hover:text-white transition-colors">Login</Link>
                    <Link href="/auth/register" className="block hover:text-white transition-colors">Sign Up</Link>
                  </>
                )}
              </div>
            </div>
          </div>
          <div className="border-t border-gray-800 mt-8 pt-5 text-xs text-gray-600 text-center">
            © {new Date().getFullYear()} GridTickets. All rights reserved.
          </div>
        </div>
      </footer>

      <style jsx global>{`
        /* scrollbar hide */
        .no-scrollbar::-webkit-scrollbar { display: none; }
        .no-scrollbar { -ms-overflow-style: none; scrollbar-width: none; }

        /* ─── Bleed carousel pattern ──────────────────────────────────────────
         * Bleed carousel: left padding must mirror the section title container
         * so first card and heading share the same left edge at every breakpoint.
         *
         * Title wrapper:  max-w-7xl mx-auto  +  px-4 / sm:px-6 / lg:px-10
         * Below max-w-7xl (< 1280px) mx-auto = 0, so left edge = inner padding.
         * Above 1280px the container centers, left edge = (100vw-80rem)/2 + lg-padding.
         */
        .dist-scroll-row { padding-left: 1rem; }     /* px-4  = 16px  */
        .dist-trail-w    { width:         1rem; }

        @media (min-width: 640px) {
          .dist-scroll-row { padding-left: 1.5rem; }  /* sm:px-6 = 24px */
          .dist-trail-w    { width:         1.5rem; }
        }

        @media (min-width: 1024px) {
          .dist-scroll-row { padding-left: 2.5rem; }  /* lg:px-10 = 40px */
          .dist-trail-w    { width:         2.5rem; }
        }

        @media (min-width: 1280px) {
          .dist-scroll-row {
            padding-left: calc((100vw - 80rem) / 2 + 2.5rem);
          }
          .dist-trail-w {
            width: calc((100vw - 80rem) / 2 + 2.5rem);
          }
        }

        /* ─── Card widths ─────────────────────────────────────────────────────
         * Mobile  (<640)  : 65vw  → ~1.5 cards visible
         * Tablet  (640+)  : 38vw  → ~2.5 cards visible
         * Desktop (1024+) : 230px → ~4 cards in ~1024px viewport
         * Wide    (1280+) : 295px → ~4 cards in ~1280px viewport
         * XL      (1536+) : 330px → ~4 cards in ~1536px viewport
         */
        .dist-card-w { width: calc((100vw - 1rem) * 0.65); }

        @media (min-width: 640px) {
          .dist-card-w { width: calc((100vw - 1.5rem) * 0.38); }
        }

        @media (min-width: 1024px) {
          .dist-card-w { width: 230px; }
        }

        @media (min-width: 1280px) {
          .dist-card-w { width: 295px; }
        }

        @media (min-width: 1536px) {
          .dist-card-w { width: 330px; }
        }
      `}</style>
    </div>
  );
}
