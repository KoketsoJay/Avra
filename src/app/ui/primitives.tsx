import { useCallback, useEffect, useLayoutEffect, useRef } from "react";
import type React from "react";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { G, Shine, bg, fg, gold, goldGrad } from "@/shared/theme";

export { G, Shine, Bracket, bg, bodoni, fg, gold, goldGrad, goldLight, money, shine, shineGold } from "@/shared/theme";

export function Orb({ color = gold, size = 500, style = {} }: { color?:string; size?:number; style?:React.CSSProperties }) {
  return <div className="absolute pointer-events-none rounded-full" style={{ width:size,height:size,opacity:0.07,background:`radial-gradient(circle, ${color}, transparent 65%)`,filter:"blur(64px)",...style }} />;
}

export function SectionNum({ n, label }: { n:string; label:string }) {
  return (
    <div className="flex items-center gap-4 mb-10">
      <span className="text-[11px] tracking-[0.3em]" style={{ color:`${fg}22` }}>{n}.</span>
      <span className="text-[10px] tracking-[0.4em] uppercase" style={{ color:gold }}>{label}</span>
      <div className="flex-1 h-px" style={{ background:`linear-gradient(90deg, rgba(191,155,94,0.25), transparent)` }} />
    </div>
  );
}

export function Grain() {
  return (
    <div className="pointer-events-none absolute inset-0 z-10 opacity-[0.055] mix-blend-overlay"
      style={{ backgroundImage:"url(\"data:image/svg+xml,%3Csvg viewBox='0 0 256 256' xmlns='http://www.w3.org/2000/svg'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='4' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E\")", backgroundSize:"128px 128px" }} />
  );
}

/** Image that wipes in with a clip-path as it scrolls into view. */
export function RevealImg({ src, alt, className = "", style = {}, objectPosition = "center" }:
  { src:string; alt:string; className?:string; style?:React.CSSProperties; objectPosition?:string }) {
  const wrapRef = useRef<HTMLDivElement>(null);
  const imgRef  = useRef<HTMLImageElement>(null);
  useLayoutEffect(() => {
    const wrap = wrapRef.current; const img = imgRef.current;
    if (!wrap || !img) return;
    gsap.set(wrap, { clipPath:"inset(100% 0% 0% 0%)" });
    gsap.set(img,  { scale:1.18 });
    const st = ScrollTrigger.create({
      trigger: wrap, start:"top 88%",
      onEnter: () => {
        gsap.to(wrap, { clipPath:"inset(0% 0% 0% 0%)", duration:1.1, ease:"expo.out" });
        gsap.to(img,  { scale:1, duration:1.4, ease:"expo.out" });
      },
    });
    return () => st.kill();
  }, []);
  return (
    <div ref={wrapRef} className={`overflow-hidden ${className}`} style={{ borderRadius:"inherit", ...style }}>
      <img ref={imgRef} src={src} alt={alt} className="w-full h-full object-cover" style={{ objectPosition, transformOrigin:"center center" }} />
    </div>
  );
}

export function ParallaxImg({ src, alt, strength = 18, className = "", style = {}, objectPosition = "center" }:
  { src:string; alt:string; strength?:number; className?:string; style?:React.CSSProperties; objectPosition?:string }) {
  const wrapRef = useRef<HTMLDivElement>(null);
  const imgRef  = useRef<HTMLImageElement>(null);
  useLayoutEffect(() => {
    if (!wrapRef.current || !imgRef.current) return;
    gsap.set(imgRef.current, { scale:1.18 });
    const st = ScrollTrigger.create({
      trigger: wrapRef.current, start:"top bottom", end:"bottom top", scrub: true,
      onUpdate(self) {
        if (imgRef.current) gsap.set(imgRef.current, { yPercent: -strength + self.progress * strength * 2 });
      },
    });
    return () => st.kill();
  }, [strength]);
  return (
    <div ref={wrapRef} className={`overflow-hidden ${className}`} style={{ borderRadius:"inherit", ...style }}>
      <img ref={imgRef} src={src} alt={alt} className="w-full h-full object-cover" style={{ objectPosition, transformOrigin:"center center" }} />
    </div>
  );
}

export function TiltCard({ children, className = "", style = {}, intensity = 8 }:
  { children:React.ReactNode; className?:string; style?:React.CSSProperties; intensity?:number }) {
  const ref = useRef<HTMLDivElement>(null);
  const onMove = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
    const el = ref.current; if (!el) return;
    const r  = el.getBoundingClientRect();
    const x  = ((e.clientX - r.left) / r.width  - 0.5) * intensity;
    const y  = ((e.clientY - r.top)  / r.height - 0.5) * intensity;
    gsap.to(el, { rotateY:x, rotateX:-y, scale:1.025, duration:0.35, ease:"power2.out", transformPerspective:900 });
  }, [intensity]);
  const onLeave = useCallback(() => {
    gsap.to(ref.current, { rotateX:0, rotateY:0, scale:1, duration:0.55, ease:"elastic.out(1,0.7)" });
  }, []);
  return (
    <div ref={ref} className={className} style={{ willChange:"transform", ...style }}
      onMouseMove={onMove} onMouseLeave={onLeave}>
      {children}
    </div>
  );
}

export function GoldBtn({ children, onClick, full=false, small=false, className="" }:
  { children:React.ReactNode; onClick?:()=>void; full?:boolean; small?:boolean; className?:string }) {
  const ref = useRef<HTMLButtonElement>(null);
  return (
    <button ref={ref} onClick={onClick}
      className={`avra-btn font-semibold tracking-widest uppercase transition-colors ${full?"w-full":"inline-flex items-center"} ${small?"text-[10px] px-5 py-2.5 rounded-full":"text-xs px-8 py-4 rounded-full"} ${className}`}
      style={{ background:goldGrad, color:bg, boxShadow:"0 4px 28px rgba(191,155,94,0.4)" }}>
      {children}
    </button>
  );
}

export function GhostBtn({ children, href, onClick }:
  { children:React.ReactNode; href?:string; onClick?:()=>void }) {
  const cls = "avra-btn font-medium tracking-widest uppercase hover:bg-white/10 relative overflow-hidden text-xs px-8 py-4 rounded-full";
  const sty: React.CSSProperties = { ...G.frosted, color:fg };
  if (href) return <a href={href} className={cls} style={sty}><Shine />{children}</a>;
  return <button onClick={onClick} className={cls} style={sty}><Shine />{children}</button>;
}

/**
 * Reveals elements as they scroll into view. `deps` re-runs it when a section's data arrives,
 * since the elements do not exist on first mount.
 */
export function useReveal(selector:string, vars:gsap.TweenVars = {}, deps:unknown[] = []) {
  useEffect(() => {
    const ctx = gsap.context(() => {
      gsap.utils.toArray<HTMLElement>(selector).forEach(el => {
        gsap.from(el, {
          opacity:0, y:52, duration:0.95, ease:"power3.out",
          scrollTrigger:{ trigger:el, start:"top 88%", toggleActions:"play none none none" },
          ...vars,
        });
      });
    });
    return () => ctx.revert();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);
}
