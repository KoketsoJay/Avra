import { useCallback, useEffect, useState } from "react";
import { RefreshCw, Truck, X } from "lucide-react";
import { avraApi, isApiUnavailable, type AdminOrderSummaryViewModel } from "@/app/api/avraApi";
import { ORDER_STATUS_COPY } from "@/shared/catalogue";
import { bodoni, danger, fg, gold, money } from "@/shared/theme";
import { Card, FilterButton, Notice, PageHeader } from "@/admin/ui";

/** Statuses the queue can be narrowed to, matching the ordering service enum. */
const ORDER_FILTERS = ["Paid", "Submitted", "AwaitingValidation", "StockConfirmed", "Shipped", "Cancelled"] as const;

/** Every order across every buyer, with the ability to cancel one that has not shipped. */
export function OrdersPage() {
  const [orders, setOrders] = useState<AdminOrderSummaryViewModel[]>([]);
  const [filter, setFilter] = useState<string>("");
  const [busy, setBusy] = useState<number|null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string|null>(null);
  const [message, setMessage] = useState<string|null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setOrders(await avraApi.ordering.allOrders(filter || undefined));
    } catch (loadError) {
      setError(isApiUnavailable(loadError) ? "Ordering service is offline." : "Unable to load orders.");
      setOrders([]);
    } finally {
      setLoading(false);
    }
  }, [filter]);

  useEffect(() => { void load(); }, [load]);

  const cancel = async (orderNumber:number) => {
    setBusy(orderNumber);
    setError(null);
    setMessage(null);
    try {
      await avraApi.ordering.cancel(orderNumber);
      setMessage(`Order ${orderNumber} cancelled.`);
      await load();
    } catch (cancelError) {
      setError(isApiUnavailable(cancelError)
        ? "Ordering service is offline."
        : `Order ${orderNumber} could not be cancelled. A shipped or paid order cannot be withdrawn.`);
    } finally {
      setBusy(null);
    }
  };

  const revenue = orders.reduce((sum, order) => sum + order.total, 0);

  return (
    <div className="space-y-4">
      <PageHeader title="Orders" description="Every order across all buyers. Cancel here; dispatch from Shipping." />

      <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
        <Card><p className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{orders.length}</p><p className="text-[10px] tracking-widest uppercase mt-1" style={{ color:`${fg}46` }}>Orders shown</p></Card>
        <Card><p className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{money(revenue)}</p><p className="text-[10px] tracking-widest uppercase mt-1" style={{ color:`${fg}46` }}>Value</p></Card>
        <Card><p className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{orders.filter(o => o.awaitingDispatch).length}</p><p className="text-[10px] tracking-widest uppercase mt-1" style={{ color:`${fg}46` }}>Awaiting dispatch</p></Card>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {["", ...ORDER_FILTERS].map(option => (
          <FilterButton key={option || "all"} selected={filter === option} onClick={()=>setFilter(option)}>
            {option ? ORDER_STATUS_COPY[option] ?? option : "All"}
          </FilterButton>
        ))}
        <button onClick={()=>void load()} aria-label="Refresh orders"
          className="ml-auto p-2 rounded-full hover:bg-white/10" style={{ color:gold }}>
          <RefreshCw size={14} />
        </button>
      </div>

      {error && <Notice tone="error">{error}</Notice>}
      {message && <Notice>{message}</Notice>}
      {loading && <Notice>Loading orders…</Notice>}
      {!loading && !error && orders.length === 0 && <Notice>No orders in this state.</Notice>}

      {orders.map(order => (
        <Card key={order.orderNumber}>
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-sm font-semibold truncate" style={{ color:fg }}>#{order.orderNumber} · {order.buyerName || "Guest"}</p>
              <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>
                {new Date(order.date).toLocaleDateString()} · {order.itemCount} item{order.itemCount === 1 ? "" : "s"} · {order.city}, {order.country}
              </p>
              {order.trackingNumber && (
                <p className="text-[11px] flex items-center gap-2 mt-1.5" style={{ color:`${fg}50` }}>
                  <Truck size={12} /> {order.carrier} · {order.trackingNumber}
                </p>
              )}
            </div>
            <div className="flex items-start gap-3 shrink-0">
              <div className="text-right">
                <p className="text-sm" style={{ fontFamily:bodoni, color:gold }}>{money(order.total)}</p>
                <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>{ORDER_STATUS_COPY[order.status] ?? order.status}</p>
              </div>
              {order.status !== "Shipped" && order.status !== "Cancelled" && (
                <button disabled={busy === order.orderNumber} onClick={()=>void cancel(order.orderNumber)}
                  aria-label={`Cancel order ${order.orderNumber}`}
                  className="p-2 rounded-full hover:bg-white/10 disabled:opacity-50" style={{ color:danger }}>
                  <X size={14}/>
                </button>
              )}
            </div>
          </div>
        </Card>
      ))}

      <p className="text-[10px] pt-2" style={{ color:`${fg}24` }}>
        Cancellation is refused by the ordering service once an order is paid — the button is a request, not a guarantee.
      </p>
    </div>
  );
}
