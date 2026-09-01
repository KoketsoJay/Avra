import { useCallback, useEffect, useState } from "react";
import {
  avraApi,
  type CommunityDto,
  type CommunityPayload,
  type EventDto,
  type EventPayload,
  type MembershipPlanDto,
  type ProductDto,
  type ProductPayload,
  type WellnessPlanDto,
  type WellnessPlanPayload,
} from "@/app/api/avraApi";

// ─── Types ────────────────────────────────────────────────────────────────────
export interface Product { id:number; name:string; price:number; category:string; img1:string; img2:string; description:string; tag?:string; }
export interface AvraEvent { id:string; title:string; date:string; time:string; location:string; image:string; price:number|"Free"; category:string; }
export interface CommunityCircle { id:string; name:string; description:string; category:string; memberCount:number; image:string; }
export interface WellnessProgram { id:string; name:string; description:string; category:string; level:string; durationDays:number; price:number; image:string; }
export interface MembershipTier { id:string; name:string; tagline:string; price:number; highlight:boolean; features:string[]; discountPercentage:number; }

/** How the ordering saga's states read to a person. */
export const ORDER_STATUS_COPY: Record<string, string> = {
  Submitted: "Order placed",
  AwaitingValidation: "Confirming stock",
  StockConfirmed: "Awaiting payment",
  Paid: "Paid",
  Shipped: "Shipped",
  Cancelled: "Cancelled",
};

// ─── Catalogue imagery ────────────────────────────────────────────────────────
// The services store an image reference (a bundled asset file name, or an absolute URL). Nothing
// about the catalogue itself lives in the client — only the mapping from reference to bundled URL.
const bundledImages = import.meta.glob<string>("../assets/avra/*.jpg", { eager: true, import: "default" });
const IMAGE_BY_FILENAME: Record<string, string> = Object.fromEntries(
  Object.entries(bundledImages).map(([path, url]) => [path.slice(path.lastIndexOf("/") + 1), url]),
);

/** Default imagery offered when an administrator creates a record without picking a picture. */
export const DEFAULT_PRODUCT_IMAGES = ["avra_30.jpg", "avra_11.jpg"] as const;
export const DEFAULT_EVENT_IMAGE = "avra_51.jpg";
export const DEFAULT_WELLNESS_IMAGE = "avra_8.jpg";
export const DEFAULT_COMMUNITY_IMAGE = "avra_2.jpg";

/** Turns a stored image reference into something an <img> can load. */
export function resolveImage(reference?: string | null) {
  if (!reference) return undefined;
  if (/^(https?:|data:|blob:|\/)/i.test(reference)) return reference;
  return IMAGE_BY_FILENAME[reference.slice(reference.lastIndexOf("/") + 1)];
}

export function toCurrencyPrice(price: number) {
  return Number.isFinite(price) ? price : 0;
}

// ─── DTO mapping ──────────────────────────────────────────────────────────────
export function mapProductDto(product: ProductDto): Product {
  return {
    id: product.id,
    name: product.name,
    price: toCurrencyPrice(product.price),
    category: product.category,
    img1: product.image1 ?? "",
    img2: product.image2 ?? product.image1 ?? "",
    description: product.description,
    tag: product.tag || undefined,
  };
}

export function formatEventDate(startDate: string, endDate: string) {
  const start = new Date(startDate);
  const end = new Date(endDate);
  if (Number.isNaN(start.getTime())) return "Date to be announced";

  const date = start.toLocaleDateString(undefined, { month:"long", day:"numeric", year:"numeric" });
  const endDay = Number.isNaN(end.getTime()) ? "" : end.toLocaleDateString(undefined, { month:"long", day:"numeric" });
  return endDay && end.toDateString() !== start.toDateString() ? `${date} - ${endDay}` : date;
}

export function formatEventTime(startDate: string) {
  const start = new Date(startDate);
  return Number.isNaN(start.getTime()) ? "Time to be announced" : start.toLocaleTimeString(undefined, { hour:"numeric", minute:"2-digit" });
}

export function mapEventDto(event: EventDto): AvraEvent {
  return {
    id: event.id,
    title: event.title,
    date: formatEventDate(event.startDate, event.endDate),
    time: formatEventTime(event.startDate),
    location: event.location,
    image: event.imageUrl ?? "",
    price: event.price === 0 ? "Free" : toCurrencyPrice(event.price),
    category: event.category,
  };
}

export function mapCommunityDto(community: CommunityDto): CommunityCircle {
  return {
    id: community.id,
    name: community.name,
    description: community.description,
    category: community.category,
    memberCount: community.memberCount,
    image: community.imageUrl ?? "",
  };
}

export function mapWellnessPlanDto(plan: WellnessPlanDto): WellnessProgram {
  return {
    id: plan.id,
    name: plan.name,
    description: plan.description,
    category: plan.category,
    level: plan.level,
    durationDays: plan.durationDays,
    price: toCurrencyPrice(plan.price),
    image: plan.imageUrl ?? "",
  };
}

