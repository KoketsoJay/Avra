import { useCallback, useEffect, useState } from "react";
import { RefreshCw, Truck } from "lucide-react";
import { avraApi, isApiUnavailable, type AdminOrderSummaryViewModel } from "@/app/api/avraApi";
import { ORDER_STATUS_COPY } from "@/shared/catalogue";
import { G, bg, bodoni, fg, gold, goldGrad, money } from "@/shared/theme";
import { Card, FilterButton, Notice, PageHeader, adminInputClass } from "@/admin/ui";

const CARRIERS = ["The Courier Guy", "Aramex", "PostNet", "DSV", "Fastway"] as const;

/**
 * The dispatch desk. An order can only be shipped once it is paid, and the ordering service
 * requires both a carrier and a tracking reference before it will accept the dispatch.
 */
export function ShippingPage() {
  const [orders, setOrders] = useState<AdminOrderSummaryViewModel[]>([]);
  const [showShipped, setShowShipped] = useState(false);
  const [dispatch, setDispatch] = useState<Record<number, { carrier:string; trackingNumber:string }>>({});
  const [busy, setBusy] = useState<number|null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string|null>(null);
  const [message, setMessage] = useState<string|null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setOrders(await avraApi.ordering.allOrders(showShipped ? "Shipped" : "Paid"));
    } catch (loadError) {
      setError(isApiUnavailable(loadError) ? "Ordering service is offline." : "Unable to load the dispatch queue.");
      setOrders([]);
    } finally {
      setLoading(false);
    }
  }, [showShipped]);

  useEffect(() => { void load(); }, [load]);

  const setField = (orderNumber:number, field:"carrier"|"trackingNumber", value:string) =>
    setDispatch(prev => ({ ...prev, [orderNumber]: { carrier:"", trackingNumber:"", ...prev[orderNumber], [field]:value } }));

  const ship = async (orderNumber:number) => {
    const details = dispatch[orderNumber];
    if (!details?.carrier?.trim() || !details?.trackingNumber?.trim()) {
      setError("A carrier and a tracking number are both required to ship an order.");
      return;
    }

    setBusy(orderNumber);
    setError(null);
    setMessage(null);
    try {
      await avraApi.ordering.ship(orderNumber, { carrier:details.carrier.trim(), trackingNumber:details.trackingNumber.trim() });
      setMessage(`Order ${orderNumber} dispatched with ${details.carrier.trim()}.`);
      setDispatch(prev => { const next = { ...prev }; delete next[orderNumber]; return next; });
      await load();
    } catch (shipError) {
      setError(isApiUnavailable(shipError) ? "Ordering service is offline." : `Order ${orderNumber} could not be shipped. It must be paid first.`);
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="space-y-4">
      <PageHeader title="Shipping" description="Hand paid orders to a carrier and record the tracking reference." />

      <div className="flex flex-wrap items-center gap-2">
        <FilterButton selected={!showShipped} onClick={()=>setShowShipped(false)}>Awaiting dispatch</FilterButton>
        <FilterButton selected={showShipped} onClick={()=>setShowShipped(true)}>Already shipped</FilterButton>
        <button onClick={()=>void load()} aria-label="Refresh the dispatch queue"
          className="ml-auto p-2 rounded-full hover:bg-white/10" style={{ color:gold }}>
          <RefreshCw size={14} />
        </button>
      </div>

      {error && <Notice tone="error">{error}</Notice>}
      {message && <Notice>{message}</Notice>}
      {loading && <Notice>Loading the dispatch queue…</Notice>}
      {!loading && !error && orders.length === 0 && (
        <Notice>{showShipped ? "Nothing has shipped yet." : "Nothing is waiting to be dispatched."}</Notice>
      )}

      {orders.map(order => (
        <Card key={order.orderNumber} className="space-y-3">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-sm font-semibold truncate" style={{ color:fg }}>#{order.orderNumber} · {order.buyerName || "Guest"}</p>
              <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>
                {new Date(order.date).toLocaleDateString()} · {order.itemCount} item{order.itemCount === 1 ? "" : "s"} · {order.city}, {order.country}
              </p>
            </div>
            <div className="text-right shrink-0">
              <p className="text-sm" style={{ fontFamily:bodoni, color:gold }}>{money(order.total)}</p>
              <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>{ORDER_STATUS_COPY[order.status] ?? order.status}</p>
            </div>
          </div>

          {order.trackingNumber ? (
            <p className="text-[11px] flex items-center gap-2" style={{ color:`${fg}50` }}>
              <Truck size={12} /> {order.carrier} · {order.trackingNumber}
              {order.shippedAt && ` · ${new Date(order.shippedAt).toLocaleDateString()}`}
            </p>
          ) : order.awaitingDispatch ? (
            <div className="grid sm:grid-cols-[1fr_1fr_auto] gap-2">
              <input list="avra-carriers" value={dispatch[order.orderNumber]?.carrier ?? ""}
                onChange={e=>setField(order.orderNumber, "carrier", e.target.value)}
                placeholder="Carrier" className={adminInputClass} style={{ ...G.soft, color:fg }} />
              <input value={dispatch[order.orderNumber]?.trackingNumber ?? ""}
                onChange={e=>setField(order.orderNumber, "trackingNumber", e.target.value)}
                placeholder="Tracking number" className={adminInputClass} style={{ ...G.soft, color:fg }} />
              <button disabled={busy === order.orderNumber} onClick={()=>void ship(order.orderNumber)}
                className="px-4 py-2 rounded-xl text-[10px] font-bold tracking-widest uppercase disabled:opacity-60"
                style={{ background:goldGrad, color:bg }}>
                {busy === order.orderNumber ? "Shipping" : "Ship"}
              </button>
            </div>
          ) : (
            <p className="text-[11px]" style={{ color:`${fg}34` }}>Waiting on payment before it can be dispatched.</p>
          )}
        </Card>
      ))}

      <datalist id="avra-carriers">{CARRIERS.map(carrier => <option key={carrier} value={carrier} />)}</datalist>
    </div>
  );
}
