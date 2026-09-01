import { useCallback, useEffect, useState } from "react";
import { Check, Trash2 } from "lucide-react";
import {
  avraApi, isApiUnavailable,
  type MemberDiscountDto, type MembershipPlanDto, type UserProfileDto,
} from "@/app/api/avraApi";
import { G, Shine, bg, bodoni, danger, fg, gold, goldGrad, money } from "@/shared/theme";
import { Card, Notice, PageHeader, adminFieldClass } from "@/admin/ui";

/** Tier rates plus per-member overrides. An override always beats the tier rate. */
export function DiscountsPage() {
  const [tiers, setTiers] = useState<MembershipPlanDto[]>([]);
  const [rates, setRates] = useState<Record<string, string>>({});
  const [overrides, setOverrides] = useState<MemberDiscountDto[]>([]);
  const [results, setResults] = useState<UserProfileDto[]>([]);
  const [term, setTerm] = useState("");
  const [picked, setPicked] = useState<UserProfileDto|null>(null);
  const [grant, setGrant] = useState({ percentage:"", reason:"", expiresAt:"" });
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string|null>(null);
  const [message, setMessage] = useState<string|null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [plans, granted] = await Promise.all([avraApi.memberships.plans(), avraApi.memberships.discounts()]);
      setTiers(plans);
      setRates(Object.fromEntries(plans.map(plan => [plan.id, String(plan.discountPercentage)])));
      setOverrides(granted);
    } catch (loadError) {
      setError(isApiUnavailable(loadError) ? "User service is offline." : "Unable to load member discounts.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const run = async (action: () => Promise<unknown>, success: string) => {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await action();
      setMessage(success);
      await load();
    } catch (actionError) {
      setError(isApiUnavailable(actionError) ? "User service is offline." : String(actionError instanceof Error ? actionError.message : actionError));
    } finally {
      setBusy(false);
    }
  };

  const search = async (nextTerm: string) => {
    setTerm(nextTerm);
    if (nextTerm.trim().length < 2) { setResults([]); return; }
    try {
      setResults(await avraApi.users.search(nextTerm));
    } catch {
      setResults([]);
    }
  };

  const submitGrant = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!picked) { setError("Choose a member first."); return; }
    await run(
      () => avraApi.memberships.grantDiscount(picked.id, {
        percentage: Number(grant.percentage) || 0,
        reason: grant.reason,
        expiresAt: grant.expiresAt ? new Date(`${grant.expiresAt}T23:59:59Z`).toISOString() : null,
      }),
      `Discount granted to ${picked.firstName} ${picked.lastName}.`,
    );
    setPicked(null);
    setResults([]);
    setTerm("");
    setGrant({ percentage:"", reason:"", expiresAt:"" });
  };

  return (
    <div className="space-y-4">
      <PageHeader title="Discounts" description="Set the rate each membership tier earns, or grant one member an override." />

      <div className="grid lg:grid-cols-[1fr_1.15fr] gap-5">
        <div className="space-y-5">
          <div className="relative rounded-3xl p-5 overflow-hidden space-y-3" style={{ ...G.soft }}>
            <Shine />
            <span className="text-[10px] tracking-widest uppercase" style={{ color:gold }}>Tier rates</span>
            <p className="text-[10px] leading-relaxed" style={{ color:`${fg}34` }}>
              Applies to every active member on the tier. Overrides take precedence.
            </p>
            {loading && <Notice>Loading tiers…</Notice>}
            {tiers.map(tier => (
              <div key={tier.id} className="flex items-center gap-3">
                <div className="flex-1 min-w-0">
                  <p className="text-sm truncate" style={{ color:fg }}>{tier.name}</p>
                  <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}30` }}>{money(tier.monthlyPrice)}/month</p>
                </div>
                <input type="number" min="0" max="100" step="1" value={rates[tier.id] ?? ""}
                  onChange={e=>setRates(prev => ({ ...prev, [tier.id]: e.target.value }))}
                  className="w-20 px-3 py-2 rounded-xl text-sm outline-none text-right" style={{ ...G.frosted, color:fg }} />
                <span className="text-xs" style={{ color:`${fg}38` }}>%</span>
                <button type="button" disabled={busy}
                  onClick={()=>void run(() => avraApi.memberships.setPlanDiscount(tier.id, Number(rates[tier.id]) || 0), `${tier.name} rate saved.`)}
                  className="px-3 py-2 rounded-xl text-[9px] font-bold tracking-widest uppercase disabled:opacity-60"
                  style={{ background:goldGrad, color:bg }}>Save</button>
              </div>
            ))}
          </div>

          <form onSubmit={submitGrant} className="relative rounded-3xl p-5 overflow-hidden space-y-3" style={{ ...G.soft }}>
            <Shine />
            <span className="text-[10px] tracking-widest uppercase" style={{ color:gold }}>Grant to a member</span>

            {picked ? (
              <div className="flex items-center justify-between gap-3 px-4 py-3 rounded-2xl" style={{ ...G.frosted }}>
                <div className="min-w-0">
                  <p className="text-sm truncate" style={{ color:fg }}>{picked.firstName} {picked.lastName}</p>
                  <p className="text-[10px] truncate" style={{ color:`${fg}34` }}>{picked.email}</p>
                </div>
                <button type="button" onClick={()=>setPicked(null)} className="text-[10px] tracking-widest uppercase shrink-0" style={{ color:`${fg}38` }}>Change</button>
              </div>
            ) : (
              <>
                <input value={term} onChange={e=>void search(e.target.value)} placeholder="Search members by name or email"
                  className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
                {results.length > 0 && (
                  <div className="space-y-1.5 max-h-40 overflow-y-auto">
                    {results.map(user => (
                      <button key={user.id} type="button" onClick={()=>{ setPicked(user); setResults([]); }}
                        className="w-full text-left px-4 py-2.5 rounded-xl" style={{ ...G.frosted }}>
                        <p className="text-xs truncate" style={{ color:fg }}>{user.firstName} {user.lastName}</p>
                        <p className="text-[10px] truncate" style={{ color:`${fg}34` }}>{user.email}</p>
                      </button>
                    ))}
                  </div>
                )}
              </>
            )}

            <div className="grid grid-cols-2 gap-3">
              <input type="number" min="0" max="100" step="1" required value={grant.percentage}
                onChange={e=>setGrant(prev => ({ ...prev, percentage: e.target.value }))} placeholder="Discount %"
                className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
              <input type="date" value={grant.expiresAt}
                onChange={e=>setGrant(prev => ({ ...prev, expiresAt: e.target.value }))}
                className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
            </div>
            <input required value={grant.reason} onChange={e=>setGrant(prev => ({ ...prev, reason: e.target.value }))}
              placeholder="Reason (internal)" className={adminFieldClass} style={{ ...G.frosted, color:fg }} />
            <p className="text-[10px]" style={{ color:`${fg}28` }}>Leave the date empty for a discount that runs until you revoke it.</p>

            {error && <Notice tone="error">{error}</Notice>}
            {message && <Notice>{message}</Notice>}

            <button disabled={busy || !picked} className="w-full py-4 rounded-2xl text-xs font-bold tracking-widest uppercase disabled:opacity-60"
              style={{ background:goldGrad, color:bg }}>
              <span className="flex items-center justify-center gap-2"><Check size={13}/>{busy ? "Saving" : "Grant Discount"}</span>
            </button>
          </form>
        </div>

        <div className="space-y-3">
          {loading && <Notice>Loading member discounts…</Notice>}
          {!loading && overrides.length === 0 && <Notice>No member overrides. Everyone is on their tier rate.</Notice>}
          {overrides.map(member => (
            <Card key={member.userId}>
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-sm font-semibold truncate" style={{ color:fg }}>{member.fullName || member.email}</p>
                  <p className="text-[10px] tracking-widest uppercase truncate" style={{ color:`${fg}34` }}>
                    {member.email}{member.planName ? ` · ${member.planName}` : ""}
                  </p>
                  <p className="text-xs mt-1.5 truncate" style={{ color:`${fg}42` }}>{member.reason}</p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <div className="text-right">
                    <p className="text-lg" style={{ fontFamily:bodoni, color:member.isExpired ? `${fg}38` : gold }}>{member.percentage}%</p>
                    <p className="text-[9px] tracking-widest uppercase" style={{ color:member.isExpired ? danger : `${fg}30` }}>
                      {member.isExpired ? "Expired" : member.expiresAt ? `Until ${new Date(member.expiresAt).toLocaleDateString()}` : "No expiry"}
                    </p>
                  </div>
                  <button type="button" disabled={busy}
                    onClick={()=>void run(() => avraApi.memberships.revokeDiscount(member.userId), "Discount revoked.")}
                    className="p-2 rounded-full hover:bg-white/10" style={{ color:danger }}><Trash2 size={14}/></button>
                </div>
              </div>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}
