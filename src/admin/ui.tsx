import type React from "react";
import { AlertTriangle } from "lucide-react";
import { G, Shine, danger, fg, gold } from "@/shared/theme";

export const adminInputClass = "w-full px-3 py-2 rounded-xl text-xs outline-none";
export const adminFieldClass = "w-full px-4 py-3 rounded-2xl text-sm outline-none";

export function Notice({ tone = "info", children }: { tone?: "info" | "error"; children: React.ReactNode }) {
  return (
    <p className="text-xs flex items-center gap-2" style={{ color: tone === "error" ? danger : gold }}>
      {tone === "error" && <AlertTriangle size={12} />}{children}
    </p>
  );
}

export function PageHeader({ title, description }: { title:string; description:string }) {
  return (
    <div className="mb-6">
      <h1 className="text-2xl" style={{ color:fg }}>{title}</h1>
      <p className="text-xs mt-1" style={{ color:`${fg}42` }}>{description}</p>
    </div>
  );
}

export function Card({ children, className = "" }: { children:React.ReactNode; className?:string }) {
  return (
    <div className={`relative rounded-2xl p-4 overflow-hidden ${className}`} style={{ ...G.frosted }}>
      <Shine />
      {children}
    </div>
  );
}

export function FilterButton({ selected, onClick, children }:
  { selected:boolean; onClick:()=>void; children:React.ReactNode }) {
  return (
    <button onClick={onClick} className="px-3 py-2 rounded-full text-[10px] tracking-widest uppercase"
      style={selected ? { ...G.goldHi, color:gold } : { ...G.frosted, color:`${fg}46` }}>
      {children}
    </button>
  );
}
