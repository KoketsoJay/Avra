import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { CalendarClock, Crown, Package, Sparkles, UserRound } from "lucide-react";
import { useStore } from "@/app/state/StoreContext";
import { avraApi, getStoredProfile, type OrderSummaryViewModel } from "@/app/api/avraApi";
import { Bracket, G, GoldBtn, Shine, bg, bodoni, fg, gold, goldGrad, money } from "@/app/ui/primitives";

function formatDate(value?: string | null) {
  if (!value) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toLocaleDateString();
}

function StatCard({ icon:Icon, label, value, hint }: { icon:typeof Crown; label:string; value:string; hint?:string }) {
  return (
    <div className="relative rounded-3xl p-5 overflow-hidden" style={{ ...G.thick }}>
      <Shine />
      <div className="w-10 h-10 rounded-xl flex items-center justify-center mb-4"
        style={{ background:"rgba(191,155,94,0.16)", border:"1px solid rgba(191,155,94,0.25)" }}>
        <Icon size={16} style={{ color:gold }} />
      </div>
      <p className="text-2xl mb-1" style={{ fontFamily:bodoni, color:fg }}>{value}</p>
      <p className="text-[10px] tracking-[0.35em] uppercase" style={{ color:`${fg}42` }}>{label}</p>
      {hint && <p className="text-[11px] mt-2" style={{ color:`${fg}52` }}>{hint}</p>}
    </div>
  );
}

