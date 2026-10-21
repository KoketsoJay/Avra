type ServiceName = "products" | "cart" | "users" | "events" | "community" | "wellness" | "ordering";

const API_BASE_URLS: Record<ServiceName, string> = {
  products: import.meta.env.VITE_PRODUCT_API_URL ?? "http://localhost:5001",
  cart: import.meta.env.VITE_CART_API_URL ?? "http://localhost:5002",
  users: import.meta.env.VITE_USER_API_URL ?? "http://localhost:5003",
  events: import.meta.env.VITE_EVENTS_API_URL ?? "http://localhost:5004",
  community: import.meta.env.VITE_COMMUNITY_API_URL ?? "http://localhost:5005",
  wellness: import.meta.env.VITE_WELLNESS_API_URL ?? "http://localhost:5006",
  ordering: import.meta.env.VITE_ORDERING_API_URL ?? "http://localhost:5007",
};

const DEFAULT_USER = {
  userName: "AVRA Guest",
  userEmail: "guest@avra.local",
};

export interface ProductDto {
  id: number;
  name: string;
  description: string;
  details?: string | null;
  price: number;
  category: string;
  stock: number;
  tag?: string | null;
  image1?: string | null;
  image2?: string | null;
  imageUrls?: string[] | null;
  isActive: boolean;
}

export interface ProductPayload {
  name: string;
  description: string;
  details?: string;
  price: number;
  category: string;
  stock: number;
  tag?: string;
  image1?: string;
  image2?: string;
  imageUrls?: string[];
}

export interface CartDto {
  id: number;
  userId: string;
  items: CartItemDto[];
  total: number;
}

export interface CartItemDto {
  productId: number;
  productName: string;
  price: number;
  quantity: number;
  image?: string | null;
  subtotal: number;
}

export interface OrderItemDto {
  productId: number;
  productName: string;
  price: number;
  quantity: number;
}

/** Matches the CartService OrderStatus enum ordering. */
export const ORDER_STATUS_LABELS = ["Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Refunded"] as const;

export interface OrderDto {
  id: number;
  userId: string;
  items: OrderItemDto[];
  totalAmount: number;
  status: number | string;
  shippingAddress?: string | null;
  billingAddress?: string | null;
  createdAt: string;
}

export interface CreateOrderPayload {
  userId: string;
  shippingAddress?: string;
  billingAddress?: string;
}

/** Shipping and payment details posted to the basket checkout endpoint. */
export interface BasketCheckoutPayload {
  userName: string;
  city: string;
  street: string;
  state: string;
  country: string;
  zipCode: string;
  /** Card, BankTransfer or CashOnDelivery. Card fields are only required for Card. */
  paymentMethod: "Card" | "BankTransfer" | "CashOnDelivery";
  cardNumber: string;
  cardHolderName: string;
  cardExpiration: string;
  cardSecurityNumber: string;
  cardTypeId: number;
  buyer?: string;
}

/** Order lifecycle as reported by the ordering service. */
export interface OrderSummaryViewModel {
  orderNumber: number;
  date: string;
  status: string;
  total: number;
}

export interface OrderItemViewModel {
  productId: number;
  productName: string;
  units: number;
  unitPrice: number;
  pictureUrl?: string | null;
}

export interface OrderViewModel {
  orderNumber: number;
  date: string;
  status: string;
  description?: string | null;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  total: number;
  carrier?: string | null;
  trackingNumber?: string | null;
  shippedAt?: string | null;
  orderItems: OrderItemViewModel[];
}

/** A row in the administrator's fulfilment queue. */
export interface AdminOrderSummaryViewModel {
  orderNumber: number;
  date: string;
  status: string;
  total: number;
  itemCount: number;
  buyerName: string;
  city: string;
  country: string;
  carrier?: string | null;
  trackingNumber?: string | null;
  shippedAt?: string | null;
  /** True once the order is paid and still waiting to leave the warehouse. */
  awaitingDispatch: boolean;
}

/** Dispatch details the warehouse records when handing a parcel to the carrier. */
export interface ShipOrderPayload {
  carrier: string;
  trackingNumber: string;
}

export interface CardTypeViewModel {
  id: number;
  name: string;
}

