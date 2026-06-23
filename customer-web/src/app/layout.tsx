import type { Metadata } from "next";
import { Inter } from "next/font/google";
import "./globals.css";
import { CitySelectOverlay } from "@/components/CitySelectOverlay";

const inter = Inter({
  subsets: ["latin"],
  display: "swap",
  variable: "--font-inter",
});

export const metadata: Metadata = {
  title: "GridTickets — Book Events Online",
  description: "Discover and book tickets for concerts, sports, theatre and more.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body className={`${inter.className} antialiased bg-white text-gray-900`}>
        <CitySelectOverlay />
        {children}
      </body>
    </html>
  );
}
