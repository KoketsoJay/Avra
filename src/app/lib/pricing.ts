/** The rate applied to the signed-in shopper, resolved by UserService. */
export interface MemberDiscount { percentage:number; label:string; }

export const NO_DISCOUNT: MemberDiscount = { percentage:0, label:"" };

/**
 * Applies a member discount to one unit. Mirrors `MemberPricing.Apply` in CartService exactly —
 * rounded to two decimals, half away from zero — so the quoted price is the price charged.
 */
export function memberUnitPrice(unitPrice:number, discountPercentage:number) {
  if (!(discountPercentage > 0)) return unitPrice;
  const pct = Math.min(Math.max(discountPercentage, 0), 100);
  return Math.round(Math.max(unitPrice * (1 - pct / 100), 0) * 100) / 100;
}
