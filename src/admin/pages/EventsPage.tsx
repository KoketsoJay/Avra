import { useCallback, useEffect, useState } from "react";
import { Check, Eye, EyeOff, Megaphone, Pencil, RefreshCw, Trash2 } from "lucide-react";
import { avraApi, isApiUnavailable, type PromotionDto, type PromotionPayload } from "@/app/api/avraApi";
import { DEFAULT_EVENT_IMAGE, resolveImage, type AvraEvent } from "@/shared/catalogue";
import { G, Shine, bg, danger, fg, gold, goldGrad, money } from "@/shared/theme";
import { Card, FilterButton, Notice, PageHeader, adminFieldClass } from "@/admin/ui";
import type { Catalogue } from "@/admin/AdminApp";

type EventsTab = "events" | "promotions";

const emptyEventForm = () => ({
  id: "",
  title: "",
  category: "Workshop",
  price: "0",
  date: new Date().toISOString().slice(0, 10),
  time: "10:00 AM",
  location: "Johannesburg, GP",
  image: DEFAULT_EVENT_IMAGE,
});

const emptyPromotionForm = () => {
  const start = new Date();
  const end = new Date();
  end.setDate(end.getDate() + 14);
  return {
    id: "",
    title: "",
    description: "",
    category: "Seasonal",
    promoCode: "",
    discountPercentage: "10",
    startDate: start.toISOString().slice(0, 10),
    endDate: end.toISOString().slice(0, 10),
    isFeatured: false,
  };
};

/** Events and the marketing promotions that run alongside them. */
export function EventsPage({ catalogue }: { catalogue: Catalogue }) {
  const [tab, setTab] = useState<EventsTab>("events");

  return (
    <div className="space-y-4">
      <PageHeader title="Events" description="Schedule events and run the promotions that support them." />

      <div className="flex flex-wrap items-center gap-2">
        <FilterButton selected={tab === "events"} onClick={()=>setTab("events")}>Events ({catalogue.events.length})</FilterButton>
        <FilterButton selected={tab === "promotions"} onClick={()=>setTab("promotions")}>Promotions</FilterButton>
      </div>

      {tab === "events" ? <EventsTabPanel catalogue={catalogue} /> : <PromotionsTabPanel />}
    </div>
  );
}

