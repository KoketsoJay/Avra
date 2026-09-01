import {
  useState, useRef, useEffect, useLayoutEffect, useCallback,
  createContext, useContext,
} from "react";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { AnimatePresence, motion } from "motion/react";
import {
  ShoppingBag, X, Menu, Users, Calendar, Heart,
  Check, ArrowRight, Minus, Plus, MapPin,
  Clock, UserRound, LogOut, Package,
  CreditCard, Truck, ReceiptText, Printer, ArrowLeft, Eye, EyeOff, AlertTriangle,
} from "lucide-react";
import {
  avraApi,
  getAvraUser,
  getStoredProfile,
  isApiUnavailable,
  isAuthenticated,
  ORDER_STATUS_LABELS,
  saveLocalProfile,
  type BasketCheckoutPayload,
  type CartDto,
  type CommunityDto,
  type CommunityPayload,
  type EventDto,
  type EventPayload,
  type MembershipPlanDto,
  type OrderDto,
  type OrderViewModel,
  type ProductPayload,
  type ProductDto,
  type UserProfileDto,
  type WellnessPlanPayload,
  type WellnessPlanDto,
} from "@/app/api/avraApi";
import { useCatalogue } from "@/shared/catalogue";
import { NO_DISCOUNT, memberUnitPrice, type MemberDiscount } from "@/app/lib/pricing";
import {
  Bracket, G, Grain, Shine, TiltCard, bg, bodoni, fg, gold, goldGrad, money,
} from "@/app/ui/primitives";
import { StoreCtx } from "@/app/state/StoreContext";
import {
  ClubPage, CommunityPage, EventsPage, HomePage, ShopPage, WellnessPage,
  CheckoutPage, OrdersPage as OrdersRoutePage,
} from "@/app/routes/pages";
import { Footer } from "@/app/sections";
import { Link, Route, Routes } from "react-router";

gsap.registerPlugin(ScrollTrigger);

// â”€â”€â”€ Meta â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
if (typeof document !== "undefined") {
  document.title = "AVRA";
  let m = document.querySelector<HTMLMetaElement>('meta[name="description"]');
  if (!m) { m = document.createElement("meta"); m.name = "description"; document.head.appendChild(m); }
  m.content = "AVRA â€” A wellness lifestyle club for women who are becoming. Curated collections, membership, wellness plans, community circles, and transformative retreats across South Africa.";
}

// â”€â”€â”€ Images â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

// â”€â”€â”€ Types â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
interface Product { id:number; name:string; price:number; category:string; img1:string; img2:string; description:string; tag?:string; }
interface CartItem extends Product { quantity:number; }
interface MembershipTier { id:string; name:string; tagline:string; price:number; highlight:boolean; features:string[]; discountPercentage:number; }

/** Sections backed by the shared create/edit form. The rest render their own operational panel. */

// â”€â”€â”€ Catalogue imagery â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
// The services store an image reference (a bundled asset file name, or an absolute URL). Nothing
// about the catalogue itself lives in the client â€” only the mapping from reference to bundled URL.
const bundledImages = import.meta.glob<string>("../assets/avra/*.jpg", { eager: true, import: "default" });
const IMAGE_BY_FILENAME: Record<string, string> = Object.fromEntries(
  Object.entries(bundledImages).map(([path, url]) => [path.slice(path.lastIndexOf("/") + 1), url]),
);

/** Default imagery offered when an administrator creates a record without picking a picture. */

/** Turns a stored image reference into something an <img> can load. */
function resolveImage(reference?: string | null) {
  if (!reference) return undefined;
  if (/^(https?:|data:|blob:|\/)/i.test(reference)) return reference;
  return IMAGE_BY_FILENAME[reference.slice(reference.lastIndexOf("/") + 1)];
}

function toCurrencyPrice(price: number) {
  return Number.isFinite(price) ? price : 0;
}


function mapCartDto(cart: CartDto): CartItem[] {
  return cart.items.map(item => ({
    id: item.productId,
    name: item.productName,
    price: toCurrencyPrice(item.price),
    quantity: item.quantity,
    category: "",
    description: "",
    img1: item.image ?? "",
    img2: item.image ?? "",
  }));
}






function mapMembershipPlanDto(plan: MembershipPlanDto): MembershipTier {
  return {
    id: plan.id,
    name: plan.name,
    tagline: plan.tagline,
    price: toCurrencyPrice(plan.monthlyPrice),
    highlight: plan.isHighlighted,
    features: plan.features,
    discountPercentage: toCurrencyPrice(plan.discountPercentage),
  };
}





// â”€â”€â”€ Checkout / order domain â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const ORDERS_KEY = "avra_orders";
const FREE_SHIPPING_THRESHOLD = 1500;
const SHIPPING_FLAT = 120;
const VAT_RATE = 0.15;
const PAYMENT_METHODS = ["Card", "EFT / Bank Transfer", "Cash on Delivery"] as const;
type PaymentMethod = typeof PAYMENT_METHODS[number];

/** The ordering service settles cards itself; the other methods are handled out of band. */
const PAYMENT_METHOD_CODES: Record<PaymentMethod, BasketCheckoutPayload["paymentMethod"]> = {
  "Card": "Card",
  "EFT / Bank Transfer": "BankTransfer",
  "Cash on Delivery": "CashOnDelivery",
};

/** How the ordering saga's states read to a shopper. */
const ORDER_STATUS_COPY: Record<string, string> = {
  Submitted: "Order placed",
  AwaitingValidation: "Confirming stock",
  StockConfirmed: "Awaiting payment",
  Paid: "Paid",
  Shipped: "Shipped",
  Cancelled: "Cancelled",
};

interface OrderLine { productId:number; name:string; price:number; quantity:number; image?:string; }
interface OrderTotals { subtotal:number; discount:number; shipping:number; tax:number; total:number; }
interface ShippingDetails {
  firstName:string; lastName:string; email:string; phone:string;
  line1:string; line2:string; city:string; province:string; postalCode:string; country:string;
}
interface PaymentSummary { method:PaymentMethod; cardBrand?:string; last4?:string; }
/** Held only for the duration of the checkout call; never stored or logged. */
interface CardDetails { name:string; number:string; expiry:string; cvc:string; }
interface OrderRecord {
  reference:string;
  remoteId?:number;
  createdAt:string;
  status:string;
  items:OrderLine[];
  totals:OrderTotals;
  shipping:ShippingDetails;
  payment:PaymentSummary;
}

function calcTotals(items:OrderLine[], discountPercentage = 0): OrderTotals {
  const gross = items.reduce((sum, item) => sum + item.price * item.quantity, 0);
  const subtotal = items.reduce((sum, item) => sum + memberUnitPrice(item.price, discountPercentage) * item.quantity, 0);
  const discount = gross - subtotal;
  const shipping = subtotal === 0 || subtotal >= FREE_SHIPPING_THRESHOLD ? 0 : SHIPPING_FLAT;
  const tax = (subtotal + shipping) * VAT_RATE;
  return { subtotal, discount, shipping, tax, total: subtotal + shipping + tax };
}

function emptyShippingDetails(profile: UserProfileDto|null): ShippingDetails {
  return {
    firstName: profile?.firstName ?? "",
    lastName: profile?.lastName ?? "",
    email: profile?.email ?? "",
    phone: profile?.phoneNumber ?? "",
    line1: "", line2: "", city: "", province: "", postalCode: "", country: "South Africa",
  };
}

function formatAddress(details: ShippingDetails) {
  return [
    `${details.firstName} ${details.lastName}`.trim(),
    details.line1,
    details.line2,
    [details.city, details.province, details.postalCode].filter(Boolean).join(", "),
    details.country,
    details.phone,
    details.email,
  ].filter(Boolean).join("\n");
}

function makeReference() {
  const stamp = Date.now().toString(36).toUpperCase().slice(-5);
  const salt = Math.floor(Math.random() * 1296).toString(36).toUpperCase().padStart(2, "0");
  return `AVRA-${stamp}${salt}`;
}

/** Only the brand and last four digits are ever kept â€” full card data never leaves the form. */
function cardBrandOf(digits: string) {
  if (/^4/.test(digits)) return "Visa";
  if (/^5[1-5]/.test(digits)) return "Mastercard";
  if (/^3[47]/.test(digits)) return "Amex";
  return "Card";
}

/** Matches the ordering service CardType enum. */
function cardTypeIdOf(digits: string) {
  const brand = cardBrandOf(digits);
  return brand === "Amex" ? 1 : brand === "Mastercard" ? 3 : 2;
}

/** MM/YY becomes the last instant of that month, which is when the card actually expires. */
function expiryToIso(expiry: string) {
  const [month, year] = expiry.replace(/\s/g, "").split("/");
  const fullYear = 2000 + Number(year);
  return new Date(Date.UTC(fullYear, Number(month), 0, 23, 59, 59)).toISOString();
}

function statusLabelOf(status: number|string) {
  if (typeof status === "number") return ORDER_STATUS_LABELS[status] ?? "Pending";
  return ORDER_STATUS_COPY[status] ?? status;
}

function readOrders(): OrderRecord[] {
  if (typeof localStorage === "undefined") return [];
  try {
    const raw = localStorage.getItem(ORDERS_KEY);
    return raw ? JSON.parse(raw) as OrderRecord[] : [];
  } catch {
    return [];
  }
}

function saveOrders(orders: OrderRecord[]) {
  if (typeof localStorage !== "undefined") {
    localStorage.setItem(ORDERS_KEY, JSON.stringify(orders));
  }
}


/** Local records win: they hold the contact, address, and payment detail the API does not return. */
function mergeOrders(local: OrderRecord[], remote: OrderRecord[]) {
  const merged = [...local];
  remote.forEach(order => {
    if (!merged.some(existing => existing.remoteId === order.remoteId)) {
      merged.push(order);
    }
  });
  return merged.sort((a, b) => Date.parse(b.createdAt) - Date.parse(a.createdAt));
}

