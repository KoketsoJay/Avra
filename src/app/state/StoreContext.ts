import { createContext, useContext } from "react";
import type { AvraEvent, CommunityCircle, MembershipTier, Product, WellnessProgram } from "@/shared/catalogue";
import type { MemberDiscount } from "@/app/lib/pricing";

/** What the shell hands to every route. Keeps pages free of prop-drilling. */
export interface StoreValue {
  products: Product[];
  events: AvraEvent[];
  programs: WellnessProgram[];
  communities: CommunityCircle[];
  membershipTiers: MembershipTier[];
  discount: MemberDiscount;
  addToCart: (product: Product, rect: DOMRect, img: string) => void;
  openCheckout: () => void;
  openOrders: () => void;
}

export const StoreCtx = createContext<StoreValue | null>(null);

export function useStore() {
  const value = useContext(StoreCtx);
  if (!value) throw new Error("useStore must be used inside the storefront shell");
  return value;
}
