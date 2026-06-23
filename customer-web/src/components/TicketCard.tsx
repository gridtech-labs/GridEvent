"use client";

import type { ReactNode } from "react";
import QRCode from "react-qr-code";
import { OrderDto } from "@/types/orders";

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString("en-IN", {
    weekday: "short", day: "numeric", month: "long", year: "numeric",
  });
}

export function TicketCard({ order }: { order: OrderDto }) {
  const ref = order.bookingReference ?? order.id.slice(0, 8).toUpperCase();
  const totalQty = order.items.reduce((s, i) => s + i.quantity, 0);

  return (
    <div className="w-full max-w-2xl mx-auto select-none" style={{ fontFamily: "'Inter', sans-serif" }}>
      {/* Ticket wrapper */}
      <div
        className="relative overflow-hidden rounded-3xl shadow-2xl"
        style={{
          background: "linear-gradient(135deg, #1e1b4b 0%, #312e81 40%, #4c1d95 100%)",
        }}
      >
        {/* Decorative dot-grid overlay */}
        <div
          className="absolute inset-0 opacity-10"
          style={{
            backgroundImage:
              "radial-gradient(circle, #a5b4fc 1px, transparent 1px)",
            backgroundSize: "20px 20px",
          }}
        />

        {/* Top accent bar */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-yellow-400 via-rose-400 to-violet-400" />

        <div className="relative flex flex-col sm:flex-row">
          {/* ── LEFT PANEL ── */}
          <div className="flex-1 p-6 sm:p-8">
            {/* Brand + admit badge */}
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center gap-2">
                <span className="text-white text-lg font-black tracking-tight">
                  Grid<span className="text-violet-300">Tickets</span>
                </span>
              </div>
              <span className="text-[10px] font-bold tracking-widest uppercase text-violet-300 border border-violet-500/40 px-2.5 py-1 rounded-full">
                Admit One
              </span>
            </div>

            {/* Event title */}
            <h2 className="text-white text-xl sm:text-2xl font-black leading-tight mb-1 line-clamp-2">
              {order.eventTitle}
            </h2>
            <p className="text-violet-300 text-sm mb-6">{order.eventVenue}</p>

            {/* Info rows */}
            <div className="space-y-3 mb-6">
              <InfoRow icon="calendar" label="Date" value={fmtDate(order.eventStartDate)} />
              <InfoRow
                icon="ticket"
                label="Tickets"
                value={`${totalQty} × ${order.items.map(i => i.tierName).join(", ")}`}
              />
              <InfoRow icon="user" label="Attendee" value={order.customerName} />
            </div>

            {/* Reference + price */}
            <div className="flex items-end justify-between mt-4">
              <div>
                <p className="text-[10px] text-violet-400 uppercase tracking-widest mb-1">Booking Ref</p>
                <p className="text-yellow-300 font-mono font-black text-lg tracking-widest">{ref}</p>
              </div>
              <div className="text-right">
                <p className="text-[10px] text-violet-400 uppercase tracking-widest mb-1">Total Paid</p>
                <p className="text-white font-black text-lg">
                  ₹{order.grandTotal.toLocaleString("en-IN")}
                </p>
              </div>
            </div>
          </div>

          {/* ── PERFORATED DIVIDER ── */}
          <div className="relative flex-none">
            {/* Vertical on sm+, horizontal on mobile */}
            <div className="hidden sm:flex flex-col items-center justify-center h-full px-0">
              {/* Top notch */}
              <div
                className="w-6 h-6 rounded-full -translate-x-1/2"
                style={{ background: "#0f0a2a" }}
              />
              {/* Dashed line */}
              <div className="flex-1 border-l-2 border-dashed border-violet-600/40 my-1" />
              {/* Bottom notch */}
              <div
                className="w-6 h-6 rounded-full -translate-x-1/2"
                style={{ background: "#0f0a2a" }}
              />
            </div>
            {/* Mobile horizontal */}
            <div className="sm:hidden flex items-center justify-center w-full py-0">
              <div
                className="w-6 h-6 rounded-full -translate-y-1/2"
                style={{ background: "#0f0a2a" }}
              />
              <div className="flex-1 border-t-2 border-dashed border-violet-600/40 mx-1" />
              <div
                className="w-6 h-6 rounded-full -translate-y-1/2"
                style={{ background: "#0f0a2a" }}
              />
            </div>
          </div>

          {/* ── RIGHT PANEL (QR) ── */}
          <div
            className="flex-none w-full sm:w-48 flex flex-col items-center justify-center px-6 py-8 sm:py-8 gap-4"
            style={{
              background: "rgba(255,255,255,0.04)",
            }}
          >
            {/* QR code */}
            <div className="bg-white rounded-2xl p-3 shadow-lg">
              <QRCode
                value={ref}
                size={128}
                bgColor="#ffffff"
                fgColor="#1e1b4b"
                style={{ display: "block" }}
              />
            </div>
            <p className="text-violet-300 text-[11px] font-semibold text-center tracking-wide uppercase">
              Scan at venue entry
            </p>
            <p className="text-violet-500 text-[10px] text-center leading-relaxed">
              Present this ticket<br />at the gate
            </p>
          </div>
        </div>

        {/* Bottom strip */}
        <div
          className="relative px-6 sm:px-8 py-3 flex items-center justify-between border-t border-violet-700/30"
          style={{ background: "rgba(0,0,0,0.2)" }}
        >
          <p className="text-violet-400 text-[10px] uppercase tracking-widest">
            Non-transferable · Non-refundable
          </p>
          <p className="text-violet-500 text-[10px] font-mono">{order.id.slice(0, 8).toUpperCase()}</p>
        </div>
      </div>
    </div>
  );
}

function InfoRow({ icon, label, value }: { icon: string; label: string; value: string }) {
  const icons: Record<string, ReactNode> = {
    calendar: (
      <svg className="w-4 h-4 text-violet-400 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
      </svg>
    ),
    ticket: (
      <svg className="w-4 h-4 text-violet-400 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M15 5v2m0 4v2m0 4v2M5 5a2 2 0 00-2 2v3a2 2 0 110 4v3a2 2 0 002 2h14a2 2 0 002-2v-3a2 2 0 110-4V7a2 2 0 00-2-2H5z" />
      </svg>
    ),
    user: (
      <svg className="w-4 h-4 text-violet-400 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.8}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
      </svg>
    ),
  };

  return (
    <div className="flex items-start gap-2.5">
      <span className="mt-0.5">{icons[icon]}</span>
      <div className="min-w-0">
        <p className="text-[10px] text-violet-400 uppercase tracking-widest leading-none mb-0.5">{label}</p>
        <p className="text-white text-sm font-medium leading-snug truncate">{value}</p>
      </div>
    </div>
  );
}
