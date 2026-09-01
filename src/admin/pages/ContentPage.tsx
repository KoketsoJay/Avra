import { useEffect, useState } from "react";
import { Check, Pencil, Trash2 } from "lucide-react";
import { getAvraUser } from "@/app/api/avraApi";
import {
  DEFAULT_COMMUNITY_IMAGE, DEFAULT_PRODUCT_IMAGES, DEFAULT_WELLNESS_IMAGE,
  resolveImage,
  type CommunityCircle, type Product, type WellnessProgram,
} from "@/shared/catalogue";
import { G, Shine, bg, danger, fg, gold, goldGrad, money } from "@/shared/theme";
import { Card, FilterButton, Notice, PageHeader, adminFieldClass } from "@/admin/ui";
import type { Catalogue } from "@/admin/AdminApp";

type ContentKind = "products" | "wellness" | "communities";
type ContentForm = Record<string, string>;

const KINDS: Array<{ key:ContentKind; label:string }> = [
  { key:"products",    label:"Store" },
  { key:"wellness",    label:"Wellness" },
  { key:"communities", label:"Community" },
];

function emptyForm(kind: ContentKind): ContentForm {
  if (kind === "products") return { name:"", category:"Apparel", price:"0", description:"", tag:"", img1:DEFAULT_PRODUCT_IMAGES[0], img2:DEFAULT_PRODUCT_IMAGES[1] };
  if (kind === "wellness") return { name:"", category:"Yoga", level:"Beginner", durationDays:"30", price:"0", description:"", image:DEFAULT_WELLNESS_IMAGE };
  return { name:"", category:"Wellness", memberCount:"1", description:"", image:DEFAULT_COMMUNITY_IMAGE };
}

