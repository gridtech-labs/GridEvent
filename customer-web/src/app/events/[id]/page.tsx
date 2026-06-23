import Link from "next/link";
import { notFound } from "next/navigation";
import { EventDetail } from "@/types/events";
import { Navbar } from "@/components/Navbar";
import Footer from "@/components/Footer";
import { ShareButton } from "@/components/ShareButton";
import { InterestButton } from "@/components/InterestButton";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5175";

async function getEvent(id: string): Promise<EventDetail | null> {
  const res = await fetch(`${API}/api/events/${id}`, { cache: "no-store" });
  if (!res.ok) return null;
  return res.json();
}

const WEEKDAYS = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
const MONTHS = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

function fmtDate(iso: string) {
  const d = new Date(iso);
  return `${WEEKDAYS[d.getDay()]} ${d.getDate()} ${MONTHS[d.getMonth()]} ${d.getFullYear()}`;
}

function fmtTime(iso: string) {
  return new Date(iso).toLocaleTimeString("en-IN", { hour: "2-digit", minute: "2-digit" });
}

function getDuration(startIso: string, endIso: string): string | null {
  const diffMs = new Date(endIso).getTime() - new Date(startIso).getTime();
  const hours = Math.floor(diffMs / 3_600_000);
  const mins = Math.floor((diffMs % 3_600_000) / 60_000);
  if (hours <= 0 || hours >= 24) return null;
  if (mins === 0) return `${hours} ${hours === 1 ? "Hour" : "Hours"}`;
  return `${hours}h ${mins}m`;
}

/* ─── Icon row ────────────────────────────────────────────────────────────── */
function InfoRow({
  icon,
  children,
}: {
  icon: "calendar" | "clock" | "hourglass" | "pin";
  children: React.ReactNode;
}) {
  const svgClass = "w-[18px] h-[18px] shrink-0 text-gray-400";

  const iconEl =
    icon === "calendar" ? (
      <svg className={svgClass} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
    ) : icon === "clock" ? (
      <svg className={svgClass} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    ) : icon === "hourglass" ? (
      <svg className={svgClass} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M6 2h12v5l-6 5 6 5v5H6v-5l6-5-6-5V2z" />
      </svg>
    ) : (
      <svg className={svgClass} fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M17.657 16.657L13.414 20.9a2 2 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
      </svg>
    );

  return (
    <div className="flex items-start gap-3">
      <span className="mt-0.5">{iconEl}</span>
      <span className="text-sm text-gray-800 leading-snug">{children}</span>
    </div>
  );
}

