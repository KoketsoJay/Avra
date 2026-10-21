import { useCallback, useEffect, useState } from "react";
import { Check, Search, Shield, ShieldOff, Trash2, UserCog, UserRound } from "lucide-react";
import { avraApi, isApiUnavailable, type UserProfileDto } from "@/app/api/avraApi";
import { G, Shine, bg, danger, fg, gold, goldGrad } from "@/shared/theme";
import { Notice, PageHeader, adminFieldClass } from "@/admin/ui";

function isAdmin(user: UserProfileDto) {
  return (user.role ?? "").toLowerCase() === "administrator";
}

function fullName(user: UserProfileDto) {
  return `${user.firstName} ${user.lastName}`.trim() || user.email;
}

/** Search, view and manage member accounts. Every action is enforced server-side. */
export function MembersPage() {
  const [term, setTerm] = useState("");
  const [includeInactive, setIncludeInactive] = useState(true);
  const [users, setUsers] = useState<UserProfileDto[]>([]);
  const [selected, setSelected] = useState<UserProfileDto|null>(null);
  const [form, setForm] = useState({ firstName:"", lastName:"", phoneNumber:"" });
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string|null>(null);
  const [error, setError] = useState<string|null>(null);

  const load = useCallback(async (search: string) => {
    setError(null);
    try {
      setUsers(await avraApi.users.searchAdmin(search, includeInactive));
    } catch (searchError) {
      setError(isApiUnavailable(searchError) ? "User service is offline." : "Could not load members.");
    }
  }, [includeInactive]);

  useEffect(() => { void load(term); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, [includeInactive]);
  useEffect(() => { void load(""); /* eslint-disable-next-line react-hooks/exhaustive-deps */ }, []);

  const select = (user: UserProfileDto) => {
    setSelected(user);
    setForm({ firstName:user.firstName, lastName:user.lastName, phoneNumber:user.phoneNumber ?? "" });
    setMessage(null);
    setError(null);
  };

  const refreshSelected = (updated: UserProfileDto) => {
    setSelected(updated);
    setUsers(prev => prev.map(u => u.id === updated.id ? updated : u));
  };

  const saveProfile = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!selected) return;
    setBusy(true); setMessage(null); setError(null);
    try {
      const updated = await avraApi.users.adminUpdateProfile(selected.id, {
        firstName: form.firstName, lastName: form.lastName, phoneNumber: form.phoneNumber || undefined, avatar: selected.avatar ?? undefined,
      });
      refreshSelected(updated);
      setMessage("Profile saved.");
    } catch { setError("Could not save the profile."); } finally { setBusy(false); }
  };

  const toggleRole = async () => {
    if (!selected) return;
    setBusy(true); setMessage(null); setError(null);
    try {
      const updated = await avraApi.users.setRole(selected.id, isAdmin(selected) ? "customer" : "administrator");
      refreshSelected(updated);
      setMessage(`Role changed to ${updated.role}.`);
    } catch { setError("Could not change the role."); } finally { setBusy(false); }
  };

  const toggleActive = async () => {
    if (!selected) return;
    setBusy(true); setMessage(null); setError(null);
    try {
      const updated = await avraApi.users.setActive(selected.id, !(selected.isActive ?? true));
      refreshSelected(updated);
      setMessage(updated.isActive ? "Account activated." : "Account deactivated.");
    } catch { setError("Could not change the account status."); } finally { setBusy(false); }
  };

  const remove = async () => {
    if (!selected) return;
    setBusy(true); setMessage(null); setError(null);
    try {
      await avraApi.users.remove(selected.id);
      const updated = { ...selected, isActive:false };
      refreshSelected(updated);
      setMessage("Account deactivated.");
    } catch { setError("Could not deactivate the account."); } finally { setBusy(false); }
  };

  return (
    <div className="space-y-4">
      <PageHeader title="Members" description="Search, view and manage member accounts." />

      <div className="grid lg:grid-cols-[1fr_1.1fr] gap-5">
        {/* Search + list */}
        <div className="space-y-3">
          <form onSubmit={e=>{ e.preventDefault(); void load(term); }} className="relative rounded-2xl overflow-hidden flex items-center gap-2 px-3" style={{ ...G.frosted }}>
            <Search size={14} style={{ color:`${fg}52` }} />
            <input value={term} onChange={e=>setTerm(e.target.value)} placeholder="Search name or email"
              className="flex-1 bg-transparent py-3 text-sm outline-none" style={{ color:fg }} />
            <button className="text-[10px] tracking-widest uppercase px-3 py-1.5 rounded-full" style={{ ...G.goldHi, color:gold }}>Search</button>
          </form>

          <label className="flex items-center gap-2 text-[10px] tracking-widest uppercase" style={{ color:`${fg}52` }}>
            <input type="checkbox" checked={includeInactive} onChange={e=>setIncludeInactive(e.target.checked)} /> Include deactivated
          </label>

          {error && <Notice tone="error">{error}</Notice>}

          <div className="space-y-2">
            {users.length === 0 && <Notice>No members found.</Notice>}
            {users.map(user => (
              <button key={user.id} onClick={()=>select(user)}
                className="w-full relative rounded-2xl p-3 flex items-center gap-3 overflow-hidden text-left"
                style={selected?.id === user.id ? { ...G.goldHi } : { ...G.frosted }}>
                <Shine />
                <div className="w-10 h-10 rounded-xl flex items-center justify-center shrink-0" style={{ background:"rgba(255,255,255,0.05)" }}>
                  <UserRound size={15} style={{ color:`${fg}70` }} />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold truncate" style={{ color:fg }}>{fullName(user)}</p>
                  <p className="text-[10px] truncate" style={{ color:`${fg}42` }}>{user.email}</p>
                </div>
                <div className="flex flex-col items-end gap-1 shrink-0">
                  {isAdmin(user) && <span className="text-[9px] tracking-widest uppercase px-2 py-0.5 rounded-full" style={{ color:gold, border:`1px solid ${gold}44` }}>Admin</span>}
                  {user.isActive === false && <span className="text-[9px] tracking-widest uppercase px-2 py-0.5 rounded-full" style={{ color:danger, border:`1px solid ${danger}44` }}>Inactive</span>}
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Detail + manage */}
        <div className="relative rounded-3xl p-5 overflow-hidden self-start" style={{ ...G.soft }}>
          <Shine />
          {!selected ? (
            <div className="text-center py-16 flex flex-col items-center gap-3" style={{ color:`${fg}30` }}>
              <UserCog size={40} strokeWidth={1} />
              <p className="text-xs tracking-widest uppercase">Select a member to manage</p>
            </div>
          ) : (
            <div className="space-y-4">
              <div>
                <p className="text-lg" style={{ color:fg }}>{fullName(selected)}</p>
                <p className="text-xs" style={{ color:`${fg}46` }}>{selected.email}</p>
                <div className="flex flex-wrap gap-2 mt-2 text-[10px] tracking-widest uppercase">
                  <span className="px-2 py-0.5 rounded-full" style={{ color:gold, border:`1px solid ${gold}44` }}>{selected.role ?? "customer"}</span>
                  <span className="px-2 py-0.5 rounded-full" style={{ color:(selected.isActive ?? true) ? gold : danger, border:`1px solid ${((selected.isActive ?? true) ? gold : danger)}44` }}>
                    {(selected.isActive ?? true) ? "Active" : "Inactive"}
                  </span>
                  {selected.createdAt && <span className="px-2 py-0.5 rounded-full" style={{ color:`${fg}42`, border:`1px solid ${fg}22` }}>Joined {new Date(selected.createdAt).toLocaleDateString()}</span>}
                </div>
              </div>

              {message && <Notice>{message}</Notice>}
              {error && <Notice tone="error">{error}</Notice>}

              <form onSubmit={saveProfile} className="space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  <input value={form.firstName} onChange={e=>setForm(p=>({...p,firstName:e.target.value}))} placeholder="First name" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
                  <input value={form.lastName} onChange={e=>setForm(p=>({...p,lastName:e.target.value}))} placeholder="Last name" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
                </div>
                <input value={form.phoneNumber} onChange={e=>setForm(p=>({...p,phoneNumber:e.target.value}))} placeholder="Phone number" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
                <button disabled={busy} className="w-full py-3 rounded-2xl text-xs font-bold tracking-widest uppercase disabled:opacity-60" style={{ background:goldGrad, color:bg }}>
                  <span className="flex items-center justify-center gap-2"><Check size={13}/>{busy ? "Saving" : "Save profile"}</span>
                </button>
              </form>

              <div className="grid grid-cols-2 gap-2 pt-1">
                <button disabled={busy} onClick={toggleRole} className="flex items-center justify-center gap-2 py-3 rounded-2xl text-[10px] tracking-widest uppercase disabled:opacity-60" style={{ ...G.frosted, color:gold }}>
                  {isAdmin(selected) ? <><ShieldOff size={13}/> Remove admin</> : <><Shield size={13}/> Make admin</>}
                </button>
                <button disabled={busy} onClick={toggleActive} className="flex items-center justify-center gap-2 py-3 rounded-2xl text-[10px] tracking-widest uppercase disabled:opacity-60"
                  style={{ ...G.frosted, color:(selected.isActive ?? true) ? danger : gold }}>
                  {(selected.isActive ?? true) ? "Deactivate" : "Activate"}
                </button>
              </div>

              <button disabled={busy || !(selected.isActive ?? true)} onClick={remove}
                className="w-full flex items-center justify-center gap-2 py-3 rounded-2xl text-[10px] tracking-widest uppercase disabled:opacity-40"
                style={{ color:danger, border:`1px solid ${danger}33` }}>
                <Trash2 size={13}/> Deactivate account
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
