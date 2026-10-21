import { useEffect, useState } from "react";
import { Check, Pencil, Trash2, Upload } from "lucide-react";
import { avraApi, getAvraUser } from "@/app/api/avraApi";
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

function parseImageReferences(raw: string) {
  return raw
    .split(/[\n,]/)
    .map(value => value.trim())
    .filter(Boolean)
    .filter((value, index, all) => all.findIndex(candidate => candidate.toLowerCase() === value.toLowerCase()) === index);
}

function emptyForm(kind: ContentKind): ContentForm {
  if (kind === "products") {
    return {
      name:"",
      category:"Apparel",
      price:"0",
      description:"",
      details:"",
      tag:"",
      images:`${DEFAULT_PRODUCT_IMAGES[0]}\n${DEFAULT_PRODUCT_IMAGES[1]}`,
    };
  }
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
  const [uploading, setUploading] = useState(false);
  const [previews, setPreviews] = useState<Record<string, string>>({});

  useEffect(() => {
    setForm(emptyForm(kind));
    setEditingId(null);
    setMessage(null);
    setPreviews({});
  }, [kind]);

  const setField = (field:string, value:string) => setForm(prev => ({ ...prev, [field]:value }));

  const uploadImages = async (files: FileList | null, mode: "append" | "replace") => {
    if (!files || files.length === 0) return;
    setUploading(true);
    setMessage(null);
    try {
      const results = await Promise.all(Array.from(files).map(file => avraApi.products.uploadImage(file)));
      const references = results.map(result => result.reference);
      setPreviews(prev => {
        const next = { ...prev };
        results.forEach(result => { if (result.url) next[result.reference] = result.url; });
        return next;
      });
      setForm(prev => {
        const existing = mode === "append" ? parseImageReferences(prev.images ?? "") : [];
        return { ...prev, images: [...existing, ...references].join("\n") };
      });
    } catch (error) {
      console.warn("Image upload failed", error);
      setMessage("Image upload failed. Make sure you are signed in as an administrator.");
    } finally {
      setUploading(false);
    }
  };

  const items = kind === "products" ? catalogue.products : kind === "wellness" ? catalogue.programs : catalogue.communities;

  const editItem = (item: Product | WellnessProgram | CommunityCircle) => {
    setEditingId(String(item.id));
    if (kind === "products") {
      const product = item as Product;
      setForm({
        name:product.name,
        category:product.category,
        price:String(product.price),
        description:product.description,
        details:product.details,
        tag:product.tag ?? "",
        images:product.images.join("\n"),
      });
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
        const images = parseImageReferences(form.images ?? "");
        await catalogue.saveProduct({
          id: editingId ? Number(editingId) : Date.now(),
          name:form.name, category:form.category, price:Number(form.price) || 0,
          description:form.description, tag:form.tag || undefined,
          details:form.details?.trim() || form.description,
          img1:images[0] || DEFAULT_PRODUCT_IMAGES[0],
          img2:images[1] || images[0] || DEFAULT_PRODUCT_IMAGES[1],
          images:images.length > 0 ? images : [...DEFAULT_PRODUCT_IMAGES],
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
              <textarea
                rows={3}
                value={form.details ?? ""}
                onChange={e=>setField("details", e.target.value)}
                placeholder="More details (fabric, fit, care, finish...)"
                className={`${adminFieldClass} resize-none`}
                style={{ ...G.frosted, color:fg }}
              />
              <input value={form.tag ?? ""} onChange={e=>setField("tag", e.target.value)} placeholder="Tag (Bestseller, New…)" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
              <label className="flex items-center justify-center gap-2 w-full py-3 rounded-2xl text-[10px] tracking-widest uppercase cursor-pointer" style={{ ...G.frosted, color:gold }}>
                <Upload size={13}/>{uploading ? "Uploading…" : "Upload images to storage"}
                <input type="file" accept="image/*" multiple hidden disabled={uploading}
                  onChange={e=>{ void uploadImages(e.target.files, "append"); e.target.value = ""; }} />
              </label>
              <textarea
                rows={4}
                value={form.images ?? ""}
                onChange={e=>setField("images", e.target.value)}
                placeholder="Image gallery references (one per line or comma-separated)"
                className={`${adminFieldClass} resize-none`}
                style={{ ...G.frosted, color:fg }}
              />
              {parseImageReferences(form.images ?? "").some(ref => previews[ref]) && (
                <div className="flex flex-wrap gap-2">
                  {parseImageReferences(form.images ?? "").map(ref => previews[ref] ? (
                    <img key={ref} src={previews[ref]} alt="" className="w-12 h-12 rounded-lg object-cover bg-[#111118]" />
                  ) : null)}
                </div>
              )}
            </>
          )}
          {kind !== "products" && (
            <input value={form.image ?? ""} onChange={e=>setField("image", e.target.value)} placeholder="Image reference" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
          )}
          <p className="text-[10px]" style={{ color:`${fg}28` }}>
            Product galleries accept multiple references. Use bundled file names (avra_30.jpg) or absolute URLs.
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
