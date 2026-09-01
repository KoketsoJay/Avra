import { useState } from "react";
import {
  BadgePercent, Boxes, Calendar, LayoutDashboard, LogOut, Package, Truck,
} from "lucide-react";
import {
  avraApi, getStoredProfile, isAdministrator, isApiUnavailable,
  type UserProfileDto,
} from "@/app/api/avraApi";
import { useCatalogue } from "@/shared/catalogue";
import { G, Shine, bg, bodoni, fg, gold, goldGrad } from "@/shared/theme";
import { Notice, adminFieldClass } from "@/admin/ui";
import { OrdersPage } from "@/admin/pages/OrdersPage";
import { ShippingPage } from "@/admin/pages/ShippingPage";
import { InventoryPage } from "@/admin/pages/InventoryPage";
import { ContentPage } from "@/admin/pages/ContentPage";
import { EventsPage } from "@/admin/pages/EventsPage";
import { DiscountsPage } from "@/admin/pages/DiscountsPage";

export type Catalogue = ReturnType<typeof useCatalogue>;

type Section = "orders" | "shipping" | "inventory" | "content" | "events" | "discounts";

const NAV: Array<{ key:Section; label:string; icon:typeof Truck }> = [
  { key:"orders",    label:"Orders",    icon:LayoutDashboard },
  { key:"shipping",  label:"Shipping",  icon:Truck },
  { key:"inventory", label:"Inventory", icon:Boxes },
  { key:"content",   label:"Content",   icon:Package },
  { key:"events",    label:"Events",    icon:Calendar },
  { key:"discounts", label:"Discounts", icon:BadgePercent },
];

/** The storefront runs on its own origin, so the admin app holds its own session. */
function SignIn({ onSignedIn }: { onSignedIn:(profile:UserProfileDto)=>void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string|null>(null);

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const auth = await avraApi.auth.login({ email, password });
      if (!isAdministrator(auth.user)) {
        avraApi.auth.logout();
        setError("That account is not an administrator.");
        return;
      }
      onSignedIn(auth.user);
    } catch (loginError) {
      setError(isApiUnavailable(loginError) ? "User service is offline." : "Those credentials were not accepted.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center px-6" style={{ background:bg }}>
      <form onSubmit={submit} className="relative rounded-3xl p-8 w-full max-w-sm overflow-hidden space-y-4" style={{ ...G.soft }}>
        <Shine />
        <div className="text-center mb-2">
          <p className="text-3xl" style={{ fontFamily:bodoni, color:gold }}>AVRA</p>
          <p className="text-[10px] tracking-[0.35em] uppercase mt-1" style={{ color:`${fg}46` }}>Admin</p>
        </div>

        <input required type="email" value={email} onChange={e=>setEmail(e.target.value)} placeholder="Email"
          autoComplete="username" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
        <input required type="password" value={password} onChange={e=>setPassword(e.target.value)} placeholder="Password"
          autoComplete="current-password" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />

        {error && <Notice tone="error">{error}</Notice>}

        <button disabled={busy} className="w-full py-4 rounded-2xl text-xs font-bold tracking-widest uppercase disabled:opacity-60"
          style={{ background:goldGrad, color:bg }}>
          {busy ? "Signing in" : "Sign In"}
        </button>
        <p className="text-[10px] text-center" style={{ color:`${fg}28` }}>
          Administrator accounts only. Every endpoint is enforced server-side.
        </p>
      </form>
    </div>
  );
}

export default function AdminApp() {
  const [profile, setProfile] = useState<UserProfileDto|null>(() => getStoredProfile());
  const [section, setSection] = useState<Section>("orders");
  const catalogue = useCatalogue();

  if (!isAdministrator(profile)) {
    return <SignIn onSignedIn={setProfile} />;
  }

  const signOut = () => {
    avraApi.auth.logout();
    setProfile(null);
  };

  return (
    <div className="min-h-screen flex" style={{ background:bg }}>
      <aside className="w-56 shrink-0 hidden md:flex flex-col justify-between p-5"
        style={{ ...G.dark, borderRadius:0, borderRight:"1px solid rgba(255,255,255,0.07)" }}>
        <div>
          <div className="mb-8">
            <p className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>AVRA</p>
            <p className="text-[9px] tracking-[0.35em] uppercase mt-0.5" style={{ color:`${fg}40` }}>Admin</p>
          </div>
          <nav className="space-y-1">
            {NAV.map(item => {
              const Icon = item.icon;
              const selected = section === item.key;
              return (
                <button key={item.key} onClick={()=>setSection(item.key)}
                  className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs tracking-widest uppercase"
                  style={selected ? { ...G.goldHi, color:gold } : { color:`${fg}52` }}>
                  <Icon size={15}/> {item.label}
                </button>
              );
            })}
          </nav>
        </div>

        <div className="space-y-2">
          <p className="text-[10px] truncate" style={{ color:`${fg}42` }}>{profile?.firstName} {profile?.lastName}</p>
          <button onClick={signOut} className="w-full flex items-center gap-2 px-3 py-2.5 rounded-xl text-[10px] tracking-widest uppercase"
            style={{ ...G.frosted, color:`${fg}60` }}>
            <LogOut size={13}/> Sign out
          </button>
        </div>
      </aside>

      <div className="flex-1 min-w-0">
        <div className="md:hidden flex gap-1 overflow-x-auto p-3" style={{ ...G.dark, borderRadius:0 }}>
          {NAV.map(item => (
            <button key={item.key} onClick={()=>setSection(item.key)}
              className="px-3 py-2 rounded-full text-[10px] tracking-widest uppercase whitespace-nowrap"
              style={section === item.key ? { ...G.goldHi, color:gold } : { ...G.frosted, color:`${fg}46` }}>
              {item.label}
            </button>
          ))}
        </div>

        <main className="max-w-5xl mx-auto px-6 py-8">
          {section === "orders"    && <OrdersPage />}
          {section === "shipping"  && <ShippingPage />}
          {section === "inventory" && <InventoryPage />}
          {section === "content"   && <ContentPage catalogue={catalogue} />}
          {section === "events"    && <EventsPage catalogue={catalogue} />}
          {section === "discounts" && <DiscountsPage />}
        </main>
      </div>
    </div>
  );
}
