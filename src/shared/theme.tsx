import type React from "react";

/**
 * Design tokens shared by the storefront and the admin application. Both apps ship as separate
 * bundles, so this module is the single place either one reads brand colours and glass styles from.
 */
export const gold      = "#BF9B5E";
export const goldLight = "#E2CAA0";
export const bg        = "#07070F";
export const fg        = "#F5EDD8";
export const bodoni    = "'Bodoni Moda', serif";
export const goldGrad  = `linear-gradient(135deg, ${gold}, ${goldLight})`;
export const shine     = "linear-gradient(90deg, transparent 5%, rgba(255,255,255,0.18) 50%, transparent 95%)";
export const shineGold = "linear-gradient(90deg, transparent 5%, rgba(191,155,94,0.4) 50%, transparent 95%)";
export const danger    = "#FCA5A5";

export const G = {
  whisper: { background:"rgba(255,255,255,0.03)", backdropFilter:"blur(12px) saturate(1.4)", border:"1px solid rgba(255,255,255,0.06)", boxShadow:"0 2px 16px rgba(0,0,0,0.18), inset 0 1px 0 rgba(255,255,255,0.05)" },
  soft:    { background:"rgba(255,255,255,0.06)", backdropFilter:"blur(20px) saturate(1.6)", border:"1px solid rgba(255,255,255,0.10)", boxShadow:"0 6px 32px rgba(0,0,0,0.25), inset 0 1px 0 rgba(255,255,255,0.09)" },
  frosted: { background:"rgba(255,255,255,0.09)", backdropFilter:"blur(32px) saturate(1.8)", border:"1px solid rgba(255,255,255,0.14)", boxShadow:"0 12px 48px rgba(0,0,0,0.32), inset 0 1px 0 rgba(255,255,255,0.13)" },
  thick:   { background:"rgba(255,255,255,0.12)", backdropFilter:"blur(48px) saturate(2.0)", border:"1px solid rgba(255,255,255,0.18)", boxShadow:"0 20px 64px rgba(0,0,0,0.40), inset 0 1px 0 rgba(255,255,255,0.18)" },
  dark:    { background:"rgba(5,5,14,0.60)",      backdropFilter:"blur(40px) saturate(1.6)", border:"1px solid rgba(255,255,255,0.10)", boxShadow:"0 24px 80px rgba(0,0,0,0.55), inset 0 1px 0 rgba(255,255,255,0.10)" },
  gold:    { background:"rgba(191,155,94,0.10)",  backdropFilter:"blur(28px) saturate(1.6)", border:"1px solid rgba(191,155,94,0.28)", boxShadow:"0 8px 40px rgba(191,155,94,0.14), inset 0 1px 0 rgba(255,224,160,0.16)" },
  goldHi:  { background:"rgba(191,155,94,0.18)",  backdropFilter:"blur(40px) saturate(2.0)", border:"1px solid rgba(191,155,94,0.45)", boxShadow:"0 16px 60px rgba(191,155,94,0.22), inset 0 1px 0 rgba(255,224,160,0.25)" },
} as const;

export function money(value:number) {
  return `R${value.toLocaleString(undefined, { minimumFractionDigits:2, maximumFractionDigits:2 })}`;
}

export function Shine({ gold: g = false }: { gold?: boolean }) {
  return <div className="absolute top-0 left-4 right-4 h-px pointer-events-none" style={{ background: g ? shineGold : shine }} />;
}

export function Bracket({ children, className = "" }: { children:React.ReactNode; className?:string }) {
  return <span className={`inline-flex items-center gap-1.5 ${className}`}><span style={{ color:`${gold}70` }}>[</span>{children}<span style={{ color:`${gold}70` }}>]</span></span>;
}