function mapOrderViewModel(order: OrderViewModel): OrderRecord {
  const items: OrderLine[] = order.orderItems.map(item => ({
    productId: item.productId,
    name: item.productName,
    price: toCurrencyPrice(item.unitPrice),
    quantity: item.units,
    image: item.pictureUrl ?? undefined,
  }));

  return {
    reference: `AVRA-${String(order.orderNumber).padStart(6, "0")}`,
    remoteId: order.orderNumber,
    createdAt: order.date,
    status: statusLabelOf(order.status),
    items,
    totals: calcTotals(items),
    shipping: {
      ...emptyShippingDetails(null),
      line1: order.street,
      city: order.city,
      province: order.state,
      postalCode: order.zipCode,
      country: order.country,
    },
    payment: { method: "Card" },
  };
}

/** The saga creates orders asynchronously, so the list is the source of truth after checkout. */
async function fetchRemoteOrders(): Promise<OrderRecord[]> {
  const summaries = await avraApi.ordering.list();
  const details = await Promise.all(
    summaries.map(summary =>
      avraApi.ordering.byId(summary.orderNumber).catch(() => null)),
  );
  return details.filter((order): order is OrderViewModel => order !== null).map(mapOrderViewModel);
}

/**
 * Checkout returns before the order exists, so poll briefly for an order number we have not
 * seen. Falling through without one is not an error â€” the saga may simply still be running.
 */
async function waitForNewOrder(knownIds: Set<number>, attempts = 12, intervalMs = 700) {
  for (let attempt = 0; attempt < attempts; attempt++) {
    await new Promise(resolve => setTimeout(resolve, intervalMs));
    try {
      const remote = await fetchRemoteOrders();
      const created = remote.find(order => typeof order.remoteId === "number" && !knownIds.has(order.remoteId));
      if (created) return created;
    } catch {
      // Keep polling; a transient failure should not lose the shopper's order.
    }
  }
  return undefined;
}

// â”€â”€â”€ Cart fly context â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
const CartCtx = createContext<{ fly:(srcRect:DOMRect, img:string)=>void; cartIconRef: React.RefObject<HTMLButtonElement|null> }>({
  fly: () => {},
  cartIconRef: { current: null },
});

// â”€â”€â”€ Custom cursor â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function Cursor() {
  const dot  = useRef<HTMLDivElement>(null);
  const ring = useRef<HTMLDivElement>(null);
  const pos  = useRef({ x:0, y:0 });
  const mse  = useRef({ x:0, y:0 });
  useEffect(() => {
    const move  = (e:MouseEvent) => { mse.current = { x:e.clientX, y:e.clientY }; };
    const hover = (e:MouseEvent) => {
      const t = (e.target as HTMLElement).closest("a,button,[data-cursor]");
      if (ring.current) {
        ring.current.style.width  = t ? "56px" : "32px";
        ring.current.style.height = t ? "56px" : "32px";
        ring.current.style.opacity = t ? "0.7" : "0.35";
        ring.current.style.borderColor = t ? gold : `${gold}80`;
      }
    };
    window.addEventListener("mousemove", move);
    window.addEventListener("mouseover", hover);
    let raf:number;
    const tick = () => {
      pos.current.x += (mse.current.x - pos.current.x) * 0.10;
      pos.current.y += (mse.current.y - pos.current.y) * 0.10;
      if (dot.current)  dot.current.style.transform  = `translate(${mse.current.x}px,${mse.current.y}px) translate(-50%,-50%)`;
      if (ring.current) ring.current.style.transform = `translate(${pos.current.x}px,${pos.current.y}px) translate(-50%,-50%)`;
      raf = requestAnimationFrame(tick);
    };
    raf = requestAnimationFrame(tick);
    return () => { window.removeEventListener("mousemove",move); window.removeEventListener("mouseover",hover); cancelAnimationFrame(raf); };
  }, []);
  return (
    <div className="pointer-events-none fixed inset-0 z-[200] hidden md:block">
      <div ref={dot}  className="absolute w-1.5 h-1.5 rounded-full" style={{ background:gold, boxShadow:`0 0 10px ${gold}` }} />
      <div ref={ring} className="absolute rounded-full transition-all duration-200" style={{ width:32,height:32,border:`1px solid ${gold}80`,opacity:0.35 }} />
    </div>
  );
}

// â”€â”€â”€ Preloader â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function Preloader({ onDone }:{ onDone:()=>void }) {
  const wrapRef  = useRef<HTMLDivElement>(null);
  const countRef = useRef<HTMLSpanElement>(null);
  const lineRef  = useRef<HTMLDivElement>(null);
  const logoRef  = useRef<HTMLDivElement>(null);
  useLayoutEffect(() => {
    const obj = { val:0 };
    const tl = gsap.timeline({ onComplete:() => {
      gsap.to(wrapRef.current, { yPercent:-100, duration:0.8, ease:"expo.inOut", delay:0.1, onComplete:onDone });
    }});
    tl.from(logoRef.current, { opacity:0, y:24, duration:0.6, ease:"power3.out" })
      .to(obj, {
        val:100, duration:2, ease:"power2.inOut",
        onUpdate() { if (countRef.current) countRef.current.textContent = String(Math.floor(obj.val)).padStart(2,"0") + "%"; },
      }, 0)
      .to(lineRef.current, { width:"100%", duration:2, ease:"power2.inOut" }, 0);
  }, []);
  return (
    <div ref={wrapRef} className="fixed inset-0 z-[300] flex flex-col items-center justify-center" style={{ background:bg }}>
      <div ref={logoRef} className="text-center mb-12">
        <p className="text-5xl tracking-[0.5em] uppercase mb-3" style={{ fontFamily:bodoni, fontStyle:"italic", color:gold }}>AVRA</p>
        <p className="text-[10px] tracking-[0.5em] uppercase" style={{ color:`${fg}28` }}>Move. Empower. Become.</p>
      </div>
      <div className="w-56 h-px mb-5 overflow-hidden" style={{ background:"rgba(255,255,255,0.07)" }}>
        <div ref={lineRef} className="h-full" style={{ width:"0%", background:goldGrad }} />
      </div>
      <span ref={countRef} className="text-[11px] tracking-[0.4em]" style={{ color:`${fg}30`, fontVariantNumeric:"tabular-nums" }}>00%</span>
    </div>
  );
}

