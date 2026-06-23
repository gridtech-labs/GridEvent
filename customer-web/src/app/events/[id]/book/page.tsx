'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { EventDetail } from '@/types/events';
import { OrderDto } from '@/types/orders';
import { api } from '@/lib/api';
import { useAuthStore } from '@/store/authStore';
import { Navbar } from '@/components/Navbar';
import Footer from '@/components/Footer';

const API = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5001';

type Step = 'select' | 'billing' | 'review';

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-IN', {
    weekday: 'short', day: 'numeric', month: 'long', year: 'numeric',
  });
}

function useCountdown(expiresAt: string | null) {
  const [remaining, setRemaining] = useState<number>(0);
  useEffect(() => {
    if (!expiresAt) return;
    const tick = () => {
      const diff = Math.max(0, new Date(expiresAt).getTime() - Date.now());
      setRemaining(diff);
    };
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [expiresAt]);
  const mins = Math.floor(remaining / 60000);
  const secs = Math.floor((remaining % 60000) / 1000);
  return { remaining, display: `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}` };
}

export default function BookPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { user, isAuthenticated, setAuth } = useAuthStore();

  const [event, setEvent] = useState<EventDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [step, setStep] = useState<Step>('select');
  const [selections, setSelections] = useState<Record<string, number>>({});
  const [billing, setBilling] = useState({ name: '', email: '', phone: '' });
  const [billingErrors, setBillingErrors] = useState<Record<string, string>>({});
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { display: timerDisplay, remaining } = useCountdown(order?.expiresAt ?? null);

  useEffect(() => {
    fetch(`${API}/api/events/${id}`)
      .then(r => r.ok ? r.json() : null)
      .then((d: EventDetail | null) => { setEvent(d); setLoading(false); });
  }, [id]);

  // Pre-fill billing from logged-in user
  useEffect(() => {
    if (isAuthenticated && user) {
      setBilling(b => ({
        name: b.name || user.fullName || `${user.firstName} ${user.lastName}`.trim(),
        email: b.email || user.email,
        phone: b.phone || user.phoneNumber || '',
      }));
    }
  }, [isAuthenticated, user]);

  if (loading) return (
    <>
      <Navbar />
      <main className="min-h-screen flex items-center justify-center">
        <div className="animate-pulse text-gray-400">Loading…</div>
      </main>
      <Footer />
    </>
  );

  if (!event) return (
    <>
      <Navbar />
      <main className="min-h-screen flex items-center justify-center text-center">
        <div>
          <p className="text-gray-500 mb-4">Event not found.</p>
          <Link href="/events" className="text-indigo-600 text-sm">← Back to events</Link>
        </div>
      </main>
      <Footer />
    </>
  );

  const availableTiers = event.ticketTiers.filter(t => t.availableQuantity > 0);

  function updateQty(tierId: string, qty: number) {
    setSelections(prev => {
      if (qty === 0) { const next = { ...prev }; delete next[tierId]; return next; }
      return { ...prev, [tierId]: qty };
    });
  }

  const selectedItems = event.ticketTiers
    .filter(t => (selections[t.id] ?? 0) > 0)
    .map(t => ({ tier: t, quantity: selections[t.id] }));

  const subTotal = selectedItems.reduce((s, i) => s + i.tier.price * i.quantity, 0);
  const bookingFee = Math.round(subTotal * 0.035 * 100) / 100;
  const grandTotal = subTotal + bookingFee;

  function validateBilling() {
    const errors: Record<string, string> = {};
    if (!billing.name.trim()) errors.name = 'Name is required.';
    if (!billing.email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(billing.email))
      errors.email = 'Valid email is required.';
    if (!billing.phone.trim() || billing.phone.replace(/\D/g, '').length < 10)
      errors.phone = 'Valid 10-digit phone number is required.';
    setBillingErrors(errors);
    return Object.keys(errors).length === 0;
  }

  async function createOrder() {
    if (!validateBilling()) return;
    setSubmitting(true);
    setError(null);
    try {
      // Auto-login / create account from billing details if not already authenticated
      if (!isAuthenticated) {
        const res = await fetch(`${API}/api/auth/silent-login`, {
          method: 'POST',
          credentials: 'include',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ email: billing.email, mobile: billing.phone, fullName: billing.name }),
        });
        const data = await res.json();
        if (!res.ok) {
          setError(data.errors?.[0] ?? 'Could not sign in. Please try again.');
          return;
        }
        setAuth(data.user, data.accessToken);
      }

      const { data } = await api.post<OrderDto>('/api/orders', {
        eventId: id,
        items: selectedItems.map(i => ({ ticketTierId: i.tier.id, quantity: i.quantity })),
        customerName: billing.name,
        customerEmail: billing.email,
        customerPhone: billing.phone,
      });
      setOrder(data);
      setStep('review');
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Failed to create order. Please try again.');
    } finally {
      setSubmitting(false);
    }
  }

  async function initiatePayment() {
    if (!order) return;
    setSubmitting(true);
    setError(null);
    // When the Razorpay modal opens we hand off control to Razorpay callbacks,
    // so we must NOT reset submitting in the finally block for that path.
    let modalOpened = false;
    try {
      const { data } = await api.post('/api/payments/initiate', { orderId: order.id });
      if (data.keyId === 'not_configured') {
        // Razorpay not yet configured — auto-verify for testing
        const { data: verified } = await api.post('/api/payments/verify', {
          orderId: order.id,
          razorpayOrderId: data.razorpayOrderId,
          razorpayPaymentId: 'pay_test_placeholder',
          razorpaySignature: '',
        });
        if (verified.status === 'Confirmed') {
          router.push(`/orders/${order.id}/confirmation`);
        }
        return;
      }

      // Real Razorpay flow — open the modal
      const openModal = () => {
        const rzp = new (window as unknown as { Razorpay: new (opts: object) => { open(): void } }).Razorpay({
          key: data.keyId,
          amount: data.amount * 100, // amount from API is already in rupees; multiply to paise
          currency: data.currency,
          order_id: data.razorpayOrderId,
          name: 'GridTickets',
          description: event!.title,
          handler: async (response: { razorpay_order_id: string; razorpay_payment_id: string; razorpay_signature: string }) => {
            try {
              const { data: verified } = await api.post('/api/payments/verify', {
                orderId: order!.id,
                razorpayOrderId: response.razorpay_order_id,
                razorpayPaymentId: response.razorpay_payment_id,
                razorpaySignature: response.razorpay_signature,
              });
              if (verified.status === 'Confirmed') {
                router.push(`/orders/${order!.id}/confirmation`);
              } else {
                setError('Payment could not be verified. Please contact support.');
                setSubmitting(false);
              }
            } catch {
              setError('Payment verification failed. Please contact support if your money was deducted.');
              setSubmitting(false);
            }
          },
          modal: {
            ondismiss: () => {
              // User closed the modal without paying
              setSubmitting(false);
            },
          },
          prefill: { name: billing.name, email: billing.email, contact: billing.phone },
          theme: { color: '#4f46e5' },
        });
        rzp.open();
      };

      // Load the Razorpay checkout script once, then open the modal
      if ((window as unknown as { Razorpay?: unknown }).Razorpay) {
        modalOpened = true;
        openModal();
      } else {
        const script = document.createElement('script');
        script.src = 'https://checkout.razorpay.com/v1/checkout.js';
        script.onload = () => { modalOpened = true; openModal(); };
        script.onerror = () => {
          setError('Failed to load payment gateway. Please check your connection and try again.');
          setSubmitting(false);
        };
        document.head.appendChild(script);
        // Script is loading asynchronously — suppress the finally reset
        modalOpened = true;
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg ?? 'Payment initiation failed.');
    } finally {
      // Only reset submitting if the Razorpay modal was not opened.
      // When the modal is open, submitting is reset inside the modal callbacks.
      if (!modalOpened) setSubmitting(false);
    }
  }

  const stepLabels: Record<Step, string> = {
    select: 'Select Tickets',
    billing: 'Your Details',
    review: 'Review & Pay',
  };
  const stepOrder: Step[] = ['select', 'billing', 'review'];

  return (
    <>
    <Navbar />
    <main className="max-w-3xl mx-auto px-6 py-10 pb-20">
      <Link href={`/events/${id}`} className="text-sm text-gray-500 hover:text-indigo-600 mb-6 inline-flex items-center gap-1">
        ← Back to event
      </Link>

      <h1 className="text-2xl font-bold mb-1">{event.title}</h1>
      <p className="text-sm text-gray-500 mb-6">
        {formatDate(event.startDate)} · {event.venueName}, {event.venueCity}
      </p>

      {/* Countdown timer (shown after order is created) */}
      {order && remaining > 0 && (
        <div className={`flex items-center justify-center gap-2 text-sm font-medium py-2 px-4 rounded-xl mb-6 ${remaining < 120000 ? 'bg-red-50 text-red-600' : 'bg-indigo-50 text-indigo-700'}`}>
          ⏱ Complete your booking in <span className="font-bold tabular-nums">{timerDisplay}</span> mins
        </div>
      )}
      {order && remaining === 0 && (
        <div className="bg-red-50 text-red-600 text-sm font-medium py-2 px-4 rounded-xl mb-6 text-center">
          Your booking has expired. <Link href={`/events/${id}`} className="underline">Start again</Link>
        </div>
      )}

      {/* Step indicator */}
      <div className="flex items-center gap-2 mb-8">
        {stepOrder.map((s, i) => (
          <div key={s} className="flex items-center gap-2">
            {i > 0 && <div className="h-px w-8 bg-gray-200" />}
            <div className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold
              ${step === s ? 'bg-indigo-600 text-white' : stepOrder.indexOf(step) > i ? 'bg-green-500 text-white' : 'bg-gray-100 text-gray-400'}`}>
              {stepOrder.indexOf(step) > i ? '✓' : i + 1}
            </div>
            <span className={`text-sm ${step === s ? 'font-semibold' : 'text-gray-400'}`}>{stepLabels[s]}</span>
          </div>
        ))}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl px-4 py-3 mb-4">
          {error}
        </div>
      )}

      {/* Step 1: Select Tickets */}
      {step === 'select' && (
        <div>
          {availableTiers.length === 0 ? (
            <div className="text-center py-16 text-gray-400">All tickets are sold out.</div>
          ) : (
            <div className="space-y-3 mb-8">
              {availableTiers.map(tier => {
                const qty = selections[tier.id] ?? 0;
                const max = Math.min(tier.availableQuantity, 10);
                return (
                  <div key={tier.id} className="flex items-center justify-between rounded-2xl border border-gray-200 p-5">
                    <div>
                      <p className="font-semibold">{tier.name}</p>
                      {tier.description && <p className="text-xs text-gray-500 mt-0.5">{tier.description}</p>}
                      <p className="text-sm font-bold text-indigo-600 mt-1">₹{tier.price.toLocaleString('en-IN')}</p>
                      <p className="text-xs text-gray-400 mt-0.5">
                        {tier.availableQuantity <= 10 ? `Only ${tier.availableQuantity} left` : `${tier.availableQuantity} available`}
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <button onClick={() => updateQty(tier.id, Math.max(0, qty - 1))} disabled={qty === 0}
                        className="w-8 h-8 rounded-full border text-lg disabled:opacity-30 hover:bg-gray-50 flex items-center justify-center">−</button>
                      <span className="w-6 text-center font-semibold">{qty}</span>
                      <button onClick={() => updateQty(tier.id, Math.min(max, qty + 1))} disabled={qty >= max}
                        className="w-8 h-8 rounded-full border text-lg disabled:opacity-30 hover:bg-gray-50 flex items-center justify-center">+</button>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
          {selectedItems.length > 0 && (
            <div className="flex items-center justify-between rounded-2xl bg-indigo-50 border border-indigo-100 px-5 py-4">
              <div>
                <p className="text-sm text-gray-600">{selectedItems.reduce((s, i) => s + i.quantity, 0)} ticket(s)</p>
                <p className="font-bold text-indigo-700 text-lg">₹{subTotal.toLocaleString('en-IN')}</p>
              </div>
              <button onClick={() => setStep('billing')}
                className="bg-indigo-600 hover:bg-indigo-700 text-white font-semibold px-6 py-2.5 rounded-xl text-sm">
                Continue →
              </button>
            </div>
          )}
        </div>
      )}

      {/* Step 2: Billing Details */}
      {step === 'billing' && (
        <div className="space-y-5">
          <p className="text-sm text-gray-500">These details will appear on your booking confirmation.</p>

          <div>
            <label className="block text-sm font-medium mb-1">Full Name <span className="text-red-500">*</span></label>
            <input value={billing.name} onChange={e => setBilling(b => ({ ...b, name: e.target.value }))}
              className={`w-full border rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 ${billingErrors.name ? 'border-red-400' : 'border-gray-200'}`}
              placeholder="Sarvesh Chaudhary" />
            {billingErrors.name && <p className="text-red-500 text-xs mt-1">{billingErrors.name}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Email <span className="text-red-500">*</span></label>
            <input value={billing.email} onChange={e => setBilling(b => ({ ...b, email: e.target.value }))}
              type="email"
              className={`w-full border rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 ${billingErrors.email ? 'border-red-400' : 'border-gray-200'}`}
              placeholder="you@email.com" />
            <p className="text-xs text-gray-400 mt-1">Booking confirmation will be sent here.</p>
            {billingErrors.email && <p className="text-red-500 text-xs">{billingErrors.email}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">Phone <span className="text-red-500">*</span></label>
            <input value={billing.phone} onChange={e => setBilling(b => ({ ...b, phone: e.target.value }))}
              type="tel"
              className={`w-full border rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 ${billingErrors.phone ? 'border-red-400' : 'border-gray-200'}`}
              placeholder="+91 98765 43210" />
            {billingErrors.phone && <p className="text-red-500 text-xs mt-1">{billingErrors.phone}</p>}
          </div>

          {/* Order summary preview */}
          <div className="rounded-2xl bg-gray-50 border border-gray-100 px-5 py-4 text-sm">
            <p className="font-semibold mb-3">Order Summary</p>
            {selectedItems.map(({ tier, quantity }) => (
              <div key={tier.id} className="flex justify-between mb-1 text-gray-600">
                <span>{tier.name} × {quantity}</span>
                <span>₹{(tier.price * quantity).toLocaleString('en-IN')}</span>
              </div>
            ))}
            <div className="flex justify-between text-gray-400 mt-2 pt-2 border-t border-gray-100">
              <span>Booking fee (3.5%)</span>
              <span>₹{bookingFee.toLocaleString('en-IN')}</span>
            </div>
            <div className="flex justify-between font-bold mt-2 pt-2 border-t">
              <span>Grand Total</span>
              <span className="text-indigo-600">₹{grandTotal.toLocaleString('en-IN')}</span>
            </div>
          </div>

          <div className="flex gap-3">
            <button onClick={() => setStep('select')} className="px-5 py-2.5 rounded-xl border text-sm font-medium hover:bg-gray-50">
              ← Back
            </button>
            <button onClick={createOrder} disabled={submitting}
              className="flex-1 bg-indigo-600 hover:bg-indigo-700 disabled:bg-indigo-300 text-white font-semibold py-2.5 rounded-xl text-sm">
              {submitting ? 'Creating order…' : 'Confirm & Proceed to Payment →'}
            </button>
          </div>
        </div>
      )}

      {/* Step 3: Review & Pay */}
      {step === 'review' && order && (
        <div className="space-y-5">
          <div className="rounded-2xl border border-gray-200 p-6">
            <h2 className="font-semibold text-lg mb-4">Order Summary</h2>
            <p className="text-xs text-gray-400 font-mono mb-4">Order ID: {order.id}</p>
            {order.items.map(item => (
              <div key={item.id} className="flex justify-between text-sm py-2 border-b border-gray-50 last:border-0">
                <span>{item.tierName} × {item.quantity}</span>
                <span className="font-medium">₹{item.lineTotal.toLocaleString('en-IN')}</span>
              </div>
            ))}
            <div className="flex justify-between text-sm text-gray-400 mt-3">
              <span>Booking fee</span>
              <span>₹{order.bookingFee.toLocaleString('en-IN')}</span>
            </div>
            <div className="flex justify-between font-bold text-base mt-3 pt-3 border-t">
              <span>Total</span>
              <span className="text-indigo-600">₹{order.grandTotal.toLocaleString('en-IN')}</span>
            </div>
          </div>

          <div className="rounded-2xl border border-gray-200 p-5 text-sm text-gray-600">
            <p className="font-semibold mb-2">Billing Details</p>
            <p>{order.customerName}</p>
            <p>{order.customerEmail}</p>
            <p>{order.customerPhone}</p>
          </div>

          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl px-4 py-3">{error}</div>
          )}

          <button onClick={initiatePayment} disabled={submitting || remaining === 0}
            className="w-full bg-indigo-600 hover:bg-indigo-700 disabled:bg-gray-300 disabled:cursor-not-allowed text-white font-bold py-3.5 rounded-xl text-sm">
            {submitting ? 'Processing…' : remaining === 0 ? 'Order Expired' : '🔒 Pay Now'}
          </button>
          <p className="text-xs text-gray-400 text-center">Secured by Razorpay</p>
        </div>
      )}
    </main>
    <Footer />
    </>
  );
}