/** Products, wellness plans and community circles — everything merchandised on the storefront. */
export function ContentPage({ catalogue }: { catalogue: Catalogue }) {
  const [kind, setKind] = useState<ContentKind>("products");
  const [editingId, setEditingId] = useState<string|null>(null);
  const [form, setForm] = useState<ContentForm>(() => emptyForm("products"));
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string|null>(null);

  useEffect(() => {
    setForm(emptyForm(kind));
    setEditingId(null);
    setMessage(null);
  }, [kind]);

  const setField = (field:string, value:string) => setForm(prev => ({ ...prev, [field]:value }));

  const items = kind === "products" ? catalogue.products : kind === "wellness" ? catalogue.programs : catalogue.communities;

  const editItem = (item: Product | WellnessProgram | CommunityCircle) => {
    setEditingId(String(item.id));
    if (kind === "products") {
      const product = item as Product;
      setForm({ name:product.name, category:product.category, price:String(product.price), description:product.description, tag:product.tag ?? "", img1:product.img1, img2:product.img2 });
    } else if (kind === "wellness") {
      const program = item as WellnessProgram;
      setForm({ name:program.name, category:program.category, level:program.level, durationDays:String(program.durationDays), price:String(program.price), description:program.description, image:program.image });
    } else {
      const community = item as CommunityCircle;
      setForm({ name:community.name, category:community.category, memberCount:String(community.memberCount), description:community.description, image:community.image });
    }
  };

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    try {
      if (kind === "products") {
        await catalogue.saveProduct({
          id: editingId ? Number(editingId) : Date.now(),
          name:form.name, category:form.category, price:Number(form.price) || 0,
          description:form.description, tag:form.tag || undefined,
          img1:form.img1 || DEFAULT_PRODUCT_IMAGES[0], img2:form.img2 || DEFAULT_PRODUCT_IMAGES[1],
        });
      } else if (kind === "wellness") {
        await catalogue.saveProgram({
          id: editingId ?? `plan-${Date.now()}`,
          name:form.name, category:form.category, level:form.level,
          durationDays:Number(form.durationDays) || 1, price:Number(form.price) || 0,
          description:form.description, image:form.image || DEFAULT_WELLNESS_IMAGE,
        });
      } else {
        await catalogue.saveCommunity({
          id: editingId ?? `community-${Date.now()}`,
          name:form.name, category:form.category, memberCount:Number(form.memberCount) || 1,
          description:form.description, image:form.image || DEFAULT_COMMUNITY_IMAGE,
        }, getAvraUser().userId);
      }
      setMessage(editingId ? "Content updated." : "Content created.");
      setEditingId(null);
      setForm(emptyForm(kind));
    } catch (error) {
      console.warn("Content save failed", error);
      setMessage("Could not save. The service is unavailable — nothing was written to the database.");
    } finally {
      setBusy(false);
    }
  };

  const remove = async (id: string | number) => {
    setBusy(true);
    try {
      if (kind === "products") await catalogue.deleteProduct(Number(id));
      else if (kind === "wellness") await catalogue.deleteProgram(String(id));
      else await catalogue.deleteCommunity(String(id));
      setMessage("Content removed.");
      if (editingId === String(id)) {
        setEditingId(null);
        setForm(emptyForm(kind));
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-4">
      <PageHeader title="Content" description="What the storefront merchandises: products, wellness plans and community circles." />

      <div className="flex flex-wrap items-center gap-2">
        {KINDS.map(option => (
          <FilterButton key={option.key} selected={kind === option.key} onClick={()=>setKind(option.key)}>
            {option.label} ({option.key === "products" ? catalogue.products.length : option.key === "wellness" ? catalogue.programs.length : catalogue.communities.length})
          </FilterButton>
        ))}
      </div>

      <div className="grid lg:grid-cols-[1fr_1.15fr] gap-5">
        <form onSubmit={submit} className="relative rounded-3xl p-5 overflow-hidden space-y-3 self-start" style={{ ...G.soft }}>
          <Shine />
          <div className="flex items-center justify-between gap-3 mb-2">
            <span className="text-[10px] tracking-widest uppercase" style={{ color:gold }}>{editingId ? "Edit" : "Create"}</span>
            {editingId && <button type="button" onClick={()=>{setEditingId(null);setForm(emptyForm(kind));}} className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}38` }}>Clear</button>}
          </div>

          <input required value={form.name ?? ""} onChange={e=>setField("name", e.target.value)} placeholder="Name" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          <input required value={form.category ?? ""} onChange={e=>setField("category", e.target.value)} placeholder="Category" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />

          {(kind === "products" || kind === "wellness") && (
            <input type="number" min="0" step="0.01" value={form.price ?? ""} onChange={e=>setField("price", e.target.value)} placeholder="Price" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          )}
          {kind === "wellness" && (
            <div className="grid grid-cols-2 gap-3">
              <input value={form.level ?? ""} onChange={e=>setField("level", e.target.value)} placeholder="Level" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
              <input type="number" min="1" value={form.durationDays ?? ""} onChange={e=>setField("durationDays", e.target.value)} placeholder="Days" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
            </div>
          )}
          {kind === "communities" && (
            <input type="number" min="0" value={form.memberCount ?? ""} onChange={e=>setField("memberCount", e.target.value)} placeholder="Member count" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          )}

          <textarea required rows={4} value={form.description ?? ""} onChange={e=>setField("description", e.target.value)} placeholder="Description"
            className={`${adminFieldClass} resize-none`} style={{ ...G.frosted, color:fg }} />

          {kind === "products" && (
            <>
              <input value={form.tag ?? ""} onChange={e=>setField("tag", e.target.value)} placeholder="Tag (Bestseller, New…)" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
              <div className="grid grid-cols-2 gap-3">
                <input value={form.img1 ?? ""} onChange={e=>setField("img1", e.target.value)} placeholder="Image 1" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
                <input value={form.img2 ?? ""} onChange={e=>setField("img2", e.target.value)} placeholder="Image 2" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
              </div>
            </>
          )}
          {kind !== "products" && (
            <input value={form.image ?? ""} onChange={e=>setField("image", e.target.value)} placeholder="Image reference" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          )}
          <p className="text-[10px]" style={{ color:`${fg}28` }}>
            Images are stored as a bundled file name (avra_30.jpg) or an absolute URL.
          </p>

          {message && <Notice>{message}</Notice>}
          <button disabled={busy} className="w-full py-4 rounded-2xl text-xs font-bold tracking-widest uppercase disabled:opacity-60" style={{ background:goldGrad, color:bg }}>
            <span className="flex items-center justify-center gap-2"><Check size={13}/>{busy ? "Saving" : editingId ? "Save Changes" : "Create Item"}</span>
          </button>
        </form>

        <div className="space-y-3">
          {items.length === 0 && <Notice>Nothing here yet.</Notice>}
          {items.map(item => {
            const meta = kind === "products"
              ? `${(item as Product).category} · ${money((item as Product).price)}`
              : kind === "wellness"
                ? `${(item as WellnessProgram).level} · ${(item as WellnessProgram).durationDays} days`
                : `${(item as CommunityCircle).category} · ${(item as CommunityCircle).memberCount.toLocaleString()} members`;
            const image = "img1" in item ? item.img1 : item.image;
            return (
              <div key={String(item.id)} className="relative rounded-2xl p-3 flex items-center gap-3 overflow-hidden" style={{ ...G.frosted }}>
                <Shine />
                <img src={resolveImage(image)} alt="" className="w-14 h-14 rounded-xl object-cover bg-[#111118]" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold truncate" style={{ color:fg }}>{item.name}</p>
                  <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>{meta}</p>
                </div>
                <button aria-label={`Edit ${item.name}`} onClick={()=>editItem(item)} className="p-2 rounded-full hover:bg-white/10" style={{ color:gold }}><Pencil size={14}/></button>
                <button aria-label={`Delete ${item.name}`} onClick={()=>void remove(item.id)} className="p-2 rounded-full hover:bg-white/10" style={{ color:danger }}><Trash2 size={14}/></button>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