export function mapMembershipPlanDto(plan: MembershipPlanDto): MembershipTier {
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

// ─── Payloads ─────────────────────────────────────────────────────────────────
export function productPayload(product: Product): ProductPayload {
  return {
    name: product.name,
    description: product.description,
    price: product.price,
    category: product.category,
    stock: 25,
    tag: product.tag,
    image1: product.img1,
    image2: product.img2,
  };
}

export function eventPayload(event: AvraEvent): EventPayload {
  const start = new Date(event.date);
  const safeStart = Number.isNaN(start.getTime()) ? new Date() : start;
  const end = new Date(safeStart);
  end.setHours(end.getHours() + 2);

  return {
    title: event.title,
    description: `${event.title} hosted by AVRA.`,
    category: event.category,
    location: event.location,
    startDate: safeStart.toISOString(),
    endDate: end.toISOString(),
    maxAttendees: 40,
    price: event.price === "Free" ? 0 : event.price,
    organizer: "AVRA",
    imageUrl: event.image,
    publishImmediately: true,
  };
}

export function communityPayload(community: CommunityCircle, creatorId: string): CommunityPayload {
  return {
    name: community.name,
    description: community.description,
    category: community.category,
    creatorId,
    imageUrl: community.image,
    isPublic: true,
  };
}

export function wellnessPayload(program: WellnessProgram): WellnessPlanPayload {
  return {
    name: program.name,
    description: program.description,
    category: program.category,
    level: program.level,
    durationDays: program.durationDays,
    imageUrl: program.image,
    price: program.price,
  };
}

export function upsertById<T extends { id:string|number }>(items: T[], item: T) {
  return items.some(existing => String(existing.id) === String(item.id))
    ? items.map(existing => String(existing.id) === String(item.id) ? item : existing)
    : [item, ...items];
}

// ─── Catalogue hook ───────────────────────────────────────────────────────────
/**
 * The database-backed catalogue plus its admin writes. Both applications mount their own copy;
 * the services remain the single source of truth.
 */
export function useCatalogue(notify?: (message: string) => void) {
  const [products, setProducts] = useState<Product[]>([]);
  const [events, setEvents] = useState<AvraEvent[]>([]);
  const [programs, setPrograms] = useState<WellnessProgram[]>([]);
  const [communities, setCommunities] = useState<CommunityCircle[]>([]);

  const warn = useCallback((message: string, error: unknown) => {
    console.warn(message, error);
    notify?.(message);
  }, [notify]);

  useEffect(() => {
    let cancelled = false;
    avraApi.products.list()
      .then(items => { if (!cancelled) setProducts(items.map(mapProductDto)); })
      .catch(error => console.warn("ProductService unavailable; no catalogue to show", error));
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    avraApi.events.list()
      .then(items => { if (!cancelled) setEvents(items.map(mapEventDto)); })
      .catch(error => console.warn("EventsService unavailable; no events to show", error));
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    avraApi.wellness.list()
      .then(items => { if (!cancelled) setPrograms(items.map(mapWellnessPlanDto)); })
      .catch(error => console.warn("WellnessService unavailable; no wellness plans to show", error));
    return () => { cancelled = true; };
  }, []);

  useEffect(() => {
    let cancelled = false;
    avraApi.communities.list()
      .then(items => { if (!cancelled) setCommunities(items.map(mapCommunityDto)); })
      .catch(error => console.warn("CommunityService unavailable; no community circles to show", error));
    return () => { cancelled = true; };
  }, []);

  const saveProduct = async (item: Product) => {
    const exists = products.some(product => product.id === item.id);
    setProducts(upsertById(products, item));
    try {
      const remote = exists ? await avraApi.products.update(item.id, productPayload(item)) : await avraApi.products.create(productPayload(item));
      setProducts(current => upsertById(current, mapProductDto(remote)));
    } catch (error) {
      warn("Product service is offline — the change was not saved", error);
    }
  };

  const deleteProduct = async (id: number) => {
    try {
      await avraApi.products.delete(id);
      setProducts(current => current.filter(product => product.id !== id));
    } catch (error) {
      warn("Product service is offline — the item was not removed", error);
    }
  };

  const saveEvent = async (item: AvraEvent) => {
    const exists = events.some(event => event.id === item.id);
    setEvents(upsertById(events, item));
    try {
      const remote = exists ? await avraApi.events.update(item.id, eventPayload(item)) : await avraApi.events.create(eventPayload(item));
      setEvents(current => upsertById(current, mapEventDto(remote)));
    } catch (error) {
      warn("Events service is offline — the change was not saved", error);
    }
  };

  const deleteEvent = async (id: string) => {
    try {
      await avraApi.events.delete(id);
      setEvents(current => current.filter(event => event.id !== id));
    } catch (error) {
      warn("Events service is offline — the item was not removed", error);
    }
  };

  const saveProgram = async (item: WellnessProgram) => {
    const exists = programs.some(program => program.id === item.id);
    setPrograms(upsertById(programs, item));
    try {
      const remote = exists ? await avraApi.wellness.update(item.id, wellnessPayload(item)) : await avraApi.wellness.create(wellnessPayload(item));
      setPrograms(current => upsertById(current, mapWellnessPlanDto(remote)));
    } catch (error) {
      warn("Wellness service is offline — the change was not saved", error);
    }
  };

  const deleteProgram = async (id: string) => {
    try {
      await avraApi.wellness.delete(id);
      setPrograms(current => current.filter(program => program.id !== id));
    } catch (error) {
      warn("Wellness service is offline — the item was not removed", error);
    }
  };

  const saveCommunity = async (item: CommunityCircle, creatorId: string) => {
    const exists = communities.some(community => community.id === item.id);
    setCommunities(upsertById(communities, item));
    try {
      const payload = communityPayload(item, creatorId);
      const remote = exists ? await avraApi.communities.update(item.id, payload) : await avraApi.communities.create(payload);
      setCommunities(current => upsertById(current, mapCommunityDto(remote)));
    } catch (error) {
      warn("Community service is offline — the change was not saved", error);
    }
  };

  const deleteCommunity = async (id: string) => {
    try {
      await avraApi.communities.delete(id);
      setCommunities(current => current.filter(community => community.id !== id));
    } catch (error) {
      warn("Community service is offline — the item was not removed", error);
    }
  };

  return {
    products, events, programs, communities,
    saveProduct, deleteProduct, saveEvent, deleteEvent,
    saveProgram, deleteProgram, saveCommunity, deleteCommunity,
  };
}