/** A member's personal home: membership, savings, orders and quick actions. */
export function MemberDashboard() {
  const store = useStore();
  const navigate = useNavigate();
  const [profile] = useState(() => getStoredProfile());
  const [orders, setOrders] = useState<OrderSummaryViewModel[] | null>(null);

  useEffect(() => {
    if (!profile) return;
    avraApi.ordering.list()
      .then(setOrders)
      .catch(() => setOrders([]));
  }, [profile]);

  if (!profile) {
    return (
      <div className="pt-32 pb-24 px-6 min-h-screen flex items-center justify-center">
        <div className="relative rounded-3xl p-10 max-w-md w-full text-center overflow-hidden" style={{ ...G.soft }}>
          <Shine />
          <UserRound size={40} strokeWidth={1} style={{ color:gold }} className="mx-auto mb-5" />
          <h1 className="text-3xl mb-2" style={{ fontFamily:bodoni, color:fg }}>Your Dashboard</h1>
          <p className="text-sm mb-8" style={{ color:`${fg}52` }}>Sign in to see your membership, savings and orders.</p>
          <GoldBtn onClick={store.openAccount}><Bracket>Sign in</Bracket></GoldBtn>
        </div>
      </div>
    );
  }

  const orderCount = orders?.length ?? 0;
  const totalSpent = orders?.reduce((sum, o) => sum + (o.total ?? 0), 0) ?? 0;
  const trialEnds = formatDate(store.membershipTrialEndsAt);
  const nextBilling = formatDate(store.membershipNextBillingDate);
  const membershipHint = trialEnds ? `Trial ends ${trialEnds}` : nextBilling ? `Renews ${nextBilling}` : undefined;

  return (
    <div className="pt-28 pb-24 px-6 min-h-screen">
      <div className="max-w-5xl mx-auto">
        <div className="mb-8">
          <p className="text-[10px] tracking-[0.45em] uppercase mb-2" style={{ color:`${gold}` }}>The AVRA Club</p>
          <h1 className="text-[clamp(2.5rem,6vw,4rem)] leading-none" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
            Welcome back, {profile.firstName}
          </h1>
          <p className="text-sm mt-3" style={{ color:`${fg}52` }}>{profile.email}</p>
        </div>

        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
          <StatCard icon={Crown} label="Membership"
            value={store.membershipPlanName ?? "Not a member"}
            hint={store.membershipPlanName ? membershipHint : "Join the Club for member pricing"} />
          <StatCard icon={Sparkles} label="Your discount"
            value={store.discount.percentage > 0 ? `${store.discount.percentage}% off` : "None yet"}
            hint={store.discount.label || undefined} />
          <StatCard icon={Package} label="Orders" value={String(orderCount)}
            hint={totalSpent > 0 ? `${money(totalSpent)} lifetime` : "No orders yet"} />
        </div>

        {!store.membershipPlanName && (
          <div className="relative rounded-3xl p-6 mb-8 overflow-hidden flex flex-wrap items-center justify-between gap-4" style={{ ...G.gold }}>
            <Shine gold />
            <div>
              <p className="text-lg" style={{ fontFamily:bodoni, color:fg }}>Become a member</p>
              <p className="text-xs mt-1" style={{ color:`${fg}70` }}>Unlock member pricing, events and the community.</p>
            </div>
            <GoldBtn onClick={()=>navigate("/club")}><Bracket>Join the Club</Bracket></GoldBtn>
          </div>
        )}

        <div className="grid lg:grid-cols-[1.4fr_1fr] gap-4">
          <div className="relative rounded-3xl p-6 overflow-hidden" style={{ ...G.soft }}>
            <Shine />
            <div className="flex items-center justify-between mb-4">
              <p className="text-[10px] tracking-[0.35em] uppercase" style={{ color:gold }}>Recent orders</p>
              <button onClick={store.openOrders} className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}52` }}>View all</button>
            </div>
            {orders === null && <p className="text-xs" style={{ color:`${fg}38` }}>Loading…</p>}
            {orders !== null && orders.length === 0 && (
              <div className="text-center py-10">
                <p className="text-xs mb-4" style={{ color:`${fg}38` }}>You have not placed any orders yet.</p>
                <GoldBtn onClick={()=>navigate("/shop")}><Bracket>Shop the collection</Bracket></GoldBtn>
              </div>
            )}
            <div className="space-y-2">
              {orders?.slice(0, 6).map(order => (
                <div key={order.orderNumber} className="flex items-center justify-between gap-3 rounded-2xl px-4 py-3" style={{ ...G.frosted }}>
                  <div className="min-w-0">
                    <p className="text-xs" style={{ color:fg }}>Order #{order.orderNumber}</p>
                    <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}34` }}>{formatDate(order.date)} · {order.status}</p>
                  </div>
                  <p className="text-sm shrink-0" style={{ color:gold }}>{money(order.total)}</p>
                </div>
              ))}
            </div>
          </div>

          <div className="relative rounded-3xl p-6 overflow-hidden" style={{ ...G.soft }}>
            <Shine />
            <p className="text-[10px] tracking-[0.35em] uppercase mb-4" style={{ color:gold }}>Quick actions</p>
            <div className="space-y-2">
              <button onClick={store.openAccount} className="w-full flex items-center gap-3 rounded-2xl px-4 py-3 text-xs tracking-widest uppercase" style={{ ...G.frosted, color:`${fg}70` }}>
                <UserRound size={14} style={{ color:gold }} /> Manage account
              </button>
              <button onClick={store.openOrders} className="w-full flex items-center gap-3 rounded-2xl px-4 py-3 text-xs tracking-widest uppercase" style={{ ...G.frosted, color:`${fg}70` }}>
                <Package size={14} style={{ color:gold }} /> Orders &amp; receipts
              </button>
              <button onClick={()=>navigate("/events")} className="w-full flex items-center gap-3 rounded-2xl px-4 py-3 text-xs tracking-widest uppercase" style={{ ...G.frosted, color:`${fg}70` }}>
                <CalendarClock size={14} style={{ color:gold }} /> Upcoming events
              </button>
              <button onClick={()=>navigate("/tickets")} className="w-full flex items-center gap-3 rounded-2xl px-4 py-3 text-xs tracking-widest uppercase" style={{ ...G.frosted, color:`${fg}70` }}>
                <Sparkles size={14} style={{ color:gold }} /> My tickets
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