export interface EventDto {
  id: string;
  title: string;
  description: string;
  category: string;
  location: string;
  startDate: string;
  endDate: string;
  maxAttendees: number;
  currentAttendees: number;
  price: number;
  organizer: string;
  imageUrl?: string | null;
  status: number | string;
  isFeatured: boolean;
  isMembersOnly: boolean;
}

export interface EventPayload {
  title: string;
  description: string;
  category: string;
  location: string;
  startDate: string;
  endDate: string;
  maxAttendees: number;
  price: number;
  organizer: string;
  imageUrl?: string;
  isFeatured?: boolean;
  isMembersOnly?: boolean;
  /** When false the event is stored as a draft and stays off the storefront. */
  publishImmediately?: boolean;
}

export interface EventTicketDto {
  id: string;
  registrationId: string;
  eventId: string;
  userId: string;
  ticketNumber: string;
  qrCodePayload: string;
  qrCodePngBase64: string;
  issuedAt: string;
}

export interface EventTicketPurchasePayload {
  numberOfTickets: number;
  paymentMethod: string;
  cardLast4: string;
}

export interface EventTicketPurchaseDto {
  eventId: string;
  eventTitle: string;
  userId: string;
  numberOfTickets: number;
  amountPaid: number;
  paymentMethod: string;
  paymentStatus: string;
  registration: EventRegistrationDto;
  tickets: EventTicketDto[];
}

export interface UserEventTicketDto {
  id: string;
  registrationId: string;
  eventId: string;
  eventTitle: string;
  eventLocation: string;
  eventStartDate: string;
  eventEndDate: string;
  ticketNumber: string;
  qrCodePayload: string;
  qrCodePngBase64: string;
  issuedAt: string;
  isCancelled: boolean;
}

export interface ValidateEventTicketPayload {
  ticketNumber: string;
  eventId?: string;
}

export interface EventTicketValidationResultDto {
  isValid: boolean;
  status: string;
  message: string;
  ticket?: UserEventTicketDto | null;
}

/** Publication state of a promotion, matching the EventsService PromotionStatus enum. */
export const PROMOTION_STATUS_LABELS = ["Draft", "Published", "Archived"] as const;

export interface PromotionDto {
  id: string;
  title: string;
  description: string;
  category: string;
  promoCode?: string | null;
  discountPercentage: number;
  startDate: string;
  endDate: string;
  imageUrl?: string | null;
  isFeatured: boolean;
  status: number | string;
  publishedAt?: string | null;
  isLive: boolean;
}

export interface PromotionPayload {
  title: string;
  description: string;
  category: string;
  promoCode?: string;
  discountPercentage: number;
  startDate: string;
  endDate: string;
  imageUrl?: string;
  isFeatured: boolean;
}

/** Warehouse view of a catalogue item, including de-listed products. */
export interface InventoryItemDto {
  id: number;
  name: string;
  category: string;
  price: number;
  stock: number;
  restockThreshold: number;
  maxStockThreshold: number;
  onReorder: boolean;
  isActive: boolean;
  image1?: string | null;
  updatedAt?: string | null;
}

export interface StockAdjustmentPayload {
  /** Positive receives stock, negative writes it off. */
  quantity: number;
  reason: string;
}

export interface StockThresholdsPayload {
  restockThreshold: number;
  maxStockThreshold: number;
}

export interface CommunityDto {
  id: string;
  name: string;
  description: string;
  category: string;
  creatorId: string;
  memberCount: number;
  imageUrl?: string | null;
  isPublic: boolean;
  status: string;
  createdAt: string;
}

export interface CommunityPayload {
  name: string;
  description: string;
  category: string;
  creatorId: string;
  imageUrl?: string;
  isPublic: boolean;
}

export interface CommunityMemberDto {
  id: string;
  communityId: string;
  userId: string;
  userName: string;
  userAvatar?: string | null;
  role: number | string;
  joinedAt: string;
  isActive: boolean;
}

export interface CommunityMembershipDto {
  communityId: string;
  communityName: string;
  communityCategory: string;
  communityImageUrl?: string | null;
  role: number | string;
  joinedAt: string;
}

export interface CreateCommunityPostPayload {
  communityId: string;
  title: string;
  content: string;
}

export interface UpdateCommunityPostPayload {
  title: string;
  content: string;
}