// â”€â”€â”€ Cart Drawer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function CartDrawer({ cart, discount, open, onClose, onUpdateQty, onCheckout }:
  { cart:CartItem[]; discount:MemberDiscount; open:boolean; onClose:()=>void; onUpdateQty:(id:number,d:number)=>void; onCheckout:()=>void }) {
  const gross    = cart.reduce((s,i) => s + i.price * i.quantity, 0);
  const total    = cart.reduce((s,i) => s + memberUnitPrice(i.price, discount.percentage) * i.quantity, 0);
  const saving   = gross - total;
  const listRef  = useRef<HTMLDivElement>(null);
  const totalRef = useRef<HTMLSpanElement>(null);
  const prevTotal = useRef(total);

  // Animate items in when cart opens
  useEffect(() => {
    if (!open || !listRef.current) return;
    const items = listRef.current.querySelectorAll(".cart-item");
    gsap.from(items, { opacity:0, x:32, duration:0.5, stagger:0.08, ease:"power3.out", delay:0.15 });
  }, [open]);

  // Animate total counter on change
  useEffect(() => {
    if (!totalRef.current || total === prevTotal.current) return;
    const obj = { val: prevTotal.current };
    gsap.to(obj, {
      val: total, duration:0.5, ease:"power2.out",
      onUpdate() { if (totalRef.current) totalRef.current.textContent = `R${Math.round(obj.val).toLocaleString()}`; },
    });
    prevTotal.current = total;
  }, [total]);

  return (
    <AnimatePresence>
      {open && <>
        <motion.div key="ov" initial={{opacity:0}} animate={{opacity:1}} exit={{opacity:0}}
          className="fixed inset-0 z-40 bg-black/70" style={{ backdropFilter:"blur(8px)" }} onClick={onClose} />
        <motion.div key="dr" initial={{x:"100%"}} animate={{x:0}} exit={{x:"100%"}}
          transition={{ type:"spring", damping:28, stiffness:280 }}
          className="fixed right-0 top-0 h-full w-full max-w-[420px] z-50 flex flex-col"
          style={{ ...G.dark, borderLeft:"1px solid rgba(255,255,255,0.10)", borderRadius:0 }}>
          <Shine />

          {/* Header */}
          <div className="flex items-center justify-between px-7 py-5" style={{ borderBottom:"1px solid rgba(255,255,255,0.07)" }}>
            <Bracket><span className="text-[11px] tracking-[0.35em] uppercase" style={{ color:gold }}>Your Bag</span></Bracket>
            <motion.button aria-label="Close cart" whileHover={{ rotate:90 }} transition={{ duration:0.2 }}
              onClick={onClose} className="p-2 rounded-full hover:bg-white/10" style={{ color:`${fg}60` }}>
              <X size={17}/>
            </motion.button>
          </div>

          {/* Items */}
          <div ref={listRef} className="flex-1 overflow-y-auto px-7 py-6 space-y-4">
            {cart.length === 0
              ? <motion.div initial={{opacity:0,y:20}} animate={{opacity:1,y:0}} transition={{delay:0.2}}
                  className="text-center mt-24 flex flex-col items-center gap-4" style={{ color:`${fg}22` }}>
                  <ShoppingBag size={56} strokeWidth={1}/>
                  <Bracket><span className="text-[11px] tracking-widest uppercase">Empty bag</span></Bracket>
                </motion.div>
              : cart.map(item => (
                <AnimatePresence key={item.id}>
                  <motion.div className="cart-item relative rounded-2xl overflow-hidden flex gap-4 p-4"
                    style={{ ...G.soft }}
                    exit={{ opacity:0, x:60, height:0, marginBottom:0, paddingTop:0, paddingBottom:0 }}
                    transition={{ duration:0.35, ease:"power3.in" as never }}>
                    <Shine />
                    {/* Image with hover zoom */}
                    <div className="w-20 h-24 rounded-xl overflow-hidden bg-[#111118] shrink-0">
                      <motion.img src={resolveImage(item.img1)} alt={item.name}
                        className="w-full h-full object-cover object-top"
                        whileHover={{ scale:1.12 }} transition={{ duration:0.4 }} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium mb-0.5 truncate" style={{ color:fg }}>{item.name}</p>
                      <p className="text-sm mb-3 flex items-baseline gap-2">
                        <span style={{ color:gold }}>R{memberUnitPrice(item.price, discount.percentage).toLocaleString()}</span>
                        {saving > 0 && <span className="text-xs line-through" style={{ color:`${fg}30` }}>R{item.price.toLocaleString()}</span>}
                      </p>
                      {/* Qty controls */}
                      <div className="flex items-center gap-2">
                        {([-1,1] as const).map(d => (
                          <motion.button key={d}
                            whileTap={{ scale:0.82 }}
                            onClick={() => onUpdateQty(item.id,d)}
                            className="w-7 h-7 rounded-full flex items-center justify-center relative overflow-hidden"
                            style={{ ...G.frosted }}>
                            <Shine />
                            {d === -1 ? <Minus size={10}/> : <Plus size={10}/>}
                          </motion.button>
                        ))}
                        <motion.span key={item.quantity}
                          initial={{ scale:1.4, color:gold }} animate={{ scale:1, color:fg }}
                          transition={{ duration:0.3 }}
                          className="text-sm w-6 text-center font-medium">
                          {item.quantity}
                        </motion.span>
                      </div>
                    </div>
                    <p className="text-sm font-medium shrink-0" style={{ color:fg }}>R{(memberUnitPrice(item.price, discount.percentage)*item.quantity).toLocaleString()}</p>
                  </motion.div>
                </AnimatePresence>
              ))
            }
          </div>

          {/* Footer */}
          {cart.length > 0 && (
            <motion.div initial={{opacity:0,y:24}} animate={{opacity:1,y:0}} transition={{delay:0.25}}
              className="px-7 py-6" style={{ borderTop:"1px solid rgba(255,255,255,0.07)" }}>
              {saving > 0 && (
                <div className="flex justify-between items-center mb-2 text-xs">
                  <Bracket><span className="tracking-widest uppercase" style={{ color:gold }}>{discount.label || "Member discount"} ({discount.percentage}%)</span></Bracket>
                  <span style={{ color:gold }}>-{money(saving)}</span>
                </div>
              )}
              <div className="flex justify-between items-center mb-2">
                <Bracket><span className="text-[11px] tracking-widest uppercase" style={{ color:`${fg}45` }}>Subtotal</span></Bracket>
                <span ref={totalRef} className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>R{total.toLocaleString()}</span>
              </div>
              <p className="text-[10px] tracking-widest uppercase mb-5" style={{ color:`${fg}30` }}>
                {total >= FREE_SHIPPING_THRESHOLD ? "Complimentary delivery unlocked" : `${money(FREE_SHIPPING_THRESHOLD - total)} away from free delivery`}
              </p>
              <GoldBtn onClick={onCheckout} full>
                <Bracket>Proceed to Checkout</Bracket>
              </GoldBtn>
            </motion.div>
          )}
        </motion.div>
      </>}
    </AnimatePresence>
  );
}

// â”€â”€â”€ Checkout â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function CheckoutField({ label, children }:{ label:string; children:React.ReactNode }) {
  return (
    <label className="block">
      <span className="block text-[9px] tracking-[0.3em] uppercase mb-1.5" style={{ color:`${fg}38` }}>{label}</span>
      {children}
    </label>
  );
}

const checkoutInputClass = "w-full px-4 py-3 rounded-2xl text-sm outline-none";

const CHECKOUT_STEPS = [
  { label:"Delivery", Icon:Truck },
  { label:"Payment",  Icon:CreditCard },
  { label:"Review",   Icon:ReceiptText },
] as const;

