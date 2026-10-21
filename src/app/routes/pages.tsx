import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "react-router";
import * as Sections from "@/app/sections";
import {
  avraApi,
  getAvraUser,
  getStoredProfile,
  isApiUnavailable,
  isAuthenticated,
  type CommunityCommentDto,
  type CommunityMemberDto,
  type CommunityMembershipDto,
  type CommunityPostDto,
  type EventTicketValidationResultDto,
  type UserEventTicketDto,
} from "@/app/api/avraApi";
import { useStore } from "@/app/state/StoreContext";

const {
  BrandStory,
  Club,
  Community,
  EditorialStrip,
  Events,
  HeroSection,
  Newsletter,
  Shop,
  Testimonials,
  Wellness,
} = Sections;

/** Standalone pages sit below the fixed navbar; the landing page handles its own spacing. */
function Page({ children }: { children: ReactNode }) {
  return <div className="pt-20">{children}</div>;
}

/** The full scroll narrative, unchanged. */
export function HomePage() {
  const store = useStore();
  const navigate = useNavigate();
  return (
    <>
      <HeroSection onShopClick={() => navigate("/shop")} />
      <BrandStory />
      <Shop products={store.products} discount={store.discount} onAddToCart={store.addToCart} />
      <EditorialStrip />
      <Club tiers={store.membershipTiers} onJoin={store.joinMembership}
        activePlanName={store.membershipPlanName} joiningPlanId={store.joiningPlanId}
        trialEndsAt={store.membershipTrialEndsAt} nextBillingDate={store.membershipNextBillingDate} />
      <Wellness programs={store.programs} />
      <Community communities={store.communities} />
      <Events events={store.events} isMember={Boolean(store.membershipPlanName)} />
      <Testimonials />
      <Newsletter />
    </>
  );
}

export function ShopPage() {
  const store = useStore();
  return <Page><Shop products={store.products} discount={store.discount} onAddToCart={store.addToCart} /></Page>;
}

export function ClubPage() {
  const store = useStore();
  return (
    <Page>
      <Club tiers={store.membershipTiers} onJoin={store.joinMembership}
        activePlanName={store.membershipPlanName} joiningPlanId={store.joiningPlanId}
        trialEndsAt={store.membershipTrialEndsAt} nextBillingDate={store.membershipNextBillingDate} />
    </Page>
  );
}

export function WellnessPage() {
  const store = useStore();
  return <Page><Wellness programs={store.programs} /></Page>;
}

function formatDate(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Now";
  return date.toLocaleString();
}

const COMMUNITY_REPORT_REASONS = [
  "Harassment",
  "Spam",
  "Misinformation",
  "Unsafe advice",
  "Other",
] as const;