export interface CommunityPostDto {
  id: string;
  communityId: string;
  userId: string;
  userName: string;
  title: string;
  content: string;
  likeCount: number;
  commentCount: number;
  isPinned: boolean;
  isHidden: boolean;
  hiddenReason?: string | null;
  likedByCurrentUser: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateCommunityCommentPayload {
  postId: string;
  content: string;
}

export interface UpdateCommunityCommentPayload {
  content: string;
}

export interface CommunityCommentDto {
  id: string;
  postId: string;
  communityId: string;
  userId: string;
  userName: string;
  userAvatar?: string | null;
  content: string;
  isHidden: boolean;
  hiddenReason?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateCommunityReportPayload {
  communityId: string;
  postId?: string;
  commentId?: string;
  reason: string;
  details?: string;
}

export interface CommunityReportDto {
  id: string;
  communityId: string;
  postId?: string | null;
  commentId?: string | null;
  reportedByUserId: string;
  reportedByUserName: string;
  reason: string;
  details?: string | null;
  status: number | string;
  resolutionNotes?: string | null;
  reviewedByUserId?: string | null;
  reviewedAt?: string | null;
  createdAt: string;
}

export interface ResolveCommunityReportPayload {
  status: number | string;
  resolutionNotes?: string;
}

export interface UpdateCommunityStatusPayload {
  status: string;
}

export interface SetCommunityMemberRolePayload {
  role: number | string;
}

export interface SetCommunityContentVisibilityPayload {
  hidden: boolean;
  reason?: string;
}

export interface WellnessPlanDto {
  id: string;
  name: string;
  description: string;
  category: string;
  level: string;
  durationDays: number;
  imageUrl?: string | null;
  price: number;
  isActive: boolean;
}

export interface WellnessPlanPayload {
  name: string;
  description: string;
  category: string;
  level: string;
  durationDays: number;
  imageUrl?: string;
  price: number;
}

export interface AuthResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: UserProfileDto;
}

/** A membership tier offered on the storefront, served from the user database. */
export interface MembershipPlanDto {
  id: string;
  name: string;
  tagline: string;
  monthlyPrice: number;
  discountPercentage: number;
  isHighlighted: boolean;
  features: string[];
}

/** Where a shopper's discount came from, matching the UserService DiscountSource enum. */
export const DISCOUNT_SOURCE_LABELS = ["None", "Tier", "Override"] as const;

/**
 * The discount that applies to the signed-in shopper. The storefront uses this for display only;
 * the checkout re-resolves it server-side, so it can never be used to underpay.
 */
export interface EffectiveDiscountDto {
  userId: string;
  percentage: number;
  source: number | string;
  label: string;
  expiresAt?: string | null;
}

/** An administrator's view of one member's discount override. */
export interface MemberDiscountDto {
  userId: string;
  email: string;
  fullName: string;
  planName?: string | null;
  percentage: number;
  reason: string;
  expiresAt?: string | null;
  isExpired: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface GrantMemberDiscountPayload {
  percentage: number;
  reason: string;
  expiresAt?: string | null;
}

/** Matches the UserService MembershipStatus enum ordering. */
export const MEMBERSHIP_STATUS_LABELS = ["Active", "Inactive", "Paused", "Cancelled", "Expired", "Trialing", "Lapsed"] as const;

export interface UserMembershipDto {
  id: number;
  userId: string;
  planName: string;
  status: number | string;
  startDate: string;
  endDate?: string | null;
  autoRenew: boolean;
  trialEndsAt?: string | null;
  nextBillingDate?: string | null;
  monthlyPrice: number;
  isInTrial: boolean;
}

export interface UserProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role?: string | null;
  phoneNumber?: string | null;
  avatar?: string | null;
  isActive?: boolean;
  createdAt?: string;
}

/** Role name the backend issues for administrators. */
export const ADMINISTRATOR_ROLE = "administrator";

function getStoredToken() {
  return localStorage.getItem("avra_access_token");
}

function storeAuth(auth: AuthResponse) {
  localStorage.setItem("avra_access_token", auth.accessToken);
  storeProfile(auth.user);
}

function storeProfile(user: UserProfileDto) {
  localStorage.setItem("avra_user_id", user.id);
  localStorage.setItem("avra_user_name", `${user.firstName} ${user.lastName}`.trim());
  localStorage.setItem("avra_user_email", user.email);
  localStorage.setItem("avra_first_name", user.firstName);
  localStorage.setItem("avra_last_name", user.lastName);
  if (user.role) {
    localStorage.setItem("avra_user_role", user.role);
  } else {
    localStorage.removeItem("avra_user_role");
  }
  if (user.phoneNumber) {
    localStorage.setItem("avra_phone_number", user.phoneNumber);
  } else {
    localStorage.removeItem("avra_phone_number");
  }
}

export function saveLocalProfile(user: UserProfileDto) {
  storeProfile(user);
}

export function isApiUnavailable(error: unknown) {
  return error instanceof TypeError || String(error).includes("Failed to fetch");
}

/** Checkout and order history are authenticated; the buyer is taken from the token, not the body. */
export function isAuthenticated() {
  return Boolean(getStoredToken());
}

/**
 * Whether the signed-in account holds the administrator role. This only decides what the UI
 * offers — every administrator endpoint is enforced server-side by the AdministratorOnly policy.
 */
export function isAdministrator(profile?: UserProfileDto | null) {
  const role = profile?.role ?? localStorage.getItem("avra_user_role");
  return isAuthenticated() && role?.toLowerCase() === ADMINISTRATOR_ROLE;
}

function clearAuth() {
  localStorage.removeItem("avra_access_token");
  localStorage.removeItem("avra_user_role");
  localStorage.removeItem("avra_first_name");
  localStorage.removeItem("avra_last_name");
  localStorage.removeItem("avra_phone_number");
}

async function request<T>(service: ServiceName, path: string, init: RequestInit = {}) {
  const token = getStoredToken();
  const headers = new Headers(init.headers);

  if (!headers.has("Content-Type") && init.body) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URLS[service]}${path}`, {
    ...init,
    headers,
  });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

/** The stored reference to persist plus a short-lived signed URL to preview the upload. */
export interface ImageUploadResult {
  reference: string;
  url?: string | null;
}

/** Uploads an image file (multipart) to a service's blob-backed image endpoint. Admin only. */
async function uploadImage(service: ServiceName, path: string, file: File): Promise<ImageUploadResult> {
  const token = getStoredToken();
  const headers = new Headers();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const body = new FormData();
  body.append("file", file);

  const response = await fetch(`${API_BASE_URLS[service]}${path}`, { method: "POST", headers, body });

  if (!response.ok) {
    const message = await response.text();
    throw new Error(message || `${response.status} ${response.statusText}`);
  }

  return response.json() as Promise<ImageUploadResult>;
}

export function getAvraUser() {
  let userId = localStorage.getItem("avra_user_id");

  if (!userId) {
    userId = crypto.randomUUID?.() ?? `guest-${Date.now()}`;
    localStorage.setItem("avra_user_id", userId);
  }

  return {
    userId,
    userName: localStorage.getItem("avra_user_name") ?? DEFAULT_USER.userName,
    userEmail: localStorage.getItem("avra_user_email") ?? DEFAULT_USER.userEmail,
  };
}

export function getStoredProfile(): UserProfileDto | null {
  const userId = localStorage.getItem("avra_user_id");
  const email = localStorage.getItem("avra_user_email");
  const firstName = localStorage.getItem("avra_first_name");
  const lastName = localStorage.getItem("avra_last_name");

  if (!userId || !email || !firstName || !lastName) {
    return null;
  }

  return {
    id: userId,
    email,
    firstName,
    lastName,
    role: localStorage.getItem("avra_user_role"),
    phoneNumber: localStorage.getItem("avra_phone_number"),
  };
}

export const avraApi = {
  products: {
    list: () => request<ProductDto[]>("products", "/api/products"),
    search: (term: string) => request<ProductDto[]>("products", `/api/products/search/${encodeURIComponent(term)}`),
    byCategory: (category: string) => request<ProductDto[]>("products", `/api/products/category/${encodeURIComponent(category)}`),
    create: (payload: ProductPayload) => request<ProductDto>("products", "/api/products", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    update: (productId: number, payload: ProductPayload) => request<ProductDto>("products", `/api/products/${productId}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    delete: (productId: number) => request<void>("products", `/api/products/${productId}`, {
      method: "DELETE",
    }),
    uploadImage: (file: File) => uploadImage("products", "/api/products/images", file),
  },
  /** Warehouse operations. Every call requires the administrator role. */
  inventory: {
    list: () => request<InventoryItemDto[]>("products", "/api/inventory"),
    lowStock: () => request<InventoryItemDto[]>("products", "/api/inventory/low-stock"),
    adjust: (productId: number, payload: StockAdjustmentPayload) =>
      request<InventoryItemDto>("products", `/api/inventory/${productId}/adjust`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    setThresholds: (productId: number, payload: StockThresholdsPayload) =>
      request<InventoryItemDto>("products", `/api/inventory/${productId}/thresholds`, {
        method: "PUT",
        body: JSON.stringify(payload),
      }),
  },
  cart: {
    get: (userId: string) => request<CartDto>("cart", `/api/carts/${encodeURIComponent(userId)}`),
    add: (userId: string, item: Omit<CartItemDto, "subtotal">) => request<CartDto>("cart", `/api/carts/${encodeURIComponent(userId)}/items`, {
      method: "POST",
      body: JSON.stringify(item),
    }),
    updateQuantity: (userId: string, productId: number, quantity: number) => request<CartDto>("cart", `/api/carts/${encodeURIComponent(userId)}/items/${productId}?quantity=${quantity}`, {
      method: "PUT",
    }),
    remove: (userId: string, productId: number) => request<CartDto>("cart", `/api/carts/${encodeURIComponent(userId)}/items/${productId}`, {
      method: "DELETE",
    }),
    clear: (userId: string) => request<void>("cart", `/api/carts/${encodeURIComponent(userId)}`, {
      method: "DELETE",
    }),
  },
  orders: {
    create: (payload: CreateOrderPayload) => request<OrderDto>("cart", "/api/orders", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    byUser: (userId: string) => request<OrderDto[]>("cart", `/api/orders/user/${encodeURIComponent(userId)}`),
    byId: (orderId: number) => request<OrderDto>("cart", `/api/orders/${orderId}`),
  },
  /**
   * eShop-style checkout. The basket service only accepts the checkout; the ordering service
   * creates the order asynchronously, so this resolves before the order exists.
   */
  checkout: {
    submit: (payload: BasketCheckoutPayload, requestId: string = crypto.randomUUID()) =>
      request<void>("cart", "/api/basket/checkout", {
        method: "POST",
        headers: { "x-requestid": requestId },
        body: JSON.stringify(payload),
      }),
  },
  ordering: {
    list: () => request<OrderSummaryViewModel[]>("ordering", "/api/orders"),
    byId: (orderNumber: number) => request<OrderViewModel>("ordering", `/api/orders/${orderNumber}`),
    cardTypes: () => request<CardTypeViewModel[]>("ordering", "/api/orders/cardtypes"),
    /** The fulfilment queue across every buyer. Administrators only. */
    allOrders: (status?: string) =>
      request<AdminOrderSummaryViewModel[]>(
        "ordering",
        status ? `/api/orders/admin?status=${encodeURIComponent(status)}` : "/api/orders/admin",
      ),
    cancel: (orderNumber: number, requestId: string = crypto.randomUUID()) =>
      request<void>("ordering", "/api/orders/cancel", {
        method: "PUT",
        headers: { "x-requestid": requestId },
        body: JSON.stringify({ orderNumber }),
      }),
    /** Records dispatch. Administrators only; carrier and tracking are both required. */
    ship: (orderNumber: number, dispatch: ShipOrderPayload, requestId: string = crypto.randomUUID()) =>
      request<void>("ordering", "/api/orders/ship", {
        method: "PUT",
        headers: { "x-requestid": requestId },
        body: JSON.stringify({ orderNumber, ...dispatch }),
      }),
  },
  auth: {
    register: async (payload: { email: string; password: string; firstName: string; lastName: string; phoneNumber?: string }) => {
      const auth = await request<AuthResponse>("users", "/api/auth/register", {
        method: "POST",
        body: JSON.stringify(payload),
      });
      storeAuth(auth);
      return auth;
    },
    login: async (payload: { email: string; password: string }) => {
      const auth = await request<AuthResponse>("users", "/api/auth/login", {
        method: "POST",
        body: JSON.stringify(payload),
      });
      storeAuth(auth);
      return auth;
    },
    logout: clearAuth,
  },
  users: {
    getProfile: async (userId: string) => {
      const profile = await request<UserProfileDto>("users", `/api/users/${encodeURIComponent(userId)}`);
      storeProfile(profile);
      return profile;
    },
    updateProfile: async (userId: string, payload: { firstName: string; lastName: string; phoneNumber?: string; avatar?: string }) => {
      const profile = await request<UserProfileDto>("users", `/api/users/${encodeURIComponent(userId)}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      });
      storeProfile(profile);
      return profile;
    },
    /** Member lookup for the discounts panel. Administrators only. */
    search: (term: string) =>
      request<UserProfileDto[]>("users", `/api/users/search?term=${encodeURIComponent(term)}`),
    /** Admin member search; includeInactive surfaces deactivated accounts for management. */
    searchAdmin: (term: string, includeInactive = false) =>
      request<UserProfileDto[]>(
        "users",
        `/api/users/search?term=${encodeURIComponent(term)}&includeInactive=${includeInactive}`,
      ),
    /** Admin edit of another member's profile. Does not touch the local session. */
    adminUpdateProfile: (userId: string, payload: { firstName: string; lastName: string; phoneNumber?: string; avatar?: string }) =>
      request<UserProfileDto>("users", `/api/users/${encodeURIComponent(userId)}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      }),
    /** Promote or demote a member. Administrators only. */
    setRole: (userId: string, role: "administrator" | "customer") =>
      request<UserProfileDto>("users", `/api/users/${encodeURIComponent(userId)}/role`, {
        method: "PUT",
        body: JSON.stringify({ role }),
      }),
    /** Activate or deactivate a member. Administrators only. */
    setActive: (userId: string, isActive: boolean) =>
      request<UserProfileDto>("users", `/api/users/${encodeURIComponent(userId)}/status`, {
        method: "PUT",
        body: JSON.stringify({ isActive }),
      }),
    /** Soft-deletes (deactivates) a member. Administrators only. */
    remove: (userId: string) =>
      request<void>("users", `/api/users/${encodeURIComponent(userId)}`, { method: "DELETE" }),
  },
  memberships: {
    plans: () => request<MembershipPlanDto[]>("users", "/api/memberships/plans"),
    /** The signed-in shopper's current tier, or null when they hold none. */
    mine: async (userId: string) => {
      try {
        return await request<UserMembershipDto>("users", `/api/memberships/user/${encodeURIComponent(userId)}`);
      } catch {
        return null;
      }
    },
    /** Enrols the signed-in shopper on a tier. The service only permits acting on your own account. */
    join: (userId: string, planName: string) =>
      request<UserMembershipDto>("users", `/api/memberships/user/${encodeURIComponent(userId)}?planName=${encodeURIComponent(planName)}`, {
        method: "POST",
      }),
    /** The signed-in shopper's own rate. Requires a session. */
    myDiscount: () => request<EffectiveDiscountDto>("users", "/api/memberships/discount/me"),
    /** Sets the rate every member on a tier receives. Administrators only. */
    setPlanDiscount: (planId: string, discountPercentage: number) =>
      request<MembershipPlanDto>("users", `/api/memberships/plans/${encodeURIComponent(planId)}/discount`, {
        method: "PUT",
        body: JSON.stringify({ discountPercentage }),
      }),
    /** Every member holding an override. Administrators only. */
    discounts: () => request<MemberDiscountDto[]>("users", "/api/memberships/discounts"),
    grantDiscount: (userId: string, payload: GrantMemberDiscountPayload) =>
      request<MemberDiscountDto>("users", `/api/memberships/discounts/${encodeURIComponent(userId)}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      }),
    revokeDiscount: (userId: string) =>
      request<void>("users", `/api/memberships/discounts/${encodeURIComponent(userId)}`, { method: "DELETE" }),
  },
  events: {
    list: () => request<EventDto[]>("events", "/api/events"),
    /** Every event including drafts. Administrators only. */
    adminList: () => request<EventDto[]>("events", "/api/events/admin"),
    create: (payload: EventPayload) => request<EventDto>("events", "/api/events", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    update: (eventId: string, payload: EventPayload) => request<EventDto>("events", `/api/events/${encodeURIComponent(eventId)}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    uploadImage: (file: File) => uploadImage("events", "/api/events/images", file),
    publish: (eventId: string) => request<EventDto>("events", `/api/events/${encodeURIComponent(eventId)}/publish`, {
      method: "PUT",
    }),
    unpublish: (eventId: string) => request<EventDto>("events", `/api/events/${encodeURIComponent(eventId)}/unpublish`, {
      method: "PUT",
    }),
    delete: (eventId: string) => request<void>("events", `/api/events/${encodeURIComponent(eventId)}`, {
      method: "DELETE",
    }),
    register: (eventId: string, attendeeCount = 1, isMember = false) => {
      const user = getAvraUser();
      return request<EventRegistrationDto>("events", `/api/registrations/events/${encodeURIComponent(eventId)}/register`, {
        method: "POST",
        body: JSON.stringify({
          userId: user.userId,
          userName: user.userName,
          userEmail: user.userEmail,
          numberOfAttendees: attendeeCount,
          isMember,
        }),
      });
    },
    purchase: (eventId: string, payload: EventTicketPurchasePayload) => {
      const user = getAvraUser();
      return request<EventTicketPurchaseDto>("events", `/api/registrations/events/${encodeURIComponent(eventId)}/purchase`, {
        method: "POST",
        body: JSON.stringify({
          userId: user.userId,
          userName: user.userName,
          userEmail: user.userEmail,
          numberOfTickets: payload.numberOfTickets,
          paymentMethod: payload.paymentMethod,
          cardLast4: payload.cardLast4,
          isMember: false,
        }),
      });
    },
    ticketsByUser: (userId?: string) => {
      const resolvedUserId = userId ?? getAvraUser().userId;
      return request<UserEventTicketDto[]>("events", `/api/registrations/tickets/user/${encodeURIComponent(resolvedUserId)}`);
    },
    validateTicket: (payload: ValidateEventTicketPayload) =>
      request<EventTicketValidationResultDto>("events", "/api/registrations/tickets/validate", {
        method: "POST",
        body: JSON.stringify(payload),
      }),
  },
  /** Marketing promotions. Reads are public; drafting and publishing require the admin role. */
  promotions: {
    live: () => request<PromotionDto[]>("events", "/api/promotions"),
    adminList: () => request<PromotionDto[]>("events", "/api/promotions/admin"),
    create: (payload: PromotionPayload) => request<PromotionDto>("events", "/api/promotions", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    update: (promotionId: string, payload: PromotionPayload) =>
      request<PromotionDto>("events", `/api/promotions/${encodeURIComponent(promotionId)}`, {
        method: "PUT",
        body: JSON.stringify(payload),
      }),
    publish: (promotionId: string) =>
      request<PromotionDto>("events", `/api/promotions/${encodeURIComponent(promotionId)}/publish`, { method: "PUT" }),
    unpublish: (promotionId: string) =>
      request<PromotionDto>("events", `/api/promotions/${encodeURIComponent(promotionId)}/unpublish`, { method: "PUT" }),
    archive: (promotionId: string) =>
      request<PromotionDto>("events", `/api/promotions/${encodeURIComponent(promotionId)}/archive`, { method: "PUT" }),
    delete: (promotionId: string) =>
      request<void>("events", `/api/promotions/${encodeURIComponent(promotionId)}`, { method: "DELETE" }),
  },
  communities: {
    list: () => request<CommunityDto[]>("community", "/api/communities"),
    adminList: () => request<CommunityDto[]>("community", "/api/communities/admin"),
    byId: (communityId: string) => request<CommunityDto>("community", `/api/communities/${encodeURIComponent(communityId)}`),
    byCategory: (category: string) => request<CommunityDto[]>("community", `/api/communities/search/category?category=${encodeURIComponent(category)}`),
    create: (payload: CommunityPayload) => request<CommunityDto>("community", "/api/communities", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    update: (communityId: string, payload: CommunityPayload) => request<CommunityDto>("community", `/api/communities/${encodeURIComponent(communityId)}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    delete: (communityId: string) => request<void>("community", `/api/communities/${encodeURIComponent(communityId)}`, {
      method: "DELETE",
    }),
    setStatus: (communityId: string, payload: UpdateCommunityStatusPayload) => request<CommunityDto>("community", `/api/communities/${encodeURIComponent(communityId)}/status`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    members: (communityId: string) => request<CommunityMemberDto[]>("community", `/api/members/community/${encodeURIComponent(communityId)}`),
    myMemberships: () => request<CommunityMembershipDto[]>("community", "/api/members/mine"),
    join: (communityId: string) => {
      const user = getAvraUser();
      const params = new URLSearchParams({ userId: user.userId, userName: user.userName });
      return request<CommunityMemberDto>("community", `/api/members/community/${encodeURIComponent(communityId)}?${params.toString()}`, {
        method: "POST",
      });
    },
    leave: (communityId: string) => request<void>("community", `/api/members/community/${encodeURIComponent(communityId)}`, {
      method: "DELETE",
    }),
    setMemberRole: (communityId: string, userId: string, payload: SetCommunityMemberRolePayload) => request<CommunityMemberDto>(
      "community",
      `/api/members/community/${encodeURIComponent(communityId)}/role/${encodeURIComponent(userId)}`,
      {
        method: "PUT",
        body: JSON.stringify(payload),
      },
    ),
    posts: (communityId: string, includeHidden = false) => request<CommunityPostDto[]>(
      "community",
      includeHidden
        ? `/api/posts/community/${encodeURIComponent(communityId)}?includeHidden=true`
        : `/api/posts/community/${encodeURIComponent(communityId)}`,
    ),
    createPost: (payload: CreateCommunityPostPayload) => request<CommunityPostDto>("community", "/api/posts", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    updatePost: (postId: string, payload: UpdateCommunityPostPayload) => request<CommunityPostDto>("community", `/api/posts/${encodeURIComponent(postId)}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    deletePost: (postId: string) => request<void>("community", `/api/posts/${encodeURIComponent(postId)}`, {
      method: "DELETE",
    }),
    toggleLike: (postId: string) => request<CommunityPostDto>("community", `/api/posts/${encodeURIComponent(postId)}/like`, {
      method: "POST",
    }),
    comments: (postId: string, includeHidden = false) => request<CommunityCommentDto[]>(
      "community",
      includeHidden
        ? `/api/posts/comments/post/${encodeURIComponent(postId)}?includeHidden=true`
        : `/api/posts/comments/post/${encodeURIComponent(postId)}`,
    ),
    createComment: (payload: CreateCommunityCommentPayload) => request<CommunityCommentDto>("community", "/api/posts/comments", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    updateComment: (commentId: string, payload: UpdateCommunityCommentPayload) => request<CommunityCommentDto>("community", `/api/posts/comments/${encodeURIComponent(commentId)}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    deleteComment: (commentId: string) => request<void>("community", `/api/posts/comments/${encodeURIComponent(commentId)}`, {
      method: "DELETE",
    }),
    setPostVisibility: (postId: string, payload: SetCommunityContentVisibilityPayload) => request<CommunityPostDto>("community", `/api/posts/${encodeURIComponent(postId)}/visibility`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    setCommentVisibility: (commentId: string, payload: SetCommunityContentVisibilityPayload) => request<CommunityCommentDto>("community", `/api/posts/comments/${encodeURIComponent(commentId)}/visibility`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    report: (payload: CreateCommunityReportPayload) => request<CommunityReportDto>("community", "/api/reports", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    reports: (status?: string) => request<CommunityReportDto[]>(
      "community",
      status ? `/api/reports?status=${encodeURIComponent(status)}` : "/api/reports",
    ),
    resolveReport: (reportId: string, payload: ResolveCommunityReportPayload) => request<CommunityReportDto>("community", `/api/reports/${encodeURIComponent(reportId)}/resolve`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  },
  wellness: {
    list: () => request<WellnessPlanDto[]>("wellness", "/api/plans"),
    create: (payload: WellnessPlanPayload) => request<WellnessPlanDto>("wellness", "/api/plans", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
    update: (planId: string, payload: WellnessPlanPayload) => request<WellnessPlanDto>("wellness", `/api/plans/${encodeURIComponent(planId)}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
    delete: (planId: string) => request<void>("wellness", `/api/plans/${encodeURIComponent(planId)}`, {
      method: "DELETE",
    }),
    enroll: (planId: string) => {
      const user = getAvraUser();
      return request("wellness", "/api/progress/enroll", {
        method: "POST",
        body: JSON.stringify({ userId: user.userId, planId }),
      });
    },
  },
};
