import './globals.css';
import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Therapist Portal',
  description: 'Therapist web application for the mental health backend',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body className="bg-yellow-50">{children}</body>
    </html>
  );
}