export function CommunityPage() {
  const store = useStore();
  const [communities, setCommunities] = useState(store.communities);
  const [selectedCommunityId, setSelectedCommunityId] = useState<string | null>(null);
  const [categoryFilter, setCategoryFilter] = useState("All");
  const [search, setSearch] = useState("");

  const [memberships, setMemberships] = useState<CommunityMembershipDto[]>([]);
  const [members, setMembers] = useState<CommunityMemberDto[]>([]);
  const [posts, setPosts] = useState<CommunityPostDto[]>([]);
  const [commentsByPost, setCommentsByPost] = useState<Record<string, CommunityCommentDto[]>>({});
  const [expandedPostId, setExpandedPostId] = useState<string | null>(null);

  const [postTitle, setPostTitle] = useState("");
  const [postContent, setPostContent] = useState("");
  const [editingPostId, setEditingPostId] = useState<string | null>(null);
  const [commentDrafts, setCommentDrafts] = useState<Record<string, string>>({});

  const [reportingPostId, setReportingPostId] = useState<string | null>(null);
  const [reportReason, setReportReason] = useState<string>(COMMUNITY_REPORT_REASONS[0]);
  const [reportDetails, setReportDetails] = useState("");

  const [newCommunityName, setNewCommunityName] = useState("");
  const [newCommunityCategory, setNewCommunityCategory] = useState("Wellness");
  const [newCommunityDescription, setNewCommunityDescription] = useState("");
  const [newCommunityPublic, setNewCommunityPublic] = useState(true);

  const [busyAction, setBusyAction] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const profile = getStoredProfile();
  const authenticated = isAuthenticated();

  useEffect(() => {
    setCommunities(store.communities);
  }, [store.communities]);

  useEffect(() => {
    if (!selectedCommunityId && communities.length > 0) {
      setSelectedCommunityId(communities[0].id);
      return;
    }

    if (selectedCommunityId && !communities.some(c => c.id === selectedCommunityId)) {
      setSelectedCommunityId(communities[0]?.id ?? null);
    }
  }, [communities, selectedCommunityId]);

  const loadMemberships = async () => {
    if (!authenticated) {
      setMemberships([]);
      return;
    }

    try {
      const rows = await avraApi.communities.myMemberships();
      setMemberships(rows);
    } catch (error) {
      console.warn("Failed to load user memberships", error);
      setMemberships([]);
    }
  };

  const loadCommunityDetails = async (communityId: string) => {
    try {
      const [memberRows, postRows] = await Promise.all([
        avraApi.communities.members(communityId),
        avraApi.communities.posts(communityId),
      ]);
      setMembers(memberRows);
      setPosts(postRows);
      setCommentsByPost({});
      setExpandedPostId(null);
    } catch (error) {
      console.warn("Failed to load community details", error);
      setMembers([]);
      setPosts([]);
      setCommentsByPost({});
      setExpandedPostId(null);
    }
  };

  useEffect(() => {
    void loadMemberships();
  }, [authenticated]);

  useEffect(() => {
    if (!selectedCommunityId) return;
    void loadCommunityDetails(selectedCommunityId);
  }, [selectedCommunityId]);

  useEffect(() => {
    if (!expandedPostId) return;
    void (async () => {
      try {
        const comments = await avraApi.communities.comments(expandedPostId);
        setCommentsByPost(current => ({ ...current, [expandedPostId]: comments }));
      } catch (error) {
        console.warn("Unable to load post comments", error);
      }
    })();
  }, [expandedPostId]);

  const selectedCommunity = useMemo(
    () => communities.find(community => community.id === selectedCommunityId) ?? null,
    [communities, selectedCommunityId],
  );

  const joinedCommunityIds = useMemo(
    () => new Set(memberships.map(membership => membership.communityId)),
    [memberships],
  );

  const visibleCommunities = useMemo(() => {
    const term = search.trim().toLowerCase();
    return communities.filter(community => {
      const categoryMatches = categoryFilter === "All" || community.category === categoryFilter;
      if (!categoryMatches) return false;
      if (!term) return true;
      return `${community.name} ${community.description} ${community.category}`.toLowerCase().includes(term);
    });
  }, [communities, categoryFilter, search]);

  const categories = useMemo(
    () => ["All", ...Array.from(new Set(communities.map(community => community.category)))],
    [communities],
  );

  const selectedIsMember = Boolean(selectedCommunity && joinedCommunityIds.has(selectedCommunity.id));

  const requireAuth = (note: string) => {
    if (authenticated) return true;
    store.openAccount();
    setMessage(note);
    return false;
  };

  const joinCommunity = async (communityId: string) => {
    if (!requireAuth("Sign in to join this community.")) return;

    setBusyAction(`join-${communityId}`);
    setMessage(null);
    try {
      await avraApi.communities.join(communityId);
      await loadMemberships();
      if (selectedCommunityId === communityId) {
        await loadCommunityDetails(communityId);
      }
      setMessage("Joined community successfully.");
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to join community right now.");
    } finally {
      setBusyAction(null);
    }
  };

  const leaveCommunity = async (communityId: string) => {
    if (!requireAuth("Sign in to leave this community.")) return;

    setBusyAction(`leave-${communityId}`);
    setMessage(null);
    try {
      await avraApi.communities.leave(communityId);
      await loadMemberships();
      if (selectedCommunityId === communityId) {
        await loadCommunityDetails(communityId);
      }
      setMessage("You left the community.");
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to leave community right now.");
    } finally {
      setBusyAction(null);
    }
  };

  const submitPost = async () => {
    if (!selectedCommunity || !requireAuth("Sign in to post in the community.")) return;
    if (!selectedIsMember) {
      setMessage("Join this community before posting.");
      return;
    }

    const title = postTitle.trim();
    const content = postContent.trim();
    if (!title || !content) {
      setMessage("A post title and content are required.");
      return;
    }

    setBusyAction(editingPostId ? `edit-post-${editingPostId}` : "create-post");
    setMessage(null);
    try {
      if (editingPostId) {
        await avraApi.communities.updatePost(editingPostId, { title, content });
      } else {
        await avraApi.communities.createPost({ communityId: selectedCommunity.id, title, content });
      }

      setPostTitle("");
      setPostContent("");
      setEditingPostId(null);
      await loadCommunityDetails(selectedCommunity.id);
      setMessage(editingPostId ? "Post updated." : "Post published.");
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to save this post.");
    } finally {
      setBusyAction(null);
    }
  };

  const deletePost = async (postId: string) => {
    if (!selectedCommunity || !requireAuth("Sign in to manage your posts.")) return;

    setBusyAction(`delete-post-${postId}`);
    setMessage(null);
    try {
      await avraApi.communities.deletePost(postId);
      await loadCommunityDetails(selectedCommunity.id);
      setMessage("Post removed.");
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to delete this post.");
    } finally {
      setBusyAction(null);
    }
  };

  const toggleLike = async (postId: string) => {
    if (!selectedCommunity || !requireAuth("Sign in to react to posts.")) return;

    setBusyAction(`like-${postId}`);
    try {
      const updated = await avraApi.communities.toggleLike(postId);
      setPosts(current => current.map(post => post.id === updated.id ? updated : post));
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Could not update your reaction.");
    } finally {
      setBusyAction(null);
    }
  };

  const submitComment = async (postId: string) => {
    if (!selectedCommunity || !requireAuth("Sign in to comment.")) return;
    if (!selectedIsMember) {
      setMessage("Join this community before commenting.");
      return;
    }

    const content = (commentDrafts[postId] ?? "").trim();
    if (!content) {
      setMessage("Comment cannot be empty.");
      return;
    }

    setBusyAction(`comment-${postId}`);
    setMessage(null);
    try {
      await avraApi.communities.createComment({ postId, content });
      const [nextComments, nextPosts] = await Promise.all([
        avraApi.communities.comments(postId),
        avraApi.communities.posts(selectedCommunity.id),
      ]);
      setCommentsByPost(current => ({ ...current, [postId]: nextComments }));
      setPosts(nextPosts);
      setCommentDrafts(current => ({ ...current, [postId]: "" }));
      setExpandedPostId(postId);
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to publish comment.");
    } finally {
      setBusyAction(null);
    }
  };

  const submitReport = async (postId: string) => {
    if (!selectedCommunity || !requireAuth("Sign in to report content.")) return;

    setBusyAction(`report-${postId}`);
    setMessage(null);
    try {
      await avraApi.communities.report({
        communityId: selectedCommunity.id,
        postId,
        reason: reportReason,
        details: reportDetails.trim() || undefined,
      });
      setReportDetails("");
      setReportingPostId(null);
      setMessage("Report submitted. Moderators will review it.");
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to submit report.");
    } finally {
      setBusyAction(null);
    }
  };

  const createCommunity = async () => {
    if (!requireAuth("Sign in to create a community.")) return;
    if (!profile?.id) {
      setMessage("Your profile could not be loaded. Sign in again.");
      return;
    }

    const name = newCommunityName.trim();
    const category = newCommunityCategory.trim();
    const description = newCommunityDescription.trim();

    if (!name || !category || !description) {
      setMessage("Community name, category and description are required.");
      return;
    }

    setBusyAction("create-community");
    setMessage(null);
    try {
      const created = await avraApi.communities.create({
        name,
        category,
        description,
        creatorId: profile.id,
        imageUrl: "avra_2.jpg",
        isPublic: newCommunityPublic,
      });

      const next = [{
        id: created.id,
        name: created.name,
        description: created.description,
        category: created.category,
        memberCount: created.memberCount,
        image: created.imageUrl ?? "",
      }, ...communities];

      setCommunities(next);
      setSelectedCommunityId(created.id);
      setNewCommunityName("");
      setNewCommunityCategory("Wellness");
      setNewCommunityDescription("");
      setNewCommunityPublic(true);
      await loadMemberships();
      setMessage("Community created.");
    } catch (error) {
      setMessage(isApiUnavailable(error) ? "Community service is offline." : "Unable to create community.");
    } finally {
      setBusyAction(null);
    }
  };

  return (
    <Page>
      <section className="max-w-7xl mx-auto px-6 py-8 space-y-6">
        <div className="rounded-3xl border border-white/10 bg-white/5 p-6">
          <p className="text-xs tracking-[0.35em] uppercase text-amber-300/85">Community Hub</p>
          <h1 className="text-3xl mt-2">Build, connect, and grow together</h1>
          <p className="text-sm text-white/65 mt-2 max-w-3xl">
            Join circles, publish updates, start discussions, and report unsafe content.
            Community moderators and administrators review reports from the admin portal.
          </p>

          <div className="mt-5 grid md:grid-cols-[1fr_auto] gap-3">
            <input
              value={search}
              onChange={event => setSearch(event.target.value)}
              placeholder="Search communities"
              className="w-full rounded-2xl px-4 py-3 bg-black/25 border border-white/10 outline-none"
            />
            <button
              disabled={busyAction === "create-community"}
              onClick={createCommunity}
              className="rounded-2xl px-5 py-3 bg-amber-300 text-black text-xs tracking-widest uppercase disabled:opacity-60"
            >
              {busyAction === "create-community" ? "Creating" : "Create Community"}
            </button>
          </div>

          <div className="mt-3 grid md:grid-cols-4 gap-3">
            <input
              value={newCommunityName}
              onChange={event => setNewCommunityName(event.target.value)}
              placeholder="Community name"
              className="rounded-xl px-3 py-2 bg-black/25 border border-white/10 outline-none text-sm"
            />
            <input
              value={newCommunityCategory}
              onChange={event => setNewCommunityCategory(event.target.value)}
              placeholder="Category"
              className="rounded-xl px-3 py-2 bg-black/25 border border-white/10 outline-none text-sm"
            />
            <label className="rounded-xl px-3 py-2 bg-black/25 border border-white/10 text-xs tracking-widest uppercase flex items-center gap-2">
              <input
                type="checkbox"
                checked={newCommunityPublic}
                onChange={event => setNewCommunityPublic(event.target.checked)}
              />
              Public
            </label>
            <div className="text-xs text-white/60 flex items-center">Owner becomes admin automatically.</div>
          </div>

          <textarea
            rows={2}
            value={newCommunityDescription}
            onChange={event => setNewCommunityDescription(event.target.value)}
            placeholder="What is this community about?"
            className="mt-3 w-full rounded-xl px-3 py-2 bg-black/25 border border-white/10 outline-none text-sm resize-none"
          />

          {message && <p className="mt-4 text-xs text-amber-200">{message}</p>}
        </div>

        <div className="flex flex-wrap gap-2">
          {categories.map(category => (
            <button
              key={category}
              onClick={() => setCategoryFilter(category)}
              className="px-3 py-1.5 rounded-full text-[10px] tracking-widest uppercase border"
              style={categoryFilter === category
                ? { borderColor: "rgba(251,191,36,0.9)", color: "rgba(251,191,36,1)" }
                : { borderColor: "rgba(255,255,255,0.14)", color: "rgba(255,255,255,0.7)" }}
            >
              {category}
            </button>
          ))}
        </div>

        <div className="grid lg:grid-cols-[300px_1fr] gap-5">
          <aside className="space-y-3">
            {visibleCommunities.map(community => {
              const selected = selectedCommunityId === community.id;
              const joined = joinedCommunityIds.has(community.id);
              const joining = busyAction === `join-${community.id}` || busyAction === `leave-${community.id}`;

              return (
                <div
                  key={community.id}
                  className="w-full text-left rounded-2xl border p-4 bg-white/5"
                  style={selected ? { borderColor: "rgba(251,191,36,0.65)" } : { borderColor: "rgba(255,255,255,0.1)" }}
                >
                  <button
                    type="button"
                    onClick={() => setSelectedCommunityId(community.id)}
                    className="w-full text-left"
                  >
                    <p className="text-sm font-semibold">{community.name}</p>
                    <p className="text-[10px] mt-1 tracking-widest uppercase text-white/55">
                      {community.category} · {community.memberCount.toLocaleString()} members
                    </p>
                    <p className="text-xs mt-2 text-white/70 line-clamp-2">{community.description}</p>
                  </button>
                  <div className="mt-3 flex gap-2">
                    {!joined && (
                      <button
                        type="button"
                        onClick={() => void joinCommunity(community.id)}
                        className="inline-flex rounded-full px-3 py-1 text-[10px] tracking-widest uppercase bg-amber-300 text-black"
                      >
                        {joining ? "Joining" : "Join"}
                      </button>
                    )}
                    {joined && (
                      <button
                        type="button"
                        onClick={() => void leaveCommunity(community.id)}
                        className="inline-flex rounded-full px-3 py-1 text-[10px] tracking-widest uppercase border border-white/25 text-white/85"
                      >
                        {joining ? "Leaving" : "Leave"}
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
            {visibleCommunities.length === 0 && (
              <div className="rounded-2xl border border-white/10 p-4 text-sm text-white/65">No communities found.</div>
            )}
          </aside>

          <div className="space-y-4">
            {!selectedCommunity && (
              <div className="rounded-2xl border border-white/10 p-5 text-sm text-white/70">Choose a community to see posts.</div>
            )}

            {selectedCommunity && (
              <>
                <div className="rounded-2xl border border-white/10 p-5 bg-white/5">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="text-xl">{selectedCommunity.name}</p>
                      <p className="text-xs tracking-widest uppercase text-white/55 mt-1">
                        {selectedCommunity.category} · {members.length.toLocaleString()} active members
                      </p>
                    </div>
                    <div className="text-xs text-white/65">
                      {selectedIsMember ? "You are a member" : "Join to post, react and comment"}
                    </div>
                  </div>
                  <p className="text-sm text-white/75 mt-3">{selectedCommunity.description}</p>
                </div>

                <div className="rounded-2xl border border-white/10 p-5 bg-white/5 space-y-3">
                  <p className="text-xs tracking-widest uppercase text-amber-300/90">
                    {editingPostId ? "Edit Post" : "Create Post"}
                  </p>
                  <input
                    value={postTitle}
                    onChange={event => setPostTitle(event.target.value)}
                    placeholder="Post title"
                    className="w-full rounded-xl px-3 py-2 bg-black/25 border border-white/10 outline-none text-sm"
                  />
                  <textarea
                    rows={3}
                    value={postContent}
                    onChange={event => setPostContent(event.target.value)}
                    placeholder="What would you like to share?"
                    className="w-full rounded-xl px-3 py-2 bg-black/25 border border-white/10 outline-none text-sm resize-none"
                  />
                  <div className="flex flex-wrap gap-2">
                    <button
                      disabled={busyAction === "create-post" || (editingPostId !== null && busyAction === `edit-post-${editingPostId}`)}
                      onClick={() => void submitPost()}
                      className="rounded-full px-4 py-2 text-[10px] tracking-widest uppercase bg-amber-300 text-black disabled:opacity-60"
                    >
                      {editingPostId
                        ? busyAction === `edit-post-${editingPostId}` ? "Saving" : "Save Post"
                        : busyAction === "create-post" ? "Publishing" : "Publish Post"}
                    </button>
                    {editingPostId && (
                      <button
                        onClick={() => {
                          setEditingPostId(null);
                          setPostTitle("");
                          setPostContent("");
                        }}
                        className="rounded-full px-4 py-2 text-[10px] tracking-widest uppercase border border-white/25"
                      >
                        Cancel
                      </button>
                    )}
                  </div>
                </div>

                <div className="space-y-3">
                  {posts.map(post => {
                    const ownedByCurrentUser = profile?.id === post.userId;
                    const expanded = expandedPostId === post.id;
                    const comments = commentsByPost[post.id] ?? [];

                    return (
                      <article key={post.id} className="rounded-2xl border border-white/10 p-5 bg-white/5 space-y-3">
                        <div className="flex items-start justify-between gap-3">
                          <div>
                            <h3 className="text-lg">{post.title}</h3>
                            <p className="text-[11px] text-white/55 mt-1">
                              {post.userName} · {formatDate(post.createdAt)}
                            </p>
                          </div>
                          {post.isHidden && <span className="text-[10px] tracking-widest uppercase text-amber-300">Hidden</span>}
                        </div>

                        <p className="text-sm text-white/80 whitespace-pre-wrap">{post.content}</p>

                        <div className="flex flex-wrap items-center gap-2 text-[10px] tracking-widest uppercase">
                          <button
                            onClick={() => void toggleLike(post.id)}
                            disabled={busyAction === `like-${post.id}`}
                            className="rounded-full px-3 py-1 border border-white/20"
                          >
                            {post.likedByCurrentUser ? "Liked" : "Like"} ({post.likeCount})
                          </button>
                          <button
                            onClick={() => setExpandedPostId(current => current === post.id ? null : post.id)}
                            className="rounded-full px-3 py-1 border border-white/20"
                          >
                            Comments ({post.commentCount})
                          </button>
                          <button
                            onClick={() => setReportingPostId(current => current === post.id ? null : post.id)}
                            className="rounded-full px-3 py-1 border border-white/20"
                          >
                            Report
                          </button>
                          {ownedByCurrentUser && (
                            <button
                              onClick={() => {
                                setEditingPostId(post.id);
                                setPostTitle(post.title);
                                setPostContent(post.content);
                              }}
                              className="rounded-full px-3 py-1 border border-white/20"
                            >
                              Edit
                            </button>
                          )}
                          {ownedByCurrentUser && (
                            <button
                              onClick={() => void deletePost(post.id)}
                              disabled={busyAction === `delete-post-${post.id}`}
                              className="rounded-full px-3 py-1 border border-red-300/40 text-red-200"
                            >
                              {busyAction === `delete-post-${post.id}` ? "Removing" : "Delete"}
                            </button>
                          )}
                        </div>

                        {reportingPostId === post.id && (
                          <div className="rounded-xl border border-white/10 p-3 bg-black/20 space-y-2">
                            <select
                              value={reportReason}
                              onChange={event => setReportReason(event.target.value)}
                              className="w-full rounded-lg px-3 py-2 bg-black/30 border border-white/10 text-sm"
                            >
                              {COMMUNITY_REPORT_REASONS.map(reason => (
                                <option key={reason} value={reason}>{reason}</option>
                              ))}
                            </select>
                            <textarea
                              rows={2}
                              value={reportDetails}
                              onChange={event => setReportDetails(event.target.value)}
                              placeholder="Optional details"
                              className="w-full rounded-lg px-3 py-2 bg-black/30 border border-white/10 text-sm resize-none"
                            />
                            <button
                              disabled={busyAction === `report-${post.id}`}
                              onClick={() => void submitReport(post.id)}
                              className="rounded-full px-4 py-1.5 text-[10px] tracking-widest uppercase bg-amber-300 text-black"
                            >
                              {busyAction === `report-${post.id}` ? "Sending" : "Submit Report"}
                            </button>
                          </div>
                        )}

                        {expanded && (
                          <div className="rounded-xl border border-white/10 p-3 bg-black/20 space-y-3">
                            {comments.length === 0 && <p className="text-xs text-white/60">No comments yet.</p>}
                            {comments.map(comment => (
                              <div key={comment.id} className="rounded-lg border border-white/10 p-2">
                                <p className="text-xs text-white/60">{comment.userName} · {formatDate(comment.createdAt)}</p>
                                <p className="text-sm text-white/82 mt-1">{comment.content}</p>
                              </div>
                            ))}
                            <div className="flex gap-2">
                              <input
                                value={commentDrafts[post.id] ?? ""}
                                onChange={event => setCommentDrafts(current => ({ ...current, [post.id]: event.target.value }))}
                                placeholder="Add a comment"
                                className="flex-1 rounded-lg px-3 py-2 bg-black/30 border border-white/10 text-sm"
                              />
                              <button
                                disabled={busyAction === `comment-${post.id}`}
                                onClick={() => void submitComment(post.id)}
                                className="rounded-lg px-3 py-2 text-[10px] tracking-widest uppercase bg-amber-300 text-black"
                              >
                                {busyAction === `comment-${post.id}` ? "Posting" : "Post"}
                              </button>
                            </div>
                          </div>
                        )}
                      </article>
                    );
                  })}

                  {posts.length === 0 && (
                    <div className="rounded-2xl border border-white/10 p-5 text-sm text-white/70">
                      No posts yet. Start the first conversation in this community.
                    </div>
                  )}
                </div>
              </>
            )}
          </div>
        </div>
      </section>
    </Page>
  );
}

export function EventsPage() {
  const store = useStore();
  return <Page><Events events={store.events} isMember={Boolean(store.membershipPlanName)} /></Page>;
}

export function TicketsPage() {
  const profile = getStoredProfile();
  const [tickets, setTickets] = useState<UserEventTicketDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [ticketNumber, setTicketNumber] = useState("");
  const [validation, setValidation] = useState<EventTicketValidationResultDto | null>(null);

  const userId = useMemo(() => profile?.id ?? getAvraUser().userId, [profile?.id]);

  const loadTickets = async () => {
    setLoading(true);
    setMessage(null);
    try {
      const rows = await avraApi.events.ticketsByUser(userId);
      setTickets(rows);
    } catch (error) {
      console.warn("Failed to load ticket wallet", error);
      setMessage(isApiUnavailable(error) ? "Events service is offline." : "Could not load your tickets.");
      setTickets([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadTickets();
  }, [userId]);

  const validateTicket = async (event: React.FormEvent) => {
    event.preventDefault();
    setValidation(null);
    setMessage(null);

    if (!ticketNumber.trim()) {
      setMessage("Enter a ticket number first.");
      return;
    }

    try {
      const result = await avraApi.events.validateTicket({ ticketNumber: ticketNumber.trim() });
      setValidation(result);
    } catch (error) {
      console.warn("Ticket validation failed", error);
      setMessage("Could not validate this ticket right now.");
    }
  };

  return (
    <Page>
      <section className="py-14 px-6">
        <div className="max-w-6xl mx-auto space-y-6">
          <div className="flex flex-wrap items-end justify-between gap-3">
            <div>
              <p className="text-[10px] tracking-[0.35em] uppercase text-white/50">07. Ticket Wallet</p>
              <h1 className="text-4xl md:text-5xl italic text-[#F6F3EE]">My Tickets</h1>
            </div>
            <button type="button" onClick={() => void loadTickets()}
              className="rounded-full px-4 py-2 text-[10px] tracking-widest uppercase border border-[#BF9B5E55] text-[#BF9B5E]">
              Refresh
            </button>
          </div>

          <form onSubmit={validateTicket} className="rounded-2xl border border-white/10 bg-[#0f1018] p-4 flex flex-col md:flex-row gap-3 md:items-end">
            <label className="flex-1 text-[10px] tracking-widest uppercase text-white/50">
              Validate Ticket Number
              <input value={ticketNumber} onChange={e => setTicketNumber(e.target.value)} placeholder="AVRA-20260901-ABC12345"
                className="mt-2 w-full rounded-xl px-3 py-2 text-sm bg-[#151725] text-white border border-white/10" />
            </label>
            <button type="submit" className="rounded-full px-5 py-3 text-[10px] tracking-widest uppercase bg-[#BF9B5E] text-[#08070f]">
              Validate QR Ticket
            </button>
          </form>

          {validation && (
            <div className={`rounded-2xl border p-4 text-sm ${validation.isValid ? "border-emerald-400/40 bg-emerald-950/20 text-emerald-100" : "border-rose-400/40 bg-rose-950/20 text-rose-100"}`}>
              <p className="font-semibold">{validation.status}</p>
              <p className="text-xs mt-1">{validation.message}</p>
              {validation.ticket && (
                <p className="text-xs mt-2">Ticket {validation.ticket.ticketNumber} · {validation.ticket.eventTitle}</p>
              )}
            </div>
          )}

          {message && (
            <div className="rounded-2xl border border-white/10 bg-[#0f1018] p-4 text-sm text-white/70">{message}</div>
          )}

          {loading && <div className="text-sm text-white/60">Loading tickets...</div>}

          {!loading && tickets.length === 0 && (
            <div className="rounded-2xl border border-white/10 bg-[#0f1018] p-5 text-sm text-white/70">
              No tickets yet. Buy tickets from the Events page and they will appear here.
            </div>
          )}

          <div className="grid md:grid-cols-2 gap-4">
            {tickets.map(ticket => (
              <article key={ticket.id} className="rounded-2xl border border-white/10 bg-[#0f1018] p-4 flex gap-4">
                <img src={`data:image/png;base64,${ticket.qrCodePngBase64}`} alt={`QR ${ticket.ticketNumber}`}
                  className="w-24 h-24 rounded-lg bg-white p-1" />
                <div className="min-w-0">
                  <p className="text-[10px] tracking-widest uppercase text-white/45">Ticket number</p>
                  <p className="text-xs font-semibold text-white break-all">{ticket.ticketNumber}</p>
                  <p className="mt-2 text-sm text-white/90">{ticket.eventTitle}</p>
                  <p className="text-xs text-white/60">{ticket.eventLocation}</p>
                  <p className="text-xs text-white/50 mt-1">{new Date(ticket.eventStartDate).toLocaleString()}</p>
                </div>
              </article>
            ))}
          </div>
        </div>
      </section>
    </Page>
  );
}

/** /checkout and /orders are linkable URLs that open the existing overlays over the landing page. */
export function CheckoutPage() {
  const { openCheckout } = useStore();
  useEffect(() => { openCheckout(); }, [openCheckout]);
  return <HomePage />;
}

export function OrdersPage() {
  const { openOrders } = useStore();
  useEffect(() => { openOrders(); }, [openOrders]);
  return <HomePage />;
}

