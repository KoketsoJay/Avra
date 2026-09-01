import { useCallback, useEffect, useState } from "react";
import { RefreshCw } from "lucide-react";
import { avraApi, isApiUnavailable, type InventoryItemDto } from "@/app/api/avraApi";
import { G, bg, bodoni, danger, fg, gold, goldGrad, money } from "@/shared/theme";
import { Card, FilterButton, Notice, PageHeader, adminInputClass } from "@/admin/ui";

/** Stock control: receive or write off units and maintain each product's reorder settings. */
export function InventoryPage() {
  const [items, setItems] = useState<InventoryItemDto[]>([]);
  const [lowOnly, setLowOnly] = useState(false);
  const [drafts, setDrafts] = useState<Record<number, { quantity:string; reason:string; restockThreshold:string; maxStockThreshold:string }>>({});
  const [busy, setBusy] = useState<number|null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string|null>(null);
  const [message, setMessage] = useState<string|null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setItems(lowOnly ? await avraApi.inventory.lowStock() : await avraApi.inventory.list());
    } catch (loadError) {
      setError(isApiUnavailable(loadError) ? "Product service is offline." : "Unable to load inventory.");
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [lowOnly]);

  useEffect(() => { void load(); }, [load]);

  const draftFor = (item: InventoryItemDto) => drafts[item.id] ?? {
    quantity: "", reason: "",
    restockThreshold: String(item.restockThreshold),
    maxStockThreshold: String(item.maxStockThreshold),
  };

  const setDraftField = (item: InventoryItemDto, field:string, value:string) =>
    setDrafts(prev => ({ ...prev, [item.id]: { ...draftFor(item), [field]:value } }));

  const applyChange = async (item: InventoryItemDto, run: () => Promise<InventoryItemDto>, success: string) => {
    setBusy(item.id);
    setError(null);
    setMessage(null);
    try {
      const updated = await run();
      setItems(current => current.map(existing => existing.id === updated.id ? updated : existing));
      setDrafts(prev => { const next = { ...prev }; delete next[item.id]; return next; });
      setMessage(success);
    } catch (changeError) {
      setError(isApiUnavailable(changeError) ? "Product service is offline." : String(changeError instanceof Error ? changeError.message : changeError));
    } finally {
      setBusy(null);
    }
  };

  const adjust = (item: InventoryItemDto) => {
    const draft = draftFor(item);
    const quantity = Number(draft.quantity);
    if (!Number.isInteger(quantity) || quantity === 0) {
      setError("Enter a whole number of units — positive to receive stock, negative to write it off.");
      return;
    }
    return applyChange(item, () => avraApi.inventory.adjust(item.id, { quantity, reason: draft.reason || "Manual adjustment" }), `${item.name} stock updated.`);
  };

  const saveThresholds = (item: InventoryItemDto) => {
    const draft = draftFor(item);
    return applyChange(item, () => avraApi.inventory.setThresholds(item.id, {
      restockThreshold: Number(draft.restockThreshold) || 0,
      maxStockThreshold: Number(draft.maxStockThreshold) || 0,
    }), `${item.name} reorder settings saved.`);
  };

  const needingReorder = items.filter(item => item.onReorder).length;

  return (
    <div className="space-y-4">
      <PageHeader title="Inventory" description="Receive stock, write it off, and keep reorder levels honest." />

      <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
        <Card><p className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{items.length}</p><p className="text-[10px] tracking-widest uppercase mt-1" style={{ color:`${fg}46` }}>Lines</p></Card>
        <Card><p className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>{items.reduce((sum, item) => sum + item.stock, 0)}</p><p className="text-[10px] tracking-widest uppercase mt-1" style={{ color:`${fg}46` }}>Units on hand</p></Card>
        <Card><p className="text-2xl" style={{ fontFamily:bodoni, color:needingReorder ? danger : gold }}>{needingReorder}</p><p className="text-[10px] tracking-widest uppercase mt-1" style={{ color:`${fg}46` }}>Need reorder</p></Card>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <FilterButton selected={!lowOnly} onClick={()=>setLowOnly(false)}>All stock</FilterButton>
        <FilterButton selected={lowOnly} onClick={()=>setLowOnly(true)}>Needs reorder</FilterButton>
        <button onClick={()=>void load()} aria-label="Refresh inventory" className="ml-auto p-2 rounded-full hover:bg-white/10" style={{ color:gold }}>
          <RefreshCw size={14} />
        </button>
      </div>

      {error && <Notice tone="error">{error}</Notice>}
      {message && <Notice>{message}</Notice>}
      {loading && <Notice>Loading inventory…</Notice>}
      {!loading && !error && items.length === 0 && <Notice>Nothing to show here.</Notice>}

      {items.map(item => {
        const draft = draftFor(item);
        return (
          <Card key={item.id} className="space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="text-sm font-semibold truncate" style={{ color:fg }}>{item.name}</p>
                <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>
                  {item.category} · {money(item.price)}{item.isActive ? "" : " · de-listed"}
                </p>
              </div>
              <div className="text-right shrink-0">
                <p className="text-lg" style={{ fontFamily:bodoni, color:item.onReorder ? danger : gold }}>{item.stock}</p>
                <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>{item.onReorder ? "Reorder" : "In stock"}</p>
              </div>
            </div>

            <div className="grid sm:grid-cols-[90px_1fr_auto] gap-2">
              <input type="number" step="1" value={draft.quantity} onChange={e=>setDraftField(item, "quantity", e.target.value)}
                placeholder="+/- units" className={adminInputClass} style={{ ...G.soft, color:fg }} />
              <input value={draft.reason} onChange={e=>setDraftField(item, "reason", e.target.value)}
                placeholder="Reason (delivery, damage, count)" className={adminInputClass} style={{ ...G.soft, color:fg }} />
              <button disabled={busy === item.id} onClick={()=>void adjust(item)}
                className="px-4 py-2 rounded-xl text-[10px] font-bold tracking-widest uppercase disabled:opacity-60"
                style={{ background:goldGrad, color:bg }}>Adjust</button>
            </div>

            <div className="grid sm:grid-cols-[1fr_1fr_auto] gap-2">
              <input type="number" min="0" value={draft.restockThreshold} onChange={e=>setDraftField(item, "restockThreshold", e.target.value)}
                placeholder="Reorder at" className={adminInputClass} style={{ ...G.soft, color:fg }} />
              <input type="number" min="1" value={draft.maxStockThreshold} onChange={e=>setDraftField(item, "maxStockThreshold", e.target.value)}
                placeholder="Warehouse cap" className={adminInputClass} style={{ ...G.soft, color:fg }} />
              <button disabled={busy === item.id} onClick={()=>void saveThresholds(item)}
                className="px-4 py-2 rounded-xl text-[10px] font-bold tracking-widest uppercase disabled:opacity-60"
                style={{ ...G.soft, color:gold }}>Save levels</button>
            </div>
          </Card>
        );
      })}
    </div>
  );
}
