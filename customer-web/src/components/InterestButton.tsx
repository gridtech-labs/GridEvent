"use client";

import { useState } from "react";

export function InterestButton() {
  const [interested, setInterested] = useState(false);

  return (
    <button
      onClick={() => setInterested((v) => !v)}
      className={`flex items-center gap-2 text-sm font-semibold px-4 py-2 rounded-full border transition-colors ${
        interested
          ? "bg-rose-500 border-rose-500 text-white"
          : "border-rose-500 text-rose-500 hover:bg-rose-50"
      }`}
    >
      <svg
        className="w-4 h-4"
        fill={interested ? "currentColor" : "none"}
        viewBox="0 0 24 24"
        stroke="currentColor"
        strokeWidth={2}
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M14 10h4.764a2 2 0 011.789 2.894l-3.5 7A2 2 0 0115.263 21h-4.017c-.163 0-.326-.02-.485-.06L7 20m7-10V5a2 2 0 00-2-2h-.095c-.5 0-.905.405-.905.905 0 .714-.211 1.412-.608 2.006L7 11v9m7-10h-2M7 20H5a2 2 0 01-2-2v-6a2 2 0 012-2h2.5"
        />
      </svg>
      {interested ? "Interested!" : "I'm Interested"}
    </button>
  );
}
