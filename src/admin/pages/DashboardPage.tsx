import { useEffect, useState } from "react";
import { Boxes, Calendar, Package, ShoppingBag, TrendingUp, Truck, Users } from "lucide-react";
import {
  avraApi, isApiUnavailable,
  type AdminOrderSummaryViewModel, type InventoryItemDto, type UserProfileDto,
} from "@/app/api/avraApi";
import { G, Shine, fg, gold, money } from "@/shared/theme";
import { Card, Notice, PageHeader } from "@/admin/ui";
import type { Catalogue } from "@/admin/AdminApp";

interface Metrics {
  members: number;
  orders: number;
  awaitingDispatch: number;
  revenue: number;
  lowStock: InventoryItemDto[];
  recentOrders: AdminOrderSummaryViewModel[];
}

function Metric({ icon:Icon, label, value }: { icon:typeof Users; label:string; value:string }) {
  return (
    <Card>
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 rounded-xl flex items-center justify-center shrink-0"
          style={{ background:"rgba(191,155,94,0.16)", border:"1px solid rgba(191,155,94,0.25)" }}>
          <Icon size={16} style={{ color:gold }} />
        </div>
        <div className="min-w-0">
          <p className="text-xl leading-none mb-1" style={{ color:fg }}>{value}</p>
          <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}42` }}>{label}</p>
        </div>
      </div>
    </Card>
  );
}

/** At-a-glance overview of the store: members, orders, revenue, catalogue and stock health. */
export function DashboardPage({ catalogue }: { catalogue: Catalogue }) {
  const [metrics, setMetrics] = useState<Metrics|null>(null);
  const [message, setMessage] = useState<string|null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const [members, orders, lowStock] = await Promise.all([
          avraApi.users.searchAdmin("", true).catch(() => [] as UserProfileDto[]),
          avraApi.ordering.allOrders().catch(() => [] as AdminOrderSummaryViewModel[]),
          avraApi.inventory.lowStock().catch(() => [] as InventoryItemDto[]),
        ]);
        if (cancelled) return;
        setMetrics({
          members: members.length,
          orders: orders.length,
          awaitingDispatch: orders.filter(o => o.awaitingDispatch).length,
          revenue: orders.reduce((sum, o) => sum + (o.total ?? 0), 0),
          lowStock,
          recentOrders: [...orders].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()).slice(0, 5),
        });
      } catch (error) {
        if (!cancelled) setMessage(isApiUnavailable(error) ? "Some services are offline." : "Could not load dashboard metrics.");
      }
    })();
    return () => { cancelled = true; };
  }, []);

  return (
    <div className="space-y-4">
      <PageHeader title="Dashboard" description="A live overview of the AVRA store." />
      {message && <Notice tone="error">{message}</Notice>}

      <div className="grid grid-cols-2 lg:grid-cols-3 gap-3">
        <Metric icon={Users} label="Members" value={metrics ? String(metrics.members) : "—"} />
        <Metric icon={ShoppingBag} label="Orders" value={metrics ? String(metrics.orders) : "—"} />
        <Metric icon={TrendingUp} label="Revenue" value={metrics ? money(metrics.revenue) : "—"} />
        <Metric icon={Truck} label="Awaiting dispatch" value={metrics ? String(metrics.awaitingDispatch) : "—"} />
        <Metric icon={Package} label="Products" value={String(catalogue.products.length)} />
        <Metric icon={Calendar} label="Events" value={String(catalogue.events.length)} />
      </div>

      <div className="grid lg:grid-cols-2 gap-4">
        <div className="relative rounded-3xl p-5 overflow-hidden" style={{ ...G.soft }}>
          <Shine />
          <p className="text-[10px] tracking-widest uppercase mb-3" style={{ color:gold }}>Recent orders</p>
          {!metrics && <p className="text-xs" style={{ color:`${fg}38` }}>Loading…</p>}
          {metrics && metrics.recentOrders.length === 0 && <p className="text-xs" style={{ color:`${fg}38` }}>No orders yet.</p>}
          <div className="space-y-2">
            {metrics?.recentOrders.map(o => (
              <div key={o.orderNumber} className="flex items-center justify-between gap-3 rounded-xl px-3 py-2" style={{ ...G.frosted }}>
                <div className="min-w-0">
                  <p className="text-xs truncate" style={{ color:fg }}>#{o.orderNumber} · {o.buyerName}</p>
                  <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>{o.status} · {o.city}, {o.country}</p>
                </div>
                <p className="text-xs shrink-0" style={{ color:gold }}>{money(o.total)}</p>
              </div>
            ))}
          </div>
        </div>

        <div className="relative rounded-3xl p-5 overflow-hidden" style={{ ...G.soft }}>
          <Shine />
          <p className="text-[10px] tracking-widest uppercase mb-3 flex items-center gap-2" style={{ color:gold }}>
            <Boxes size={12}/> Low stock
          </p>
          {!metrics && <p className="text-xs" style={{ color:`${fg}38` }}>Loading…</p>}
          {metrics && metrics.lowStock.length === 0 && <p className="text-xs" style={{ color:`${fg}38` }}>Everything is well stocked.</p>}
          <div className="space-y-2">
            {metrics?.lowStock.slice(0, 6).map(item => (
              <div key={item.id} className="flex items-center justify-between gap-3 rounded-xl px-3 py-2" style={{ ...G.frosted }}>
                <p className="text-xs truncate" style={{ color:fg }}>{item.name}</p>
                <p className="text-[10px] tracking-widest uppercase shrink-0" style={{ color:`${fg}52` }}>{item.stock} left</p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
