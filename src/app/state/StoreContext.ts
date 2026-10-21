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
  /** Tier the shopper currently holds, or null. */
  membershipPlanName: string | null;
  /** Free-trial end date, when the member is still inside the trial. */
  membershipTrialEndsAt: string | null;
  /** When the next charge falls due. */
  membershipNextBillingDate: string | null;
  joiningPlanId: string | null;
  joinMembership: (tier: MembershipTier) => void;
  openAccount: () => void;
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
