"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCityStore } from "@/store/cityStore";
import { useAuthStore } from "@/store/authStore";

const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5175";

interface NavbarProps {
  showSearch?: boolean;
  search?: string;
  onSearch?: (value: string) => void;
}

export function Navbar({ showSearch = false, search = "", onSearch }: NavbarProps) {
  const router = useRouter();
  const { city, clearCity } = useCityStore();
  const { user, isAuthenticated, clearAuth } = useAuthStore();

  const handleLogout = async () => {
    try {
      await fetch(`${API}/api/auth/logout`, { method: "POST", credentials: "include" });
    } catch {}
    clearAuth();
    router.push("/");
  };

  return (
    <nav className="sticky top-0 z-50 bg-white border-b border-gray-200">
      {/* Main bar */}
      <div className="flex items-center gap-3 px-4 sm:px-6 lg:px-10 h-14">

        {/* Logo */}
        <Link href="/" className="flex items-center gap-1.5 shrink-0 mr-1">
          <svg className="w-6 h-6 text-violet-600" fill="currentColor" viewBox="0 0 24 24">
            <path d="M3 9.5A1.5 1.5 0 0 1 4.5 8h15A1.5 1.5 0 0 1 21 9.5v1a2 2 0 0 0 0 4v1A1.5 1.5 0 0 1 19.5 17h-15A1.5 1.5 0 0 1 3 15.5v-1a2 2 0 0 0 0-4v-1zm9 .75a.75.75 0 1 0 0 1.5.75.75 0 0 0 0-1.5zm0 2.5a.75.75 0 1 0 0 1.5.75.75 0 0 0 0-1.5zm0 2.5a.75.75 0 1 0 0 1.5.75.75 0 0 0 0-1.5z"/>
          </svg>
          <span className="text-lg font-black tracking-tight">
            Grid<span className="text-violet-600">Tickets</span>
          </span>
        </Link>

        {/* Divider */}
        <span className="text-gray-200 text-lg hidden sm:block">|</span>

        {/* City selector */}
        <button
          onClick={clearCity}
          className="hidden sm:flex items-center gap-1 shrink-0 group"
          title="Change city"
        >
          <svg className="w-4 h-4 text-violet-500 shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M5.05 4.05a7 7 0 119.9 9.9L10 18.9l-4.95-4.95a7 7 0 010-9.9zM10 11a2 2 0 100-4 2 2 0 000 4z" clipRule="evenodd" />
          </svg>
          <div className="text-left leading-none">
            <p className="text-sm font-bold text-gray-900 group-hover:text-violet-600 transition-colors">
              {city ? city.name : "Select City"}
            </p>
            {city && <p className="text-xs text-gray-400">{city.state}</p>}
          </div>
          <svg className="w-3.5 h-3.5 text-gray-400 ml-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
          </svg>
        </button>

        {/* Search bar — center, expands */}
        {showSearch && (
          <div className="flex-1 max-w-lg mx-2 hidden md:block">
            <div className="relative">
              <svg className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
              <input
                type="text"
                value={search}
                onChange={(e) => onSearch?.(e.target.value)}
                placeholder="Search for events, venues…"
                className="w-full pl-10 pr-4 py-2 rounded-full text-[14px] bg-gray-100 border border-gray-200 focus:outline-none focus:ring-2 focus:ring-violet-400 focus:bg-white transition-all placeholder-gray-400"
              />
            </div>
          </div>
        )}

        {/* Right: search icon (mobile) + auth */}
        <div className="ml-auto flex items-center gap-2 shrink-0">
          {/* Mobile search icon */}
          {showSearch && (
            <button className="md:hidden p-2 text-gray-500 hover:text-violet-600 transition-colors">
              <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </button>
          )}

          {isAuthenticated ? (
            <>
              <Link
                href="/orders"
                className="hidden sm:block text-sm text-gray-600 hover:text-violet-600 font-medium px-3 py-1.5 rounded-full hover:bg-violet-50 transition-colors"
              >
                My Orders
              </Link>
              <div className="relative group">
                <button className="w-8 h-8 rounded-full bg-gray-100 border border-gray-200 flex items-center justify-center text-sm font-bold text-gray-700 hover:border-violet-400 transition-colors">
                  {user?.firstName?.[0]?.toUpperCase() ?? "U"}
                </button>
                <div className="absolute right-0 top-10 bg-white border border-gray-100 rounded-xl shadow-lg w-44 py-1 hidden group-hover:block z-50">
                  <Link href="/orders" className="block px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50">My Orders</Link>
                  <button onClick={handleLogout} className="w-full text-left px-4 py-2.5 text-sm text-red-500 hover:bg-red-50">Sign Out</button>
                </div>
              </div>
            </>
          ) : (
            <>
              <Link
                href="/auth/login"
                className="text-sm font-semibold text-gray-700 hover:text-violet-600 px-3 py-1.5 rounded-full hover:bg-violet-50 transition-colors"
              >
                Login
              </Link>
              <Link
                href="/auth/register"
                className="text-sm font-semibold bg-violet-600 hover:bg-violet-700 text-white px-4 py-1.5 rounded-full transition-colors"
              >
                Sign Up
              </Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
}
