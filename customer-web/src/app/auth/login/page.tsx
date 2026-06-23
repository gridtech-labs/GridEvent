"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useAuthStore } from "@/store/authStore";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5175";

type Step = "details" | "otp";

export default function LoginPage() {
  const router = useRouter();
  const setAuth = useAuthStore((s) => s.setAuth);

  const [step, setStep] = useState<Step>("details");
  const [email, setEmail] = useState("");
  const [mobile, setMobile] = useState("");
  const [otp, setOtp] = useState("");
  const [errors, setErrors] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  // ── Step 1: request OTP ───────────────────────────────────────────────────
  const handleRequestOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);

    const emailTrimmed = email.trim();
    const mobileTrimmed = mobile.trim();

    if (!emailTrimmed || !mobileTrimmed) {
      setErrors(["Please enter your email and mobile number."]);
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(emailTrimmed)) {
      setErrors(["Please enter a valid email address."]);
      return;
    }
    if (mobileTrimmed.replace(/\D/g, "").length < 10) {
      setErrors(["Please enter a valid 10-digit mobile number."]);
      return;
    }

    setLoading(true);
    try {
      const res = await fetch(`${API}/api/auth/request-otp`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: emailTrimmed, mobile: mobileTrimmed }),
      });
      const data = await res.json();
      if (!res.ok) {
        setErrors(data.errors ?? ["Something went wrong. Please try again."]);
        return;
      }
      setStep("otp");
    } catch {
      setErrors(["Network error. Please try again."]);
    } finally {
      setLoading(false);
    }
  };

  // ── Step 2: verify OTP ────────────────────────────────────────────────────
  const handleVerifyOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors([]);

    if (otp.trim().length !== 6) {
      setErrors(["Please enter the 6-digit OTP."]);
      return;
    }

    setLoading(true);
    try {
      const res = await fetch(`${API}/api/auth/verify-otp`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: email.trim(), otp: otp.trim() }),
      });
      const data = await res.json();
      if (!res.ok) {
        setErrors(data.errors ?? ["Invalid OTP. Please try again."]);
        return;
      }
      setAuth(data.user, data.accessToken);
      router.push("/");
    } catch {
      setErrors(["Network error. Please try again."]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md">

        {/* Logo */}
        <div className="text-center mb-8">
          <Link href="/" className="text-3xl font-black tracking-tight text-gray-900">
            Grid<span className="text-violet-600">Tickets</span>
          </Link>
          <p className="text-gray-500 mt-2 text-sm">
            {step === "details"
              ? "Sign in or create your account"
              : `Enter the OTP sent to ${email}`}
          </p>
        </div>

        <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8">

          {errors.length > 0 && (
            <div className="mb-5 rounded-xl bg-red-50 border border-red-200 p-3">
              {errors.map((e, i) => (
                <p key={i} className="text-red-600 text-sm">{e}</p>
              ))}
            </div>
          )}

          {/* ── Step 1: email + mobile ── */}
          {step === "details" && (
            <form onSubmit={handleRequestOtp} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Email address <span className="text-red-500">*</span>
                </label>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  required
                  autoComplete="email"
                  className="w-full border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-violet-400 transition-all"
                  placeholder="you@example.com"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Mobile number <span className="text-red-500">*</span>
                </label>
                <input
                  type="tel"
                  value={mobile}
                  onChange={(e) => setMobile(e.target.value)}
                  required
                  autoComplete="tel"
                  className="w-full border border-gray-200 rounded-xl px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-violet-400 transition-all"
                  placeholder="+91 98765 43210"
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full bg-violet-600 hover:bg-violet-700 disabled:opacity-60 text-white font-semibold py-3 rounded-xl text-sm transition-colors"
              >
                {loading ? "Sending OTP…" : "Send OTP →"}
              </button>

              <p className="text-center text-xs text-gray-400 mt-2">
                New here? We&apos;ll create your account automatically.
              </p>
            </form>
          )}

          {/* ── Step 2: OTP entry ── */}
          {step === "otp" && (
            <form onSubmit={handleVerifyOtp} className="space-y-5">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  6-digit OTP <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  inputMode="numeric"
                  pattern="\d{6}"
                  maxLength={6}
                  value={otp}
                  onChange={(e) => setOtp(e.target.value.replace(/\D/g, ""))}
                  autoFocus
                  className="w-full border border-gray-200 rounded-xl px-4 py-3 text-2xl font-bold tracking-[0.5em] text-center focus:outline-none focus:ring-2 focus:ring-violet-400 transition-all"
                  placeholder="------"
                />
                <p className="text-xs text-gray-400 mt-1.5 text-center">
                  Check the API console / server logs for the OTP.
                  <br />SMS &amp; Email delivery will be enabled once wired up.
                </p>
              </div>

              <button
                type="submit"
                disabled={loading || otp.length !== 6}
                className="w-full bg-violet-600 hover:bg-violet-700 disabled:opacity-60 text-white font-semibold py-3 rounded-xl text-sm transition-colors"
              >
                {loading ? "Verifying…" : "Verify & Sign In →"}
              </button>

              <button
                type="button"
                onClick={() => { setStep("details"); setOtp(""); setErrors([]); }}
                className="w-full text-sm text-gray-500 hover:text-violet-600 transition-colors"
              >
                ← Change email / mobile
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