function CheckoutModal({ open, cart, discount, profile, onClose, onPlaceOrder }:
  {
    open:boolean;
    cart:CartItem[];
    discount:MemberDiscount;
    profile:UserProfileDto|null;
    onClose:()=>void;
    onPlaceOrder:(details:ShippingDetails, payment:PaymentSummary, card:CardDetails|null)=>Promise<boolean>;
  }) {
  const [step, setStep] = useState(0);
  const [details, setDetails] = useState<ShippingDetails>(() => emptyShippingDetails(profile));
  const [method, setMethod] = useState<PaymentMethod>("Card");
  const [card, setCard] = useState({ name:"", number:"", expiry:"", cvc:"" });
  const [error, setError] = useState<string|null>(null);
  const [busy, setBusy] = useState(false);

  const lines: OrderLine[] = cart.map(item => ({ productId:item.id, name:item.name, price:item.price, quantity:item.quantity, image:item.img1 }));
  const totals = calcTotals(lines, discount.percentage);
  const cardDigits = card.number.replace(/\D/g, "");

  useEffect(() => {
    if (!open) return;
    setStep(0);
    setError(null);
    setBusy(false);
    setCard({ name:"", number:"", expiry:"", cvc:"" });
    setDetails(prev => ({ ...emptyShippingDetails(profile), ...Object.fromEntries(Object.entries(prev).filter(([, value]) => value)) } as ShippingDetails));
  }, [open, profile]);

  const setField = (field: keyof ShippingDetails, value: string) => setDetails(prev => ({ ...prev, [field]: value }));

  const validateDetails = () => {
    const required: Array<[keyof ShippingDetails, string]> = [
      ["firstName","first name"], ["lastName","last name"], ["email","email"], ["phone","phone number"],
      ["line1","street address"], ["city","city"], ["province","province"], ["postalCode","postal code"],
    ];
    const missing = required.find(([field]) => !details[field].trim());
    if (missing) return `Please enter your ${missing[1]}.`;
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(details.email.trim())) return "Please enter a valid email address.";
    return null;
  };

  const validatePayment = () => {
    if (method !== "Card") return null;
    if (!card.name.trim()) return "Please enter the name on the card.";
    if (cardDigits.length < 13 || cardDigits.length > 19) return "Please enter a valid card number.";
    if (!/^(0[1-9]|1[0-2])\s?\/\s?\d{2}$/.test(card.expiry.trim())) return "Please enter the expiry as MM/YY.";
    if (!/^\d{3,4}$/.test(card.cvc.trim())) return "Please enter a valid CVC.";
    return null;
  };

  const next = () => {
    const problem = step === 0 ? validateDetails() : step === 1 ? validatePayment() : null;
    if (problem) { setError(problem); return; }
    setError(null);
    setStep(current => Math.min(current + 1, CHECKOUT_STEPS.length - 1));
  };

  const back = () => { setError(null); setStep(current => Math.max(current - 1, 0)); };

  const submit = async () => {
    setBusy(true);
    setError(null);
    const payment: PaymentSummary = method === "Card"
      ? { method, cardBrand: cardBrandOf(cardDigits), last4: cardDigits.slice(-4) }
      : { method };

    try {
      const placed = await onPlaceOrder(details, payment, method === "Card" ? card : null);
      if (!placed) setError("We could not place your order. Please try again.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <AnimatePresence>
      {open && <>
        <motion.div key="checkout-ov" initial={{opacity:0}} animate={{opacity:1}} exit={{opacity:0}}
          className="fixed inset-0 z-[60] bg-black/80" style={{ backdropFilter:"blur(10px)" }} onClick={onClose} />
        <motion.div key="checkout-panel" initial={{opacity:0, y:40, scale:0.97}} animate={{opacity:1, y:0, scale:1}} exit={{opacity:0, y:30, scale:0.97}}
          transition={{ type:"spring", damping:26, stiffness:260 }}
          className="fixed inset-x-4 top-[6vh] bottom-[6vh] md:inset-x-auto md:left-1/2 md:-translate-x-1/2 md:w-[min(980px,92vw)] z-[61] flex flex-col rounded-3xl overflow-hidden"
          style={{ ...G.dark }}>
          <Shine gold />

          <div className="flex items-center justify-between px-7 py-5" style={{ borderBottom:"1px solid rgba(255,255,255,0.07)" }}>
            <div>
              <Bracket><span className="text-[11px] tracking-[0.35em] uppercase" style={{ color:gold }}>Checkout</span></Bracket>
              <p className="text-xs mt-1" style={{ color:`${fg}36` }}>{cart.reduce((sum,item)=>sum+item.quantity,0)} item(s) Â· {money(totals.total)}</p>
            </div>
            <button aria-label="Close checkout" onClick={onClose} className="p-2 rounded-full hover:bg-white/10" style={{ color:`${fg}60` }}><X size={17}/></button>
          </div>

          {/* Step rail */}
          <div className="flex items-center gap-3 px-7 py-4" style={{ borderBottom:"1px solid rgba(255,255,255,0.05)" }}>
            {CHECKOUT_STEPS.map((entry, index) => (
              <div key={entry.label} className="flex items-center gap-3 flex-1">
                <div className="flex items-center gap-2.5">
                  <div className="w-8 h-8 rounded-full flex items-center justify-center shrink-0"
                    style={index <= step ? { background:goldGrad, color:bg } : { ...G.frosted, color:`${fg}38` }}>
                    {index < step ? <Check size={13}/> : <entry.Icon size={13}/>}
                  </div>
                  <span className="text-[9px] tracking-[0.3em] uppercase hidden sm:inline" style={{ color:index <= step ? gold : `${fg}30` }}>{entry.label}</span>
                </div>
                {index < CHECKOUT_STEPS.length - 1 && (
                  <div className="flex-1 h-px" style={{ background:index < step ? goldGrad : "rgba(255,255,255,0.08)" }} />
                )}
              </div>
            ))}
          </div>

          <div className="flex-1 overflow-y-auto px-7 py-7 grid lg:grid-cols-[1.25fr_1fr] gap-6">
            <div>
              <AnimatePresence mode="wait">
                {step === 0 && (
                  <motion.div key="step-details" initial={{opacity:0,x:24}} animate={{opacity:1,x:0}} exit={{opacity:0,x:-24}} transition={{duration:0.25}} className="space-y-4">
                    <div className="grid sm:grid-cols-2 gap-3">
                      <CheckoutField label="First name">
                        <input value={details.firstName} onChange={e=>setField("firstName", e.target.value)} autoComplete="given-name" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                      <CheckoutField label="Last name">
                        <input value={details.lastName} onChange={e=>setField("lastName", e.target.value)} autoComplete="family-name" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                    </div>
                    <div className="grid sm:grid-cols-2 gap-3">
                      <CheckoutField label="Email">
                        <input type="email" value={details.email} onChange={e=>setField("email", e.target.value)} autoComplete="email" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                      <CheckoutField label="Phone">
                        <input type="tel" value={details.phone} onChange={e=>setField("phone", e.target.value)} autoComplete="tel" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                    </div>
                    <CheckoutField label="Street address">
                      <input value={details.line1} onChange={e=>setField("line1", e.target.value)} autoComplete="address-line1" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                    </CheckoutField>
                    <CheckoutField label="Apartment, suite (optional)">
                      <input value={details.line2} onChange={e=>setField("line2", e.target.value)} autoComplete="address-line2" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                    </CheckoutField>
                    <div className="grid sm:grid-cols-3 gap-3">
                      <CheckoutField label="City">
                        <input value={details.city} onChange={e=>setField("city", e.target.value)} autoComplete="address-level2" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                      <CheckoutField label="Province">
                        <input value={details.province} onChange={e=>setField("province", e.target.value)} autoComplete="address-level1" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                      <CheckoutField label="Postal code">
                        <input value={details.postalCode} onChange={e=>setField("postalCode", e.target.value)} autoComplete="postal-code" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                      </CheckoutField>
                    </div>
                    <CheckoutField label="Country">
                      <input value={details.country} onChange={e=>setField("country", e.target.value)} autoComplete="country-name" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                    </CheckoutField>
                  </motion.div>
                )}

                {step === 1 && (
                  <motion.div key="step-payment" initial={{opacity:0,x:24}} animate={{opacity:1,x:0}} exit={{opacity:0,x:-24}} transition={{duration:0.25}} className="space-y-4">
                    <div className="grid sm:grid-cols-3 gap-2">
                      {PAYMENT_METHODS.map(option => (
                        <button key={option} type="button" onClick={()=>{setMethod(option);setError(null);}}
                          className="relative rounded-2xl px-4 py-3 text-[10px] font-bold tracking-widest uppercase overflow-hidden"
                          style={method===option ? { background:goldGrad, color:bg } : { ...G.frosted, color:`${fg}52` }}>
                          {method!==option && <Shine />}
                          {option}
                        </button>
                      ))}
                    </div>

                    {method === "Card" && (
                      <div className="space-y-4">
                        <CheckoutField label="Name on card">
                          <input value={card.name} onChange={e=>setCard(prev=>({...prev, name:e.target.value}))} autoComplete="cc-name" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                        </CheckoutField>
                        <CheckoutField label="Card number">
                          <input inputMode="numeric" value={card.number} maxLength={23} placeholder="0000 0000 0000 0000"
                            onChange={e=>setCard(prev=>({...prev, number:e.target.value.replace(/[^\d\s]/g,"")}))}
                            autoComplete="cc-number" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                        </CheckoutField>
                        <div className="grid grid-cols-2 gap-3">
                          <CheckoutField label="Expiry (MM/YY)">
                            <input inputMode="numeric" value={card.expiry} maxLength={5} placeholder="08/29"
                              onChange={e=>setCard(prev=>({...prev, expiry:e.target.value}))}
                              autoComplete="cc-exp" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                          </CheckoutField>
                          <CheckoutField label="CVC">
                            <input inputMode="numeric" value={card.cvc} maxLength={4} placeholder="123"
                              onChange={e=>setCard(prev=>({...prev, cvc:e.target.value.replace(/\D/g,"")}))}
                              autoComplete="cc-csc" className={checkoutInputClass} style={{ ...G.frosted, color:fg }} />
                          </CheckoutField>
                        </div>
                        <p className="text-[10px] leading-relaxed" style={{ color:`${fg}30` }}>
                          Card details are used to authorise this purchase only. AVRA stores nothing beyond the card brand and last four digits.
                        </p>
                      </div>
                    )}

                    {method === "EFT / Bank Transfer" && (
                      <div className="relative rounded-2xl p-5 overflow-hidden text-sm leading-relaxed" style={{ ...G.soft, color:`${fg}55` }}>
                        <Shine />
                        Banking details and your order reference are emailed to you the moment the order is placed. Your pieces ship as soon as payment reflects.
                      </div>
                    )}

                    {method === "Cash on Delivery" && (
                      <div className="relative rounded-2xl p-5 overflow-hidden text-sm leading-relaxed" style={{ ...G.soft, color:`${fg}55` }}>
                        <Shine />
                        Pay the courier on arrival. Please have the exact amount ready â€” {money(totals.total)}.
                      </div>
                    )}
                  </motion.div>
                )}

                {step === 2 && (
                  <motion.div key="step-review" initial={{opacity:0,x:24}} animate={{opacity:1,x:0}} exit={{opacity:0,x:-24}} transition={{duration:0.25}} className="space-y-4">
                    <div className="relative rounded-2xl p-5 overflow-hidden" style={{ ...G.soft }}>
                      <Shine />
                      <Bracket className="mb-3 block"><span className="text-[9px] tracking-[0.3em] uppercase" style={{ color:gold }}>Deliver to</span></Bracket>
                      <p className="text-sm whitespace-pre-line leading-relaxed" style={{ color:`${fg}62` }}>{formatAddress(details)}</p>
                    </div>
                    <div className="relative rounded-2xl p-5 overflow-hidden" style={{ ...G.soft }}>
                      <Shine />
                      <Bracket className="mb-3 block"><span className="text-[9px] tracking-[0.3em] uppercase" style={{ color:gold }}>Payment</span></Bracket>
                      <p className="text-sm" style={{ color:`${fg}62` }}>
                        {method === "Card" ? `${cardBrandOf(cardDigits)} ending ${cardDigits.slice(-4) || "----"}` : method}
                      </p>
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>

            {/* Summary */}
            <div className="relative rounded-3xl p-6 overflow-hidden h-fit" style={{ ...G.gold }}>
              <Shine gold />
              <Bracket className="mb-4 block"><span className="text-[9px] tracking-[0.3em] uppercase" style={{ color:gold }}>Order Summary</span></Bracket>
              <div className="space-y-3 mb-5 max-h-56 overflow-y-auto">
                {lines.map(line => (
                  <div key={line.productId} className="flex items-center gap-3">
                    <div className="w-11 h-14 rounded-lg overflow-hidden bg-[#111118] shrink-0">
                      {line.image && <img src={resolveImage(line.image)} alt="" className="w-full h-full object-cover object-top" />}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-xs font-medium truncate" style={{ color:fg }}>{line.name}</p>
                      <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}34` }}>Qty {line.quantity}</p>
                    </div>
                    <p className="text-xs shrink-0" style={{ color:fg }}>{money(line.price * line.quantity)}</p>
                  </div>
                ))}
              </div>
              <div className="space-y-2 pt-4 text-xs" style={{ borderTop:"1px solid rgba(191,155,94,0.25)", color:`${fg}48` }}>
                <div className="flex justify-between"><span>Subtotal</span><span style={{ color:fg }}>{money(totals.subtotal)}</span></div>
                {totals.discount > 0 && (
                  <div className="flex justify-between"><span>{discount.label || "Member discount"} ({discount.percentage}%)</span><span style={{ color:gold }}>-{money(totals.discount)}</span></div>
                )}
                <div className="flex justify-between"><span>Delivery</span><span style={{ color:fg }}>{totals.shipping === 0 ? "Complimentary" : money(totals.shipping)}</span></div>
                <div className="flex justify-between"><span>VAT (15%)</span><span style={{ color:fg }}>{money(totals.tax)}</span></div>
              </div>
              <div className="flex justify-between items-center pt-4 mt-4" style={{ borderTop:"1px solid rgba(191,155,94,0.25)" }}>
                <Bracket><span className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}45` }}>Total</span></Bracket>
                <span className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{money(totals.total)}</span>
              </div>
            </div>
          </div>

          <div className="px-7 py-5 flex flex-col sm:flex-row items-center gap-3" style={{ borderTop:"1px solid rgba(255,255,255,0.07)" }}>
            {error && <p className="text-xs flex-1 order-2 sm:order-1" style={{ color:"#FCA5A5" }}>{error}</p>}
            <div className="flex items-center gap-3 sm:ml-auto order-1 sm:order-2 w-full sm:w-auto">
              {step > 0 && (
                <button onClick={back} className="avra-btn relative flex-1 sm:flex-none px-6 py-3.5 rounded-full text-xs font-semibold tracking-widest uppercase overflow-hidden" style={{ ...G.frosted, color:`${fg}70` }}>
                  <Shine />
                  <span className="flex items-center justify-center gap-2"><ArrowLeft size={12}/> Back</span>
                </button>
              )}
              {step < CHECKOUT_STEPS.length - 1
                ? <GoldBtn onClick={next} className="flex-1 sm:flex-none justify-center"><Bracket><span className="flex items-center gap-2">Continue <ArrowRight size={11}/></span></Bracket></GoldBtn>
                : <button disabled={busy} onClick={submit} className="avra-btn flex-1 sm:flex-none px-8 py-4 rounded-full text-xs font-semibold tracking-widest uppercase disabled:opacity-60" style={{ background:goldGrad, color:bg, boxShadow:"0 4px 28px rgba(191,155,94,0.4)" }}>
                    <Bracket>{busy ? "Placing Order" : `Pay ${money(totals.total)}`}</Bracket>
                  </button>
              }
            </div>
          </div>
        </motion.div>
      </>}
    </AnimatePresence>
  );
}

// â”€â”€â”€ Receipt â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function ReceiptModal({ order, celebrate, onClose }:{ order:OrderRecord|null; celebrate:boolean; onClose:()=>void }) {
  const placedOn = order ? new Date(order.createdAt) : null;

  return (
    <AnimatePresence>
      {order && <>
        <motion.div key="receipt-ov" initial={{opacity:0}} animate={{opacity:1}} exit={{opacity:0}}
          className="fixed inset-0 z-[70] bg-black/85 avra-no-print" style={{ backdropFilter:"blur(10px)" }} onClick={onClose} />
        <motion.div key="receipt-panel" initial={{opacity:0, y:40, scale:0.97}} animate={{opacity:1, y:0, scale:1}} exit={{opacity:0, y:30, scale:0.97}}
          transition={{ type:"spring", damping:26, stiffness:260 }}
          className="avra-receipt fixed inset-x-4 top-[5vh] bottom-[5vh] md:inset-x-auto md:left-1/2 md:-translate-x-1/2 md:w-[min(680px,92vw)] z-[71] flex flex-col rounded-3xl overflow-hidden"
          style={{ ...G.dark }}>
          <Shine gold />

          <div className="flex items-center justify-between px-7 py-5 avra-no-print" style={{ borderBottom:"1px solid rgba(255,255,255,0.07)" }}>
            <Bracket><span className="text-[11px] tracking-[0.35em] uppercase" style={{ color:gold }}>Receipt</span></Bracket>
            <div className="flex items-center gap-1">
              <button aria-label="Print receipt" onClick={()=>window.print()} className="p-2 rounded-full hover:bg-white/10" style={{ color:`${fg}60` }}><Printer size={16}/></button>
              <button aria-label="Close receipt" onClick={onClose} className="p-2 rounded-full hover:bg-white/10" style={{ color:`${fg}60` }}><X size={17}/></button>
            </div>
          </div>

          <div className="flex-1 overflow-y-auto px-7 py-7">
            {celebrate && (
              <motion.div initial={{opacity:0, scale:0.9}} animate={{opacity:1, scale:1}} transition={{ type:"spring", damping:14 }}
                className="flex flex-col items-center text-center mb-8">
                <div className="w-14 h-14 rounded-full flex items-center justify-center mb-4" style={{ background:goldGrad, color:bg }}>
                  <Check size={24}/>
                </div>
                <h3 className="text-3xl mb-2" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>Thank you, {order.shipping.firstName || "friend"}</h3>
                <p className="text-sm" style={{ color:`${fg}42` }}>Your order is confirmed. A copy of this receipt is on its way to {order.shipping.email || "your inbox"}.</p>
              </motion.div>
            )}

            <div className="flex items-start justify-between gap-6 pb-6 mb-6" style={{ borderBottom:"1px solid rgba(255,255,255,0.08)" }}>
              <div>
                <p className="text-2xl tracking-[0.4em] uppercase mb-1" style={{ fontFamily:bodoni, fontStyle:"italic", color:gold }}>AVRA</p>
                <p className="text-[10px] tracking-[0.3em] uppercase" style={{ color:`${fg}28` }}>Move. Empower. Become.</p>
              </div>
              <div className="text-right">
                <p className="text-[9px] tracking-[0.3em] uppercase mb-1" style={{ color:`${fg}34` }}>Order</p>
                <p className="text-sm font-semibold" style={{ color:fg }}>{order.reference}</p>
                <p className="text-[11px] mt-1" style={{ color:`${fg}34` }}>
                  {placedOn && !Number.isNaN(placedOn.getTime()) ? placedOn.toLocaleString(undefined, { dateStyle:"medium", timeStyle:"short" }) : "â€”"}
                </p>
              </div>
            </div>

            <div className="grid sm:grid-cols-2 gap-5 mb-7">
              <div>
                <p className="text-[9px] tracking-[0.3em] uppercase mb-2" style={{ color:gold }}>Delivered to</p>
                <p className="text-sm whitespace-pre-line leading-relaxed" style={{ color:`${fg}58` }}>{formatAddress(order.shipping)}</p>
              </div>
              <div>
                <p className="text-[9px] tracking-[0.3em] uppercase mb-2" style={{ color:gold }}>Payment</p>
                <p className="text-sm" style={{ color:`${fg}58` }}>
                  {order.payment.method === "Card" && order.payment.last4
                    ? `${order.payment.cardBrand} ending ${order.payment.last4}`
                    : order.payment.method}
                </p>
                <p className="text-[9px] tracking-[0.3em] uppercase mt-4 mb-2" style={{ color:gold }}>Status</p>
                <span className="inline-block px-3 py-1 rounded-full text-[9px] font-bold tracking-widest uppercase" style={{ ...G.gold, color:gold }}>{order.status}</span>
              </div>
            </div>

            <div className="space-y-3 mb-6">
              {order.items.map(line => (
                <div key={line.productId} className="flex items-center gap-4 pb-3" style={{ borderBottom:"1px solid rgba(255,255,255,0.05)" }}>
                  {line.image && <img src={resolveImage(line.image)} alt="" className="w-12 h-15 rounded-lg object-cover object-top bg-[#111118]" style={{ height:60 }} />}
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium truncate" style={{ color:fg }}>{line.name}</p>
                    <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}34` }}>{money(line.price)} Ã— {line.quantity}</p>
                  </div>
                  <p className="text-sm shrink-0" style={{ color:fg }}>{money(line.price * line.quantity)}</p>
                </div>
              ))}
            </div>

            <div className="space-y-2 text-sm ml-auto max-w-xs" style={{ color:`${fg}45` }}>
              <div className="flex justify-between"><span>Subtotal</span><span style={{ color:fg }}>{money(order.totals.subtotal)}</span></div>
              {order.totals.discount > 0 && (
                <div className="flex justify-between"><span>Member discount</span><span style={{ color:gold }}>-{money(order.totals.discount)}</span></div>
              )}
              <div className="flex justify-between"><span>Delivery</span><span style={{ color:fg }}>{order.totals.shipping === 0 ? "Complimentary" : money(order.totals.shipping)}</span></div>
              <div className="flex justify-between"><span>VAT (15%)</span><span style={{ color:fg }}>{money(order.totals.tax)}</span></div>
              <div className="flex justify-between items-center pt-3 mt-1" style={{ borderTop:"1px solid rgba(191,155,94,0.28)" }}>
                <Bracket><span className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}45` }}>Total paid</span></Bracket>
                <span className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{money(order.totals.total)}</span>
              </div>
            </div>

            <p className="text-[10px] leading-relaxed text-center mt-10" style={{ color:`${fg}26` }}>
              Thank you for choosing AVRA. Returns accepted within 30 days with this receipt. hello@avra.co.za
            </p>
          </div>
        </motion.div>
      </>}
    </AnimatePresence>
  );
}

// â”€â”€â”€ Orders â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function OrdersPanel({ open, orders, onClose, onViewReceipt }:
  { open:boolean; orders:OrderRecord[]; onClose:()=>void; onViewReceipt:(order:OrderRecord)=>void }) {
  return (
    <AnimatePresence>
      {open && <>
        <motion.div key="orders-ov" initial={{opacity:0}} animate={{opacity:1}} exit={{opacity:0}}
          className="fixed inset-0 z-40 bg-black/70" style={{ backdropFilter:"blur(8px)" }} onClick={onClose} />
        <motion.div key="orders-panel" initial={{x:"100%"}} animate={{x:0}} exit={{x:"100%"}}
          transition={{ type:"spring", damping:28, stiffness:280 }}
          className="fixed right-0 top-0 h-full w-full max-w-[480px] z-50 flex flex-col"
          style={{ ...G.dark, borderLeft:"1px solid rgba(255,255,255,0.10)", borderRadius:0 }}>
          <Shine />
          <div className="flex items-center justify-between px-7 py-5" style={{ borderBottom:"1px solid rgba(255,255,255,0.07)" }}>
            <div>
              <Bracket><span className="text-[11px] tracking-[0.35em] uppercase" style={{ color:gold }}>Your Orders</span></Bracket>
              <p className="text-xs mt-1" style={{ color:`${fg}36` }}>{orders.length} order{orders.length === 1 ? "" : "s"} placed</p>
            </div>
            <button aria-label="Close orders" onClick={onClose} className="p-2 rounded-full hover:bg-white/10" style={{ color:`${fg}60` }}><X size={17}/></button>
          </div>

          <div className="flex-1 overflow-y-auto px-7 py-6 space-y-3">
            {orders.length === 0
              ? <div className="text-center mt-24 flex flex-col items-center gap-4" style={{ color:`${fg}22` }}>
                  <Package size={56} strokeWidth={1}/>
                  <Bracket><span className="text-[11px] tracking-widest uppercase">No orders yet</span></Bracket>
                </div>
              : orders.map(order => {
                  const placedOn = new Date(order.createdAt);
                  return (
                    <div key={order.reference} className="relative rounded-2xl p-5 overflow-hidden" style={{ ...G.soft }}>
                      <Shine />
                      <div className="flex items-start justify-between gap-4 mb-3">
                        <div>
                          <p className="text-sm font-semibold" style={{ color:fg }}>{order.reference}</p>
                          <p className="text-[10px] tracking-widest uppercase mt-0.5" style={{ color:`${fg}34` }}>
                            {Number.isNaN(placedOn.getTime()) ? "â€”" : placedOn.toLocaleDateString(undefined, { dateStyle:"medium" })}
                            {" Â· "}{order.items.reduce((sum,line)=>sum+line.quantity,0)} item(s)
                          </p>
                        </div>
                        <span className="px-3 py-1 rounded-full text-[9px] font-bold tracking-widest uppercase shrink-0" style={{ ...G.gold, color:gold }}>{order.status}</span>
                      </div>

                      <div className="flex -space-x-3 mb-4">
                        {order.items.slice(0,4).map(line => (
                          line.image
                            ? <img key={line.productId} src={resolveImage(line.image)} alt="" className="w-10 h-12 rounded-lg object-cover object-top bg-[#111118]" style={{ border:"1px solid rgba(255,255,255,0.12)" }} />
                            : <div key={line.productId} className="w-10 h-12 rounded-lg" style={{ ...G.frosted }} />
                        ))}
                      </div>

                      <div className="flex items-center justify-between gap-4">
                        <span className="text-xl" style={{ fontFamily:bodoni, color:gold }}>{money(order.totals.total)}</span>
                        <button onClick={()=>onViewReceipt(order)}
                          className="avra-btn relative px-5 py-2.5 rounded-full text-[9px] font-bold tracking-widest uppercase overflow-hidden"
                          style={{ ...G.frosted, color:fg }}>
                          <Shine />
                          <span className="flex items-center gap-2"><ReceiptText size={11}/> <Bracket>Receipt</Bracket></span>
                        </button>
                      </div>
                    </div>
                  );
                })
            }
          </div>
        </motion.div>
      </>}
    </AnimatePresence>
  );
}

type AccountMode = "login" | "register" | "profile";

function AccountPanel({ open, profile, onClose, onProfileChange }:
  { open:boolean; profile:UserProfileDto|null; onClose:()=>void; onProfileChange:(profile:UserProfileDto|null)=>void }) {
  const [mode, setMode] = useState<AccountMode>(profile ? "profile" : "login");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string|null>(null);
  const [form, setForm] = useState({ firstName:"", lastName:"", email:"", password:"", phoneNumber:"" });

  useEffect(() => {
    if (!open) return;
    setMessage(null);
    setMode(profile ? "profile" : "login");
    setForm({
      firstName: profile?.firstName ?? "",
      lastName: profile?.lastName ?? "",
      email: profile?.email ?? "",
      password: "",
      phoneNumber: profile?.phoneNumber ?? "",
    });
  }, [open, profile]);

  const setField = (field: keyof typeof form, value: string) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setMessage(null);

    try {
      if (mode === "register") {
        const auth = await avraApi.auth.register({
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          email: form.email.trim(),
          password: form.password,
          phoneNumber: form.phoneNumber.trim() || undefined,
        });
        onProfileChange(auth.user);
        setMode("profile");
        setMessage("Registration complete. Welcome to AVRA.");
      } else if (mode === "login") {
        const auth = await avraApi.auth.login({ email: form.email.trim(), password: form.password });
        onProfileChange(auth.user);
        setMode("profile");
        setMessage("You are signed in.");
      } else if (profile) {
        const updated = await avraApi.users.updateProfile(profile.id, {
          firstName: form.firstName.trim(),
          lastName: form.lastName.trim(),
          phoneNumber: form.phoneNumber.trim() || undefined,
        });
        onProfileChange(updated);
        setMessage("Profile updated.");
      }
    } catch (error) {
      console.warn("Account action failed", error);
      if (isApiUnavailable(error)) {
        const storedUser = getAvraUser();
        const localProfile: UserProfileDto = {
          id: profile?.id ?? storedUser.userId,
          email: form.email.trim() || profile?.email || storedUser.userEmail,
          firstName: form.firstName.trim() || profile?.firstName || "AVRA",
          lastName: form.lastName.trim() || profile?.lastName || "Guest",
          phoneNumber: form.phoneNumber.trim() || profile?.phoneNumber || null,
        };
        saveLocalProfile(localProfile);
        onProfileChange(localProfile);
        setMode("profile");
        setMessage("Saved locally while UserService is offline.");
      } else {
        setMessage(mode === "login" ? "Login failed. Check your details and try again." : "Account request failed. Please try again.");
      }
    } finally {
      setBusy(false);
    }
  };

  const logout = () => {
    avraApi.auth.logout();
    onProfileChange(null);
    setMode("login");
    setMessage("You are signed out.");
  };

  const isProfile = mode === "profile" && profile;
  const title = isProfile ? "Customer Profile" : mode === "register" ? "Create Account" : "Welcome Back";

  return (
    <AnimatePresence>
      {open && <>
        <motion.div key="account-ov" initial={{opacity:0}} animate={{opacity:1}} exit={{opacity:0}}
          className="fixed inset-0 z-40 bg-black/70" style={{ backdropFilter:"blur(8px)" }} onClick={onClose} />
        <motion.div key="account-panel" initial={{x:"100%"}} animate={{x:0}} exit={{x:"100%"}}
          transition={{ type:"spring", damping:28, stiffness:280 }}
          className="fixed right-0 top-0 h-full w-full max-w-[460px] z-50 flex flex-col"
          style={{ ...G.dark, borderLeft:"1px solid rgba(255,255,255,0.10)", borderRadius:0 }}>
          <Shine />
          <div className="flex items-center justify-between px-7 py-5" style={{ borderBottom:"1px solid rgba(255,255,255,0.07)" }}>
            <Bracket><span className="text-[11px] tracking-[0.35em] uppercase" style={{ color:gold }}>{title}</span></Bracket>
            <button aria-label="Close account" onClick={onClose} className="p-2 rounded-full hover:bg-white/10" style={{ color:`${fg}60` }}><X size={17}/></button>
          </div>

          <div className="flex-1 overflow-y-auto px-7 py-7">
            {profile && (
              <div className="relative rounded-3xl p-5 mb-6 overflow-hidden" style={{ ...G.gold }}>
                <Shine gold />
                <div className="flex items-center gap-4">
                  <div className="w-14 h-14 rounded-2xl flex items-center justify-center" style={{ background:goldGrad, color:bg }}>
                    <UserRound size={24}/>
                  </div>
                  <div>
                    <p className="text-base font-semibold" style={{ color:fg }}>{profile.firstName} {profile.lastName}</p>
                    <p className="text-xs" style={{ color:`${fg}40` }}>{profile.email}</p>
                  </div>
                </div>
              </div>
            )}

            {!profile && (
              <div className="grid grid-cols-2 gap-2 mb-6">
                {(["login", "register"] as AccountMode[]).map(tab => (
                  <button key={tab} onClick={()=>{ setMode(tab); setMessage(null); }}
                    className="relative rounded-full px-4 py-3 text-[10px] font-bold tracking-widest uppercase overflow-hidden"
                    style={mode===tab ? { background:goldGrad, color:bg } : { ...G.frosted, color:`${fg}52` }}>
                    {mode!==tab && <Shine />}
                    <Bracket>{tab === "login" ? "Login" : "Register"}</Bracket>
                  </button>
                ))}
              </div>
            )}

            <form onSubmit={submit} className="space-y-4">
              {(mode === "register" || isProfile) && (
                <div className="grid grid-cols-2 gap-3">
                  <input value={form.firstName} onChange={e=>setField("firstName", e.target.value)} required placeholder="First name" className="px-4 py-3 rounded-2xl text-sm outline-none" style={{ ...G.frosted, color:fg }} />
                  <input value={form.lastName} onChange={e=>setField("lastName", e.target.value)} required placeholder="Last name" className="px-4 py-3 rounded-2xl text-sm outline-none" style={{ ...G.frosted, color:fg }} />
                </div>
              )}
              <input type="email" value={form.email} onChange={e=>setField("email", e.target.value)} required disabled={!!isProfile}
                placeholder="Email address" className="w-full px-4 py-3 rounded-2xl text-sm outline-none disabled:opacity-60" style={{ ...G.frosted, color:fg }} />
              {!isProfile && (
                <input type="password" value={form.password} onChange={e=>setField("password", e.target.value)} required minLength={6}
                  placeholder="Password" className="w-full px-4 py-3 rounded-2xl text-sm outline-none" style={{ ...G.frosted, color:fg }} />
              )}
              {(mode === "register" || isProfile) && (
                <input value={form.phoneNumber} onChange={e=>setField("phoneNumber", e.target.value)} placeholder="Phone number" className="w-full px-4 py-3 rounded-2xl text-sm outline-none" style={{ ...G.frosted, color:fg }} />
              )}

              {message && <p className="text-xs leading-relaxed" style={{ color:message.includes("failed") ? "#FCA5A5" : gold }}>{message}</p>}

              <GoldBtn full>
                <Bracket>{busy ? "Working" : isProfile ? "Save Profile" : mode === "register" ? "Create Account" : "Login"}</Bracket>
              </GoldBtn>
            </form>

            {profile && (
              <button onClick={logout} className="mt-5 w-full relative rounded-2xl px-4 py-3 text-xs font-semibold tracking-widest uppercase overflow-hidden" style={{ ...G.frosted, color:`${fg}60` }}>
                <Shine />
                <span className="flex items-center justify-center gap-2"><LogOut size={13}/> <Bracket>Logout</Bracket></span>
              </button>
            )}
          </div>
        </motion.div>
      </>}
    </AnimatePresence>
  );
}

