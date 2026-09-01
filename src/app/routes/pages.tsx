import { useEffect } from "react";
import type React from "react";
import {
  BrandStory, Club, Community, EditorialStrip, Events, Hero,
  Newsletter, Shop, Testimonials, Wellness,
} from "@/app/sections";
import { useStore } from "@/app/state/StoreContext";

/** Standalone pages sit below the fixed navbar; the landing page handles its own spacing. */
function Page({ children }: { children: React.ReactNode }) {
  return <div className="pt-20">{children}</div>;
}

/** The full scroll narrative, unchanged. */
export function HomePage() {
  const store = useStore();
  return (
    <>
      <Hero onShopClick={()=>document.getElementById("shop")?.scrollIntoView({ behavior:"smooth" })} />
      <BrandStory />
      <Shop products={store.products} discount={store.discount} onAddToCart={store.addToCart} />
      <EditorialStrip />
      <Club tiers={store.membershipTiers} />
      <Wellness programs={store.programs} />
      <Community communities={store.communities} />
      <Events events={store.events} />
      <Testimonials />
      <Newsletter />
    </>
  );
}

export function ShopPage() {
  const store = useStore();
  return <Page><Shop products={store.products} discount={store.discount} onAddToCart={store.addToCart} /></Page>;
}

export function ClubPage() {
  const store = useStore();
  return <Page><Club tiers={store.membershipTiers} /></Page>;
}

export function WellnessPage() {
  const store = useStore();
  return <Page><Wellness programs={store.programs} /></Page>;
}

export function CommunityPage() {
  const store = useStore();
  return <Page><Community communities={store.communities} /></Page>;
}

export function EventsPage() {
  const store = useStore();
  return <Page><Events events={store.events} /></Page>;
}

/** /checkout and /orders are linkable URLs that open the existing overlays over the landing page. */
export function CheckoutPage() {
  const { openCheckout } = useStore();
  useEffect(() => { openCheckout(); }, [openCheckout]);
  return <HomePage />;
}

export function OrdersPage() {
  const { openOrders } = useStore();
  useEffect(() => { openOrders(); }, [openOrders]);
  return <HomePage />;
}
