import { useCallback, useEffect, useMemo, useState } from "react";
import { Check, RefreshCw, ShieldAlert, Users } from "lucide-react";
import {
  avraApi,
  isApiUnavailable,
  type CommunityDto,
  type CommunityMemberDto,
  type CommunityPostDto,
  type CommunityReportDto,
} from "@/app/api/avraApi";
import { resolveImage } from "@/shared/catalogue";
import { G, danger, fg, gold, goldGrad } from "@/shared/theme";
import { Card, FilterButton, Notice, PageHeader } from "@/admin/ui";

type CommunityTab = "members" | "posts" | "reports";

const MEMBER_ROLES = ["Member", "Moderator", "Admin"] as const;
const REPORT_STATUSES = ["Open", "Dismissed", "Actioned"] as const;
const SUCCESS = "#86EFAC";

function roleName(role: CommunityMemberDto["role"]) {
  if (typeof role === "string") return role;
  return MEMBER_ROLES[role] ?? "Member";
}

function reportStatusName(status: CommunityReportDto["status"]) {
  if (typeof status === "string") return status;
  return REPORT_STATUSES[status] ?? "Open";
}

/** Community operations: moderation, role management and report handling. */
export function CommunityPage() {
  const [tab, setTab] = useState<CommunityTab>("members");
  const [communities, setCommunities] = useState<CommunityDto[]>([]);
  const [selectedCommunityId, setSelectedCommunityId] = useState<string | null>(null);

  const [members, setMembers] = useState<CommunityMemberDto[]>([]);
  const [posts, setPosts] = useState<CommunityPostDto[]>([]);
  const [reports, setReports] = useState<CommunityReportDto[]>([]);

  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const selectedCommunity = useMemo(
    () => communities.find(community => community.id === selectedCommunityId) ?? null,
    [communities, selectedCommunityId],
  );

  const loadCommunities = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const rows = await avraApi.communities.adminList();
      setCommunities(rows);
      setSelectedCommunityId(current => current ?? rows[0]?.id ?? null);
    } catch (loadError) {
      setCommunities([]);
      setError(isApiUnavailable(loadError) ? "Community service is offline." : "Unable to load communities.");
    } finally {
      setLoading(false);
    }
  }, []);

  const loadDetails = useCallback(async (communityId: string) => {
    setLoading(true);
    setError(null);
    try {
      const [memberRows, postRows, reportRows] = await Promise.all([
        avraApi.communities.members(communityId),
        avraApi.communities.posts(communityId, true),
        avraApi.communities.reports("Open"),
      ]);

      setMembers(memberRows);
      setPosts(postRows);
      setReports(reportRows.filter(report => report.communityId === communityId));
    } catch (loadError) {
      setMembers([]);
      setPosts([]);
      setReports([]);
      setError(isApiUnavailable(loadError) ? "Community service is offline." : "Unable to load community detail.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadCommunities();
  }, [loadCommunities]);

  useEffect(() => {
    if (!selectedCommunityId) return;
    void loadDetails(selectedCommunityId);
  }, [selectedCommunityId, loadDetails]);

  const run = async (key: string, action: () => Promise<unknown>, successMessage: string) => {
    setBusy(key);
    setError(null);
    setMessage(null);
    try {
      await action();
      if (selectedCommunityId) {
        await loadDetails(selectedCommunityId);
      }
      await loadCommunities();
      setMessage(successMessage);
    } catch (actionError) {
      setError(isApiUnavailable(actionError) ? "Community service is offline." : String(actionError instanceof Error ? actionError.message : actionError));
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="space-y-4">
      <PageHeader title="Community" description="Moderate discussions, manage member roles, and resolve reports." />

      <div className="flex items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <FilterButton selected={tab === "members"} onClick={() => setTab("members")}>Members ({members.length})</FilterButton>
          <FilterButton selected={tab === "posts"} onClick={() => setTab("posts")}>Posts ({posts.length})</FilterButton>
          <FilterButton selected={tab === "reports"} onClick={() => setTab("reports")}>Reports ({reports.length})</FilterButton>
        </div>

        <button
          onClick={() => void loadCommunities()}
          aria-label="Refresh community operations"
          className="p-2 rounded-full hover:bg-white/10"
          style={{ color: gold }}
        >
          <RefreshCw size={14} />
        </button>
      </div>

      {loading && <Notice>Loading community data…</Notice>}
      {error && <Notice tone="error">{error}</Notice>}
      {message && <Notice>{message}</Notice>}

      <div className="grid lg:grid-cols-[280px_1fr] gap-5">
        <aside className="space-y-3">
          {communities.map(community => {
            const selected = community.id === selectedCommunityId;
            const statusTone = community.status === "Active" ? SUCCESS : community.status === "Inactive" ? danger : gold;
            const deactivate = community.status === "Active";

            return (
              <Card key={community.id} className="space-y-2">
                <button onClick={() => setSelectedCommunityId(community.id)} className="w-full text-left">
                  <div className="flex gap-3 items-start">
                    <img src={resolveImage(community.imageUrl) ?? ""} alt="" className="w-11 h-11 rounded-xl object-cover bg-[#111118]" />
                    <div className="min-w-0">
                      <p className="text-sm font-semibold truncate" style={{ color: fg }}>{community.name}</p>
                      <p className="text-[10px] tracking-widest uppercase truncate" style={{ color: `${fg}34` }}>
                        {community.category} · {community.memberCount.toLocaleString()} members
                      </p>
                    </div>
                  </div>
                </button>

                <div className="flex items-center justify-between gap-2">
                  <span className="text-[10px] tracking-widest uppercase" style={{ color: statusTone }}>{community.status}</span>
                  <button
                    disabled={busy === `status-${community.id}`}
                    onClick={() => void run(
                      `status-${community.id}`,
                      () => avraApi.communities.setStatus(community.id, { status: deactivate ? "Inactive" : "Active" }),
                      deactivate ? "Community deactivated." : "Community reactivated.",
                    )}
                    className="rounded-full px-3 py-1 text-[10px] tracking-widest uppercase disabled:opacity-60"
                    style={selected ? { ...G.goldHi, color: gold } : { ...G.frosted, color: `${fg}58` }}
                  >
                    {busy === `status-${community.id}` ? "Saving" : deactivate ? "Deactivate" : "Activate"}
                  </button>
                </div>
              </Card>
            );
          })}

          {!loading && communities.length === 0 && <Notice>No communities found.</Notice>}
        </aside>

        <div className="space-y-3">
          {!selectedCommunity && <Notice>Select a community to manage.</Notice>}

          {selectedCommunity && tab === "members" && (
            <div className="space-y-3">
              {members.length === 0 && <Notice>This community has no active members.</Notice>}
              {members.map(member => (
                <Card key={member.id} className="flex flex-wrap items-center gap-3 justify-between">
                  <div>
                    <p className="text-sm font-semibold" style={{ color: fg }}>{member.userName}</p>
                    <p className="text-[10px] tracking-widest uppercase" style={{ color: `${fg}34` }}>
                      {member.userId} · joined {new Date(member.joinedAt).toLocaleDateString()}
                    </p>
                  </div>

                  <div className="flex items-center gap-2">
                    <select
                      value={roleName(member.role)}
                      onChange={event => void run(
                        `role-${member.id}`,
                        () => avraApi.communities.setMemberRole(selectedCommunity.id, member.userId, { role: event.target.value }),
                        "Member role updated.",
                      )}
                      className="px-3 py-2 rounded-xl text-xs"
                      style={{ ...G.frosted, color: fg }}
                    >
                      {MEMBER_ROLES.map(role => <option key={role} value={role}>{role}</option>)}
                    </select>
                    {busy === `role-${member.id}` && <span className="text-[10px]" style={{ color: `${fg}42` }}>Saving…</span>}
                  </div>
                </Card>
              ))}
            </div>
          )}

          {selectedCommunity && tab === "posts" && (
            <div className="space-y-3">
              {posts.length === 0 && <Notice>No posts in this community yet.</Notice>}
              {posts.map(post => (
                <Card key={post.id} className="space-y-2">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold" style={{ color: fg }}>{post.title}</p>
                      <p className="text-[10px] tracking-widest uppercase" style={{ color: `${fg}34` }}>
                        {post.userName} · likes {post.likeCount} · comments {post.commentCount}
                      </p>
                    </div>
                    <span className="text-[10px] tracking-widest uppercase" style={{ color: post.isHidden ? danger : `${fg}38` }}>
                      {post.isHidden ? "Hidden" : "Visible"}
                    </span>
                  </div>

                  <p className="text-xs leading-relaxed" style={{ color: `${fg}58` }}>{post.content}</p>
                  {post.hiddenReason && (
                    <p className="text-[10px]" style={{ color: `${fg}42` }}>Reason: {post.hiddenReason}</p>
                  )}

                  <div className="flex items-center gap-2">
                    <button
                      disabled={busy === `post-visibility-${post.id}`}
                      onClick={() => void run(
                        `post-visibility-${post.id}`,
                        () => avraApi.communities.setPostVisibility(post.id, {
                          hidden: !post.isHidden,
                          reason: post.isHidden ? undefined : "Hidden by admin moderation",
                        }),
                        post.isHidden ? "Post restored." : "Post hidden.",
                      )}
                      className="rounded-full px-3 py-1.5 text-[10px] tracking-widest uppercase disabled:opacity-60"
                      style={{ background: goldGrad, color: "#05050E" }}
                    >
                      {busy === `post-visibility-${post.id}` ? "Saving" : post.isHidden ? "Unhide" : "Hide"}
                    </button>
                  </div>
                </Card>
              ))}
            </div>
          )}

          {selectedCommunity && tab === "reports" && (
            <div className="space-y-3">
              {reports.length === 0 && <Notice>No open reports for this community.</Notice>}
              {reports.map(report => (
                <Card key={report.id} className="space-y-2">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold flex items-center gap-2" style={{ color: fg }}>
                        <ShieldAlert size={13} style={{ color: gold }} /> {report.reason}
                      </p>
                      <p className="text-[10px] tracking-widest uppercase" style={{ color: `${fg}34` }}>
                        by {report.reportedByUserName} · {new Date(report.createdAt).toLocaleString()}
                      </p>
                    </div>
                    <span className="text-[10px] tracking-widest uppercase" style={{ color: gold }}>{reportStatusName(report.status)}</span>
                  </div>

                  {report.details && <p className="text-xs" style={{ color: `${fg}58` }}>{report.details}</p>}

                  <div className="flex flex-wrap gap-2">
                    <button
                      disabled={busy === `report-dismiss-${report.id}`}
                      onClick={() => void run(
                        `report-dismiss-${report.id}`,
                        () => avraApi.communities.resolveReport(report.id, {
                          status: "Dismissed",
                          resolutionNotes: "Reviewed and dismissed by admin",
                        }),
                        "Report dismissed.",
                      )}
                      className="rounded-full px-3 py-1.5 text-[10px] tracking-widest uppercase border border-white/20"
                    >
                      {busy === `report-dismiss-${report.id}` ? "Saving" : "Dismiss"}
                    </button>
                    <button
                      disabled={busy === `report-action-${report.id}`}
                      onClick={() => void run(
                        `report-action-${report.id}`,
                        () => avraApi.communities.resolveReport(report.id, {
                          status: "Actioned",
                          resolutionNotes: "Action taken by admin",
                        }),
                        "Report resolved as actioned.",
                      )}
                      className="rounded-full px-3 py-1.5 text-[10px] tracking-widest uppercase"
                      style={{ background: goldGrad, color: "#05050E" }}
                    >
                      {busy === `report-action-${report.id}` ? "Saving" : "Actioned"}
                    </button>
                  </div>
                </Card>
              ))}
            </div>
          )}
        </div>
      </div>

      <Card className="flex items-center gap-3">
        <Users size={14} style={{ color: gold }} />
        <p className="text-xs" style={{ color: `${fg}52` }}>
          Community creation and baseline content remain in the Content page; this panel focuses on operations and moderation.
        </p>
      </Card>
    </div>
  );
}