// â”€â”€â”€ Navbar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
function Navbar({ cartCount, orderCount, profile, onCartOpen, onAccountOpen, onOrdersOpen }:
  { cartCount:number; orderCount:number; profile:UserProfileDto|null; onCartOpen:()=>void; onAccountOpen:()=>void; onOrdersOpen:()=>void }) {
  const [open, setOpen] = useState(false);
  const navRef     = useRef<HTMLElement>(null);
  const cartBtnRef = useRef<HTMLButtonElement>(null);
  const { cartIconRef } = useContext(CartCtx);
  const links = [{l:"Shop",href:"/shop"},{l:"The Club",href:"/club"},{l:"Wellness",href:"/wellness"},{l:"Community",href:"/community"},{l:"Events",href:"/events"}];

  // Forward the ref to context
  useEffect(() => {
    if (cartBtnRef.current) (cartIconRef as React.MutableRefObject<HTMLButtonElement|null>).current = cartBtnRef.current;
  }, []);

  useLayoutEffect(() => {
    gsap.from(navRef.current, { yPercent:-100, opacity:0, duration:1, ease:"power3.out", delay:0.15 });
  }, []);

  // Shake on add
  const prevCount = useRef(cartCount);
  useEffect(() => {
    if (cartCount > prevCount.current && cartBtnRef.current) {
      gsap.fromTo(cartBtnRef.current, { x:-4 }, {
        x:0, duration:0.4, ease:"elastic.out(3, 0.4)",
        keyframes:[{ x:-5 },{ x:5 },{ x:-4 },{ x:4 },{ x:0 }],
      });
    }
    prevCount.current = cartCount;
  }, [cartCount]);

  return (
    <nav ref={navRef} className="fixed top-0 left-0 right-0 z-30 px-6"
      style={{ background:"rgba(5,5,14,0.30)", backdropFilter:"blur(32px) saturate(1.8)" }}>
      <div className="absolute bottom-0 left-0 right-0 h-px" style={{ background:"linear-gradient(90deg,transparent,rgba(191,155,94,0.15),transparent)" }} />
      <div className="max-w-7xl mx-auto flex items-center justify-between h-16">
        <Link to="/" className="text-xl tracking-[0.5em] uppercase" style={{ fontFamily:bodoni, fontStyle:"italic", color:gold }}>AVRA</Link>
        <div className="hidden md:flex items-center gap-8">
          {links.map(({l,href}) => (
            <Link key={l} to={href} className="text-[10px] tracking-widest uppercase transition-colors hover:text-[#BF9B5E]" style={{ color:`${fg}48` }}>
              <Bracket>{l}</Bracket>
            </Link>
          ))}
        </div>
        <div className="flex items-center gap-2">
          <button aria-label="Open your orders" onClick={onOrdersOpen} className="flex items-center gap-2 relative px-2.5 sm:px-3 py-2 rounded-full hover:bg-white/10 text-[10px] tracking-widest uppercase" style={{ color:`${fg}70` }}>
            <Package size={15}/>
            <span className="hidden lg:inline">Orders{orderCount > 0 ? ` (${orderCount})` : ""}</span>
          </button>
          <button aria-label={profile ? "Open customer profile" : "Open login and registration"} onClick={onAccountOpen} className="flex items-center gap-2 relative px-2.5 sm:px-3 py-2 rounded-full hover:bg-white/10 text-[10px] tracking-widest uppercase" style={{ color:`${fg}70` }}>
            <UserRound size={15}/>
            <span className="hidden sm:inline">{profile ? profile.firstName : "Account"}</span>
          </button>
          <button aria-label="Open cart" ref={cartBtnRef} onClick={onCartOpen} className="relative p-2.5 rounded-full hover:bg-white/10" style={{ color:`${fg}80` }}>
            <ShoppingBag size={18}/>
            <AnimatePresence>
              {cartCount > 0 && (
                <motion.span key={cartCount} initial={{scale:0}} animate={{scale:1}} exit={{scale:0}}
                  transition={{ type:"spring", stiffness:500, damping:20 }}
                  className="absolute -top-0.5 -right-0.5 w-5 h-5 rounded-full text-[10px] flex items-center justify-center font-bold"
                  style={{ background:goldGrad, color:bg }}>
                  {cartCount}
                </motion.span>
              )}
            </AnimatePresence>
          </button>
          <button className="md:hidden p-2.5 rounded-full hover:bg-white/10" style={{ color:`${fg}80` }} onClick={()=>setOpen(!open)}>
            {open ? <X size={18}/> : <Menu size={18}/>}
          </button>
        </div>
      </div>
      <AnimatePresence>
        {open && (
          <motion.div initial={{opacity:0,height:0}} animate={{opacity:1,height:"auto"}} exit={{opacity:0,height:0}}
            className="md:hidden overflow-hidden" style={{ borderTop:"1px solid rgba(255,255,255,0.06)" }}>
            <div className="py-4 space-y-1">
              {links.map(({l,href},i) => (
                <motion.div key={l} initial={{opacity:0,x:-20}} animate={{opacity:1,x:0}} transition={{delay:i*0.06}}>
                  <Link to={href} onClick={()=>setOpen(false)}
                    className="block text-[10px] tracking-widest uppercase py-3 px-2 rounded-lg hover:bg-white/08"
                    style={{ color:`${fg}50` }}>{l}</Link>
                </motion.div>
              ))}
              <button onClick={()=>{setOpen(false);onAccountOpen();}}
                className="block w-full text-left text-[10px] tracking-widest uppercase py-3 px-2 rounded-lg hover:bg-white/08"
                style={{ color:`${fg}50` }}>{profile ? "Customer Profile" : "Login / Register"}</button>
              <button onClick={()=>{setOpen(false);onOrdersOpen();}}
                className="block w-full text-left text-[10px] tracking-widest uppercase py-3 px-2 rounded-lg hover:bg-white/08"
                style={{ color:`${fg}50` }}>Orders &amp; Receipts</button>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </nav>
  );
}

// â”€â”€â”€ Root â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export default function App() {
  const [cart,     setCart]     = useState<CartItem[]>([]);
  const [cartOpen, setCartOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);
  const [checkoutOpen, setCheckoutOpen] = useState(false);
  const [ordersOpen, setOrdersOpen] = useState(false);
  const [orders, setOrders] = useState<OrderRecord[]>(() => readOrders());
  const [receipt, setReceipt] = useState<OrderRecord|null>(null);
  const [receiptIsNew, setReceiptIsNew] = useState(false);
  const [profile, setProfile] = useState<UserProfileDto|null>(() => getStoredProfile());
  const [membershipTiers, setMembershipTiers] = useState<MembershipTier[]>([]);
  const [discount, setDiscount] = useState<MemberDiscount>(NO_DISCOUNT);
  const [loaded,   setLoaded]   = useState(false);
  const [toast,    setToast]    = useState<string|null>(null);
  const { products, events, programs, communities } = useCatalogue();
  const toastRef    = useRef<HTMLDivElement>(null);
  const cartIconRef = useRef<HTMLButtonElement|null>(null);
  const totalItems  = cart.reduce((s,i)=>s+i.quantity,0);

  // Ticker
  useEffect(() => {
    const el = document.querySelector<HTMLElement>(".avra-ticker");
    if (!el) return;
    const w = el.scrollWidth / 2;
    gsap.to(el, { x:-w, duration:22, ease:"none", repeat:-1 });
  }, []);

  // Button hover GSAP
  useEffect(() => {
    if (!loaded) return;
    const btns = document.querySelectorAll<HTMLElement>(".avra-btn");
    const offs: Array<()=>void> = [];
    btns.forEach(btn => {
      const over = ()=>gsap.to(btn,{scale:1.05, duration:0.2,ease:"power2.out"});
      const out  = ()=>gsap.to(btn,{scale:1,   duration:0.22,ease:"power2.out"});
      const down = ()=>gsap.to(btn,{scale:0.95,duration:0.1, ease:"power2.out"});
      const up   = ()=>gsap.to(btn,{scale:1.05,duration:0.1, ease:"power2.out"});
      btn.addEventListener("mouseenter",over); btn.addEventListener("mouseleave",out);
      btn.addEventListener("mousedown",down);  btn.addEventListener("mouseup",up);
      offs.push(()=>{ btn.removeEventListener("mouseenter",over); btn.removeEventListener("mouseleave",out); btn.removeEventListener("mousedown",down); btn.removeEventListener("mouseup",up); });
    });
    return ()=>offs.forEach(f=>f());
  }, [loaded]);

  // Toast animation
  useEffect(() => {
    if (!toast || !toastRef.current) return;
    gsap.fromTo(toastRef.current, { opacity:0,y:-24 }, { opacity:1,y:0, duration:0.45,ease:"back.out(1.7)" });
    const tid = setTimeout(()=>{
      if (!toastRef.current) return;
      gsap.to(toastRef.current, { opacity:0,y:-16,duration:0.3,onComplete:()=>setToast(null) });
    }, 2400);
    return ()=>clearTimeout(tid);
  }, [toast]);

  useEffect(() => {
    const { userId } = getAvraUser();
    let cancelled = false;

    avraApi.cart.get(userId)
      .then(remoteCart => {
        if (!cancelled) {
          setCart(mapCartDto(remoteCart));
        }
      })
      .catch(error => console.warn("CartService unavailable or cart empty; using local cart state", error));

    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    if (!isAuthenticated()) return;
    let cancelled = false;

    const sync = () => {
      fetchRemoteOrders()
        .then(remoteOrders => {
          if (cancelled || remoteOrders.length === 0) return;
          setOrders(current => {
            // Remote status wins; the local record keeps the contact and payment detail.
            const reconciled = current.map(order => {
              const remote = remoteOrders.find(candidate => candidate.remoteId === order.remoteId);
              return remote ? { ...order, status: remote.status, items: remote.items } : order;
            });
            const merged = mergeOrders(reconciled, remoteOrders);
            saveOrders(merged);
            return merged;
          });
        })
        .catch(error => console.warn("Order history unavailable; using locally stored orders", error));
    };

    sync();
    // The saga moves an order through several states, so refresh while the panel is in use.
    const timer = setInterval(sync, 10_000);

    return () => { cancelled = true; clearInterval(timer); };
  }, [profile?.id]);

  useEffect(() => {
    if (!profile?.id) return;
    let cancelled = false;

    avraApi.users.getProfile(profile.id)
      .then(remoteProfile => {
        if (!cancelled) {
          setProfile(remoteProfile);
        }
      })
      .catch(error => console.warn("Unable to refresh customer profile from UserService", error));

    return () => { cancelled = true; };
  }, [profile?.id]);

  const handleProfileChange = (nextProfile: UserProfileDto|null) => {
    setProfile(nextProfile);
    if (nextProfile) {
      setToast(`Welcome, ${nextProfile.firstName}`);
    } else {
      setToast("Signed out");
    }
  };

  useEffect(() => {
    let cancelled = false;
    avraApi.memberships.plans()
      .then(items => {
        if (!cancelled) setMembershipTiers(items.map(mapMembershipPlanDto));
      })
      .catch(error => console.warn("UserService unavailable; no membership tiers to show", error));
    return () => { cancelled = true; };
  }, []);

  // Display only. Checkout re-resolves the rate server-side, so this cannot be used to underpay.
  useEffect(() => {
    if (!isAuthenticated()) { setDiscount(NO_DISCOUNT); return; }
    let cancelled = false;
    avraApi.memberships.myDiscount()
      .then(result => {
        if (!cancelled) setDiscount({ percentage: result.percentage, label: result.label });
      })
      .catch(error => console.warn("Unable to resolve the member discount; showing list prices", error));
    return () => { cancelled = true; };
  }, [profile?.id]);

  // Fly-to-cart animation
  const fly = useCallback((srcRect: DOMRect, img: string) => {
    const cartBtn = cartIconRef.current;
    if (!cartBtn) return;
    const destRect = cartBtn.getBoundingClientRect();

    // Create flying clone
    const clone = document.createElement("div");
    clone.style.cssText = `
      position:fixed;
      top:${srcRect.top}px; left:${srcRect.left}px;
      width:${srcRect.width}px; height:${srcRect.height}px;
      border-radius:12px; overflow:hidden;
      pointer-events:none; z-index:500;
      box-shadow: 0 8px 32px rgba(191,155,94,0.4);
    `;
    const cloneImg = document.createElement("img");
    cloneImg.src = img;
    cloneImg.style.cssText = "width:100%;height:100%;object-fit:cover;";
    clone.appendChild(cloneImg);
    document.body.appendChild(clone);

    const destX = destRect.left + destRect.width/2 - srcRect.left - srcRect.width/2;
    const destY = destRect.top  + destRect.height/2 - srcRect.top - srcRect.height/2;

    gsap.to(clone, {
      x: destX, y: destY, scale:0.08, opacity:0,
      duration:0.65, ease:"power3.in",
      onComplete: ()=>{ document.body.removeChild(clone); },
    });
  }, []);

  function addToCart(p: Product, srcRect: DOMRect, img: string) {
    fly(srcRect, img);
    setCart(prev=>{
      const ex = prev.find(i=>i.id===p.id);
      return ex ? prev.map(i=>i.id===p.id ? {...i,quantity:i.quantity+1}:i) : [...prev,{...p,quantity:1}];
    });
    const { userId } = getAvraUser();
    avraApi.cart.add(userId, {
      productId: p.id,
      productName: p.name,
      price: p.price,
      quantity: 1,
      image: p.img1,
    })
      .then(remoteCart => setCart(mapCartDto(remoteCart)))
      .catch(error => console.warn("Unable to sync add-to-cart with CartService", error));
    setToast(`${p.name} added to bag`);
  }
  function updateQty(id:number, d:number) {
    const current = cart.find(item => item.id === id);
    const nextQuantity = Math.max(0, (current?.quantity ?? 0) + d);
    setCart(prev=>prev.map(i=>i.id===id ? {...i,quantity:i.quantity+d}:i).filter(i=>i.quantity>0));
    const { userId } = getAvraUser();
    const sync = nextQuantity > 0
      ? avraApi.cart.updateQuantity(userId, id, nextQuantity)
      : avraApi.cart.remove(userId, id);

    sync
      .then(remoteCart => setCart(mapCartDto(remoteCart)))
      .catch(error => console.warn("Unable to sync cart quantity with CartService", error));
  }

  const openCheckout = () => {
    if (cart.length === 0) { setToast("Your bag is empty"); return; }
    setCartOpen(false);
    setCheckoutOpen(true);
  };

  const viewReceipt = (order: OrderRecord) => {
    setReceiptIsNew(false);
    setReceipt(order);
  };

  const placeOrder = async (details: ShippingDetails, payment: PaymentSummary, card: CardDetails|null) => {
    if (cart.length === 0) return false;

    // The basket and ordering APIs identify the buyer from the token, so a session is required.
    if (!isAuthenticated()) {
      setCheckoutOpen(false);
      setAccountOpen(true);
      setToast("Please sign in to complete checkout");
      return false;
    }

    const lines: OrderLine[] = cart.map(item => ({
      productId: item.id, name: item.name, price: item.price, quantity: item.quantity, image: item.img1,
    }));
    const cardDigits = card?.number.replace(/\D/g, "") ?? "";

    const payload: BasketCheckoutPayload = {
      userName: `${details.firstName} ${details.lastName}`.trim(),
      street: [details.line1, details.line2].filter(Boolean).join(", "),
      city: details.city,
      state: details.province,
      country: details.country,
      zipCode: details.postalCode,
      paymentMethod: PAYMENT_METHOD_CODES[payment.method],
      cardNumber: cardDigits,
      cardHolderName: card?.name ?? "",
      cardExpiration: card ? expiryToIso(card.expiry) : new Date().toISOString(),
      cardSecurityNumber: card?.cvc ?? "",
      cardTypeId: card ? cardTypeIdOf(cardDigits) : 0,
      buyer: details.email,
    };

    const knownIds = new Set(orders.map(order => order.remoteId).filter((id): id is number => typeof id === "number"));
    let placed: OrderRecord|undefined;

    try {
      await avraApi.checkout.submit(payload);
      // Checkout is only accepted here; the ordering service creates the order off the event bus.
      placed = await waitForNewOrder(knownIds);
    } catch (error) {
      if (!isApiUnavailable(error)) {
        console.warn("Checkout was rejected", error);
        return false;
      }
      console.warn("Checkout service offline; recording the order locally", error);
    }

    const order: OrderRecord = placed
      ? { ...placed, shipping: details, payment }
      : {
          reference: makeReference(),
          createdAt: new Date().toISOString(),
          status: "Order placed",
          items: lines,
          totals: calcTotals(lines, discount.percentage),
          shipping: details,
          payment,
        };

    setOrders(current => {
      const next = mergeOrders([order, ...current.filter(existing => existing.remoteId !== order.remoteId)], []);
      saveOrders(next);
      return next;
    });
    setCart([]);
    setCheckoutOpen(false);
    setReceiptIsNew(true);
    setReceipt(order);
    setToast(`Order ${order.reference} confirmed`);

    return true;
  };

  return (
    <CartCtx.Provider value={{ fly, cartIconRef }}>
      <StoreCtx.Provider value={{
        products, events, programs, communities, membershipTiers, discount,
        addToCart,
        openCheckout: () => setCheckoutOpen(true),
        openOrders: () => setOrdersOpen(true),
      }}>
      <div className="min-h-screen overflow-x-hidden" style={{ background:bg, color:fg, fontFamily:"'Plus Jakarta Sans',sans-serif", cursor:"none" }}>
        <Cursor />
        <AnimatePresence>{!loaded && <Preloader key="pre" onDone={()=>setLoaded(true)} />}</AnimatePresence>

        {toast && (
          <div ref={toastRef} className="fixed top-20 left-1/2 z-[100] px-6 py-3 rounded-full text-xs font-bold tracking-widest uppercase pointer-events-none"
            style={{ transform:"translateX(-50%)", background:goldGrad, color:bg, boxShadow:"0 8px 32px rgba(191,155,94,0.45)" }}>
            <Bracket>{toast}</Bracket>
          </div>
        )}

        <CartDrawer cart={cart} discount={discount} open={cartOpen} onClose={()=>setCartOpen(false)} onUpdateQty={updateQty} onCheckout={openCheckout} />
        <CheckoutModal open={checkoutOpen} cart={cart} discount={discount} profile={profile} onClose={()=>setCheckoutOpen(false)} onPlaceOrder={placeOrder} />
        <OrdersPanel open={ordersOpen} orders={orders} onClose={()=>setOrdersOpen(false)} onViewReceipt={viewReceipt} />
        <ReceiptModal order={receipt} celebrate={receiptIsNew} onClose={()=>setReceipt(null)} />
        <AccountPanel open={accountOpen} profile={profile} onClose={()=>setAccountOpen(false)} onProfileChange={handleProfileChange} />
        <Navbar cartCount={totalItems} orderCount={orders.length} profile={profile}
          onCartOpen={()=>setCartOpen(true)} onAccountOpen={()=>setAccountOpen(true)}
          onOrdersOpen={()=>setOrdersOpen(true)} />

        <main>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/shop" element={<ShopPage />} />
            <Route path="/club" element={<ClubPage />} />
            <Route path="/wellness" element={<WellnessPage />} />
            <Route path="/community" element={<CommunityPage />} />
            <Route path="/events" element={<EventsPage />} />
            <Route path="/checkout" element={<CheckoutPage />} />
            <Route path="/orders" element={<OrdersRoutePage />} />
            <Route path="*" element={<HomePage />} />
          </Routes>
        </main>
        <Footer />
      </div>
      </StoreCtx.Provider>
    </CartCtx.Provider>
  );
}