/* ─── Page ────────────────────────────────────────────────────────────────── */
export default async function EventDetailPage({ params }: { params: { id: string } }) {
  const event = await getEvent(params.id);
  if (!event) notFound();

  const minPrice =
    event.ticketTiers.length > 0 ? Math.min(...event.ticketTiers.map((t) => t.price)) : 0;
  const isSoldOut =
    event.ticketTiers.length > 0 && event.ticketTiers.every((t) => t.availableQuantity === 0);
  const isBookable =
    event.status === "Published" && !isSoldOut && event.ticketTiers.length > 0;
  const duration = getDuration(event.startDate, event.endDate);
  const dateRange =
    new Date(event.startDate).toDateString() === new Date(event.endDate).toDateString()
      ? fmtDate(event.startDate)
      : `${fmtDate(event.startDate)} - ${fmtDate(event.endDate)}`;

  return (
    <div className="min-h-screen bg-white">
      <Navbar />

      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-10 py-6 pb-20">

        {/* ── Title row ── */}
        <div className="flex items-start justify-between gap-4 mb-5">
          <h1 className="text-2xl sm:text-3xl font-black text-gray-900 leading-tight tracking-tight">
            {event.title}
          </h1>
          <ShareButton title={event.title} />
        </div>

        {/* ── Two-column grid ── */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_340px] gap-8 lg:items-start">

          {/* ── LEFT ── */}
          <div>
            {/* Banner with nav arrows + dots */}
            <div
              className="relative rounded-2xl overflow-hidden bg-gray-100 w-full"
              style={{ aspectRatio: "16/9" }}
            >
              {event.bannerImageUrl ? (
                <img
                  src={event.bannerImageUrl}
                  alt={event.title}
                  className="w-full h-full object-cover"
                />
              ) : (
                <div className="w-full h-full bg-gradient-to-br from-violet-100 to-purple-200 flex items-center justify-center">
                  <span className="text-7xl opacity-20">🎭</span>
                </div>
              )}

              {/* Prev arrow */}
              <button className="absolute left-3 top-1/2 -translate-y-1/2 w-8 h-8 bg-white/80 rounded-full flex items-center justify-center shadow hover:bg-white transition-colors">
                <svg className="w-4 h-4 text-gray-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M15 19l-7-7 7-7" />
                </svg>
              </button>

              {/* Next arrow */}
              <button className="absolute right-3 top-1/2 -translate-y-1/2 w-8 h-8 bg-white/80 rounded-full flex items-center justify-center shadow hover:bg-white transition-colors">
                <svg className="w-4 h-4 text-gray-700" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                </svg>
              </button>

              {/* Dots */}
              <div className="absolute bottom-3 left-1/2 -translate-x-1/2 flex items-center gap-1.5">
                <span className="w-5 h-1.5 bg-white rounded-full" />
                <span className="w-1.5 h-1.5 bg-white/50 rounded-full" />
                <span className="w-1.5 h-1.5 bg-white/50 rounded-full" />
                <span className="w-1.5 h-1.5 bg-white/50 rounded-full" />
              </div>
            </div>

            {/* Tags + I'm Interested */}
            <div className="flex items-center justify-between mt-4 gap-3 flex-wrap">
              <div className="flex gap-2 flex-wrap">
                <span className="bg-gray-900 text-white text-xs font-semibold px-3 py-1.5 rounded-full">
                  {event.categoryName}
                </span>
                {event.collectionName && (
                  <span className="bg-gray-900 text-white text-xs font-semibold px-3 py-1.5 rounded-full">
                    {event.collectionName}
                  </span>
                )}
              </div>
              <InterestButton />
            </div>

            {/* About */}
            {event.description && (
              <div className="mt-7">
                <h2 className="text-lg font-bold text-gray-900 mb-3">About this Event</h2>
                <p className="text-gray-600 text-sm leading-relaxed whitespace-pre-line">
                  {event.description}
                </p>
              </div>
            )}

            {/* Venue */}
            <div className="mt-7 rounded-2xl border border-gray-100 bg-gray-50 p-5">
              <h3 className="font-bold text-gray-900 mb-3">Venue</h3>
              <p className="font-semibold text-gray-800">{event.venueName}</p>
              {event.venueAddress && (
                <p className="text-sm text-gray-500 mt-0.5">{event.venueAddress}</p>
              )}
              <p className="text-sm text-gray-500">
                {event.venueCity}, {event.venueState}
              </p>
            </div>
          </div>

          {/* ── RIGHT – Booking Card ── */}
          <div className="lg:sticky lg:top-20">
            <div className="rounded-2xl border border-gray-200 shadow-sm p-5 space-y-3.5">

              {/* Info rows */}
              <InfoRow icon="calendar">{dateRange}</InfoRow>
              <InfoRow icon="clock">{fmtTime(event.startDate)}</InfoRow>
              {duration && <InfoRow icon="hourglass">{duration}</InfoRow>}
              <InfoRow icon="pin">
                <span className="flex items-center gap-1 flex-wrap">
                  {event.venueName}: {event.venueCity}
                  <svg className="w-3.5 h-3.5 text-blue-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13 7l5 5m0 0l-5 5m5-5H6" />
                  </svg>
                </span>
              </InfoRow>

              <div className="border-t border-gray-100 pt-4">
                {/* Price */}
                <p className="text-2xl font-black text-gray-900">
                  {event.ticketTiers.length === 0
                    ? "No tickets"
                    : minPrice === 0
                    ? "Free"
                    : `₹${minPrice.toLocaleString("en-IN")} onwards`}
                </p>

                {/* Availability badge */}
                {isBookable && (
                  <p className="text-sm font-semibold text-green-600 mt-0.5">Available</p>
                )}
                {isSoldOut && (
                  <p className="text-sm font-semibold text-red-500 mt-0.5">Sold Out</p>
                )}
                {event.status !== "Published" && (
                  <p className="text-sm font-semibold text-gray-400 mt-0.5">{event.status}</p>
                )}

                {/* CTA */}
                <div className="mt-4">
                  {!isBookable ? (
                    <button
                      disabled
                      className="w-full bg-gray-100 text-gray-400 font-semibold py-3.5 rounded-xl text-sm cursor-not-allowed"
                    >
                      {isSoldOut ? "Sold Out" : "Not Available"}
                    </button>
                  ) : (
                    <Link
                      href={`/events/${event.id}/book`}
                      className="block w-full bg-rose-500 hover:bg-rose-600 active:bg-rose-700 text-white font-bold py-3.5 rounded-xl text-base text-center transition-colors"
                    >
                      Book Now
                    </Link>
                  )}
                </div>

                {/* Ticket tiers */}
                {event.ticketTiers.length > 0 && (
                  <div className="mt-5 space-y-2">
                    <p className="text-[11px] font-semibold text-gray-400 uppercase tracking-wider">
                      Ticket Tiers
                    </p>
                    {event.ticketTiers.map((tier) => (
                      <div
                        key={tier.id}
                        className="flex justify-between items-center py-2 border-b border-gray-50 last:border-0"
                      >
                        <div className="flex items-center gap-1.5 flex-wrap">
                          <span className="text-sm font-medium text-gray-800">{tier.name}</span>
                          {tier.availableQuantity === 0 && (
                            <span className="text-xs text-red-500 bg-red-50 px-1.5 py-0.5 rounded-full">
                              Sold out
                            </span>
                          )}
                          {tier.availableQuantity > 0 && tier.availableQuantity <= 10 && (
                            <span className="text-xs text-amber-600 bg-amber-50 px-1.5 py-0.5 rounded-full">
                              Only {tier.availableQuantity} left
                            </span>
                          )}
                        </div>
                        <span className="text-sm font-bold text-gray-900 shrink-0 ml-2">
                          ₹{tier.price.toLocaleString("en-IN")}
                        </span>
                      </div>
                    ))}
                  </div>
                )}

                <p className="text-xs text-gray-400 text-center mt-4">
                  Secure checkout · Instant confirmation
                </p>
              </div>
            </div>
          </div>

        </div>
      </div>
      <Footer />
    </div>
  );
}