function EventsTabPanel({ catalogue }: { catalogue: Catalogue }) {
  const [form, setForm] = useState(emptyEventForm);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string|null>(null);

  const setField = (field:string, value:string) => setForm(prev => ({ ...prev, [field]:value }));

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    try {
      const price = Number(form.price);
      await catalogue.saveEvent({
        id: form.id || `event-${Date.now()}`,
        title: form.title,
        category: form.category,
        price: price === 0 ? "Free" : price || 0,
        date: form.date,
        time: form.time,
        location: form.location,
        image: form.image || DEFAULT_EVENT_IMAGE,
      } as AvraEvent);
      setMessage(form.id ? "Event updated." : "Event created.");
      setForm(emptyEventForm());
    } catch (error) {
      console.warn("Event save failed", error);
      setMessage("Could not save. The events service is unavailable.");
    } finally {
      setBusy(false);
    }
  };

  const edit = (item: AvraEvent) => setForm({
    id: item.id,
    title: item.title,
    category: item.category,
    price: String(item.price === "Free" ? 0 : item.price),
    date: item.date,
    time: item.time,
    location: item.location,
    image: item.image,
  });

  return (
    <div className="grid lg:grid-cols-[1fr_1.15fr] gap-5">
      <form onSubmit={submit} className="relative rounded-3xl p-5 overflow-hidden space-y-3 self-start" style={{ ...G.soft }}>
        <Shine />
        <div className="flex items-center justify-between gap-3 mb-2">
          <span className="text-[10px] tracking-widest uppercase" style={{ color:gold }}>{form.id ? "Edit" : "Create"} event</span>
          {form.id && <button type="button" onClick={()=>setForm(emptyEventForm())} className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}38` }}>Clear</button>}
        </div>

        <input required value={form.title} onChange={e=>setField("title", e.target.value)} placeholder="Title" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <input required value={form.category} onChange={e=>setField("category", e.target.value)} placeholder="Category" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <input type="number" min="0" step="0.01" value={form.price} onChange={e=>setField("price", e.target.value)} placeholder="Price (0 for free)" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <div className="grid grid-cols-2 gap-3">
          <input type="date" value={form.date} onChange={e=>setField("date", e.target.value)} className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          <input value={form.time} onChange={e=>setField("time", e.target.value)} placeholder="Time" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        </div>
        <input required value={form.location} onChange={e=>setField("location", e.target.value)} placeholder="Location" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <input value={form.image} onChange={e=>setField("image", e.target.value)} placeholder="Image reference" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />

        {message && <Notice>{message}</Notice>}
        <button disabled={busy} className="w-full py-4 rounded-2xl text-xs font-bold tracking-widest uppercase disabled:opacity-60" style={{ background:goldGrad, color:bg }}>
          <span className="flex items-center justify-center gap-2"><Check size={13}/>{busy ? "Saving" : form.id ? "Save Changes" : "Create Event"}</span>
        </button>
      </form>

      <div className="space-y-3">
        {catalogue.events.length === 0 && <Notice>No events scheduled.</Notice>}
        {catalogue.events.map(item => (
          <div key={item.id} className="relative rounded-2xl p-3 flex items-center gap-3 overflow-hidden" style={{ ...G.frosted }}>
            <Shine />
            <img src={resolveImage(item.image)} alt="" className="w-14 h-14 rounded-xl object-cover bg-[#111118]" />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold truncate" style={{ color:fg }}>{item.title}</p>
              <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>
                {item.category} · {item.date} · {item.price === "Free" ? "Free" : money(item.price)}
              </p>
            </div>
            <button aria-label={`Edit ${item.title}`} onClick={()=>edit(item)} className="p-2 rounded-full hover:bg-white/10" style={{ color:gold }}><Pencil size={14}/></button>
            <button aria-label={`Delete ${item.title}`} onClick={()=>void catalogue.deleteEvent(item.id)} className="p-2 rounded-full hover:bg-white/10" style={{ color:danger }}><Trash2 size={14}/></button>
          </div>
        ))}
      </div>
    </div>
  );
}

function PromotionsTabPanel() {
  const [promotions, setPromotions] = useState<PromotionDto[]>([]);
  const [form, setForm] = useState(emptyPromotionForm);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string|null>(null);
  const [message, setMessage] = useState<string|null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setPromotions(await avraApi.promotions.adminList());
    } catch (loadError) {
      setError(isApiUnavailable(loadError) ? "Events service is offline." : "Unable to load promotions.");
      setPromotions([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const setField = (field:string, value:string|boolean) => setForm(prev => ({ ...prev, [field]:value }));

  const toPayload = (): PromotionPayload => ({
    title: form.title,
    description: form.description,
    category: form.category,
    promoCode: form.promoCode || undefined,
    discountPercentage: Number(form.discountPercentage) || 0,
    startDate: new Date(`${form.startDate}T00:00:00Z`).toISOString(),
    endDate: new Date(`${form.endDate}T23:59:59Z`).toISOString(),
    isFeatured: form.isFeatured,
  });

  const run = async (action: () => Promise<unknown>, success: string) => {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await action();
      setMessage(success);
      await load();
    } catch (actionError) {
      setError(isApiUnavailable(actionError) ? "Events service is offline." : String(actionError instanceof Error ? actionError.message : actionError));
    } finally {
      setBusy(false);
    }
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    const editingId = form.id;
    await run(
      () => editingId ? avraApi.promotions.update(editingId, toPayload()) : avraApi.promotions.create(toPayload()),
      editingId ? "Promotion updated." : "Promotion drafted — publish it when you are ready.",
    );
    setForm(emptyPromotionForm());
  };

  const edit = (promotion: PromotionDto) => setForm({
    id: promotion.id,
    title: promotion.title,
    description: promotion.description,
    category: promotion.category,
    promoCode: promotion.promoCode ?? "",
    discountPercentage: String(promotion.discountPercentage),
    startDate: promotion.startDate.slice(0, 10),
    endDate: promotion.endDate.slice(0, 10),
    isFeatured: promotion.isFeatured,
  });

  return (
    <div className="grid lg:grid-cols-[1fr_1.15fr] gap-5">
      <form onSubmit={submit} className="relative rounded-3xl p-5 overflow-hidden space-y-3 self-start" style={{ ...G.soft }}>
        <Shine />
        <div className="flex items-center justify-between gap-3 mb-2">
          <span className="text-[10px] tracking-widest uppercase" style={{ color:gold }}>{form.id ? "Edit" : "Draft"} promotion</span>
          {form.id && <button type="button" onClick={()=>setForm(emptyPromotionForm())} className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}38` }}>Clear</button>}
        </div>

        <input required value={form.title} onChange={e=>setField("title", e.target.value)} placeholder="Title" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <input required value={form.category} onChange={e=>setField("category", e.target.value)} placeholder="Category" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <div className="grid grid-cols-2 gap-3">
          <input value={form.promoCode} onChange={e=>setField("promoCode", e.target.value)} placeholder="Promo code" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          <input type="number" min="0" max="100" step="1" value={form.discountPercentage} onChange={e=>setField("discountPercentage", e.target.value)} placeholder="Discount %" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <input type="date" required value={form.startDate} onChange={e=>setField("startDate", e.target.value)} className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          <input type="date" required value={form.endDate} onChange={e=>setField("endDate", e.target.value)} className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        </div>
        <textarea required rows={4} value={form.description} onChange={e=>setField("description", e.target.value)} placeholder="Description"
          className={`${adminFieldClass} resize-none`} style={{ ...G.frosted, color:fg }} />
        <label className="flex items-center gap-2 text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>
          <input type="checkbox" checked={form.isFeatured} onChange={e=>setField("isFeatured", e.target.checked)} /> Feature this promotion
        </label>

        {error && <Notice tone="error">{error}</Notice>}
        {message && <Notice>{message}</Notice>}

        <button disabled={busy} className="w-full py-4 rounded-2xl text-xs font-bold tracking-widest uppercase disabled:opacity-60" style={{ background:goldGrad, color:bg }}>
          <span className="flex items-center justify-center gap-2"><Check size={13}/>{busy ? "Saving" : form.id ? "Save Changes" : "Save Draft"}</span>
        </button>
      </form>

      <div className="space-y-3">
        <button onClick={()=>void load()} aria-label="Refresh promotions" className="ml-auto flex p-2 rounded-full hover:bg-white/10" style={{ color:gold }}>
          <RefreshCw size={14} />
        </button>
        {loading && <Notice>Loading promotions…</Notice>}
        {!loading && promotions.length === 0 && <Notice>No promotions yet.</Notice>}
        {promotions.map(promotion => {
          const published = promotion.status === "Published" || promotion.status === 1;
          return (
            <Card key={promotion.id} className="space-y-2">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-sm font-semibold truncate flex items-center gap-2" style={{ color:fg }}>
                    <Megaphone size={13} style={{ color:gold }}/> {promotion.title}
                  </p>
                  <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>
                    {promotion.discountPercentage}% off{promotion.promoCode ? ` · ${promotion.promoCode}` : ""} · until {new Date(promotion.endDate).toLocaleDateString()}
                  </p>
                </div>
                <span className="text-[10px] tracking-widest uppercase shrink-0" style={{ color:promotion.isLive ? gold : `${fg}38` }}>
                  {promotion.isLive ? "Live" : published ? "Published" : "Draft"}
                </span>
              </div>
              <div className="flex items-center gap-1">
                <button disabled={busy}
                  onClick={()=>void run(() => published ? avraApi.promotions.unpublish(promotion.id) : avraApi.promotions.publish(promotion.id), published ? "Promotion unpublished." : "Promotion published.")}
                  className="p-2 rounded-full hover:bg-white/10" style={{ color:gold }}>
                  {published ? <EyeOff size={14}/> : <Eye size={14}/>}
                </button>
                <button onClick={()=>edit(promotion)} className="p-2 rounded-full hover:bg-white/10" style={{ color:gold }}><Pencil size={14}/></button>
                <button disabled={busy} onClick={()=>void run(() => avraApi.promotions.delete(promotion.id), "Promotion removed.")}
                  className="p-2 rounded-full hover:bg-white/10" style={{ color:danger }}><Trash2 size={14}/></button>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
