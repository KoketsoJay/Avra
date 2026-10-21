import { useCallback, useEffect, useLayoutEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import gsap from "gsap";
import { ScrollTrigger } from "gsap/ScrollTrigger";
import { AnimatePresence, motion } from "motion/react";
import {
  ArrowRight, Calendar, Check, Clock, Facebook, Heart, Instagram, MapPin, Plus,
  Shield, Sparkles, Star, Twitter, Users, Zap,
} from "lucide-react";
import { avraApi, isApiUnavailable, type EventTicketDto } from "@/app/api/avraApi";
import {
  resolveImage,
  type AvraEvent, type CommunityCircle, type MembershipTier, type Product, type WellnessProgram,
} from "@/shared/catalogue";
import { memberUnitPrice, type MemberDiscount } from "@/app/lib/pricing";
import {
  Bracket, G, GhostBtn, GoldBtn, Grain, Orb, ParallaxImg, RevealImg,
  SectionNum, Shine, TiltCard, bg, bodoni, fg, gold, goldGrad, money, shine, useReveal,
} from "@/app/ui/primitives";

import heroImg        from "@/assets/avra/avra_42.jpg";
import lifestyleKitch from "@/assets/avra/avra_44.jpg";
import meditationImg  from "@/assets/avra/avra_1.jpg";
import mindfulImg     from "@/assets/avra/avra_59.jpg";
import selfCareImg    from "@/assets/avra/avra_11.jpg";
import pilatesOcean   from "@/assets/avra/avra_8.jpg";
import farmersMarket  from "@/assets/avra/avra_32.jpg";
import studioYoga     from "@/assets/avra/avra_29.jpg";
import communityGroup from "@/assets/avra/avra_2.jpg";
import beachFeet      from "@/assets/avra/avra_0.jpg";
import tennisImg      from "@/assets/avra/avra_9.jpg";

// â”€â”€â”€ Hero â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
// â”€â”€â”€ Hero video sources (Pexels free licence) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export const HERO_VIDEOS = [
  "https://videos.pexels.com/video-files/5041717/5041717-hd_1080_1920_30fps.mp4", // Black woman yoga â€” portrait
  "https://videos.pexels.com/video-files/8981788/8981788-hd_1920_1080_30fps.mp4", // Woman meditating outdoors â€” fallback
];

function Hero({ onShopClick }:{ onShopClick:()=>void }) {
  const badgeRef = useRef<HTMLDivElement>(null);
  const h1Ref    = useRef<HTMLHeadingElement>(null);
  const subRef   = useRef<HTMLDivElement>(null);
  const paraRef  = useRef<HTMLParagraphElement>(null);
  const btnsRef  = useRef<HTMLDivElement>(null);
  const statsRef = useRef<HTMLDivElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const imgRef   = useRef<HTMLImageElement>(null); // poster fallback

  // Attempt to play video; fall back to Ken Burns on static image if it fails
  useEffect(() => {
    const vid = videoRef.current;
    if (!vid) return;
    vid.play().catch(() => {
      // Video blocked â€” animate the fallback image instead
      if (imgRef.current) {
        gsap.to(imgRef.current, { scale:1.08, duration:18, ease:"none", repeat:-1, yoyo:true });
      }
    });
  }, []);

  useLayoutEffect(() => {
    // Subtle slow zoom on video element itself
    gsap.to(videoRef.current, { scale:1.06, duration:20, ease:"none", repeat:-1, yoyo:true });
    // Content stagger
    const tl = gsap.timeline({ defaults:{ ease:"power3.out" }, delay:0.45 });
    tl.from(badgeRef.current,  { opacity:0, y:-18, duration:0.6 })
      .from(h1Ref.current,     { opacity:0, y:100, duration:1.3, ease:"expo.out" }, "-=0.2")
      .from(subRef.current,    { opacity:0, y:22,  duration:0.7 }, "-=0.7")
      .from(paraRef.current,   { opacity:0, y:28,  duration:0.8 }, "-=0.55")
      .from(btnsRef.current,   { opacity:0, y:22,  duration:0.7 }, "-=0.55")
      .from(statsRef.current?.children ? Array.from(statsRef.current.children):[],
            { opacity:0, x:56, duration:0.65, stagger:0.13 }, "-=0.65");
  }, []);

  return (
    <section className="relative min-h-screen flex items-end overflow-hidden" style={{ paddingBottom:"10vh" }}>
      <div className="absolute inset-0 overflow-hidden">
        {/* â”€â”€ Looping hero video â”€â”€ */}
        <video
          ref={videoRef}
          autoPlay muted loop playsInline
          poster={heroImg}
          className="absolute inset-0 w-full h-full object-cover"
          style={{ objectPosition:"50% 20%", transformOrigin:"center" }}>
          {HERO_VIDEOS.map(src => <source key={src} src={src} type="video/mp4" />)}
        </video>
        {/* Static image shown while video loads / as poster */}
        <img ref={imgRef} src={heroImg} alt="AVRA â€” Become Her"
          className="absolute inset-0 w-full h-full object-cover -z-10"
          style={{ objectPosition:"50% 20%" }} />
        {/* Gradient overlays â€” same as before */}
        <div className="absolute inset-0" style={{ background:"linear-gradient(180deg,rgba(5,5,14,0.10) 0%,rgba(5,5,14,0.02) 28%,rgba(5,5,14,0.55) 65%,rgba(5,5,14,0.94) 100%)" }} />
        <div className="absolute inset-0" style={{ background:"linear-gradient(90deg,rgba(5,5,14,0.72) 0%,rgba(5,5,14,0.08) 55%,transparent 100%)" }} />
        <Grain />
      </div>
      <Orb color={gold} size={700} style={{ top:"-10%",left:"-15%",opacity:0.05 }} />

      <div className="relative z-10 w-full max-w-7xl mx-auto px-6 grid md:grid-cols-5 gap-8 items-end">
        <div className="md:col-span-3 pb-4">
          <div ref={badgeRef} className="inline-flex items-center gap-2 px-4 py-2 rounded-full mb-8 relative overflow-hidden" style={{ ...G.gold }}>
            <Shine gold />
            <Sparkles size={11} style={{ color:gold }} />
            <span className="text-[10px] tracking-[0.35em] uppercase" style={{ color:gold }}>The AVRA Experience</span>
          </div>
          <h1 ref={h1Ref} className="text-[clamp(4.5rem,11vw,9.5rem)] leading-[0.85] mb-4"
            style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
            Become<br /><span style={{ color:gold }}>Her</span>
          </h1>
          <div ref={subRef} className="flex items-center gap-3 mb-6">
            <div className="h-px w-10" style={{ background:`${gold}50` }} />
            <span className="text-[10px] tracking-[0.45em] uppercase" style={{ color:`${fg}38` }}>Women's Wellness Lifestyle Club</span>
          </div>
          <p ref={paraRef} className="text-sm md:text-base leading-relaxed mb-10 max-w-sm" style={{ color:`${fg}58` }}>
            For the woman becoming â€” intentional, holistic, and beautifully alive. A club built for <em>her</em>.
          </p>
          <div ref={btnsRef} className="flex flex-wrap gap-4">
            <GoldBtn onClick={onShopClick}>
              <Bracket><span className="flex items-center gap-2">Shop Collection <ArrowRight size={11}/></span></Bracket>
            </GoldBtn>
            <GhostBtn href="#club">Join the Club</GhostBtn>
          </div>
        </div>

        <div ref={statsRef} className="hidden md:flex md:col-span-2 flex-col gap-3 items-stretch pb-4">
          {[
            { Icon:Users,    val:"5,200+", label:"Women becoming, together" },
            { Icon:Calendar, val:"48",     label:"Retreats, runs & circles"  },
            { Icon:Heart,    val:"30+",    label:"Holistic wellness programmes" },
          ].map(({Icon,val,label}) => (
            <TiltCard key={val} className="relative rounded-2xl overflow-hidden px-5 py-4 flex items-center gap-4" style={{ ...G.thick }}>
              <Shine />
              <div className="w-9 h-9 rounded-xl flex items-center justify-center shrink-0" style={{ background:"rgba(191,155,94,0.18)",border:"1px solid rgba(191,155,94,0.25)" }}>
                <Icon size={15} style={{ color:gold }} />
              </div>
              <div>
                <p className="text-xl leading-none mb-0.5" style={{ fontFamily:bodoni, color:gold }}>{val}</p>
                <p className="text-[10px]" style={{ color:`${fg}42` }}>{label}</p>
              </div>
            </TiltCard>
          ))}
        </div>
      </div>

      {/* Bottom ticker */}
      <div className="absolute bottom-0 left-0 right-0 overflow-hidden" style={{ borderTop:"1px solid rgba(255,255,255,0.05)" }}>
        <div className="avra-ticker flex gap-12 py-3 whitespace-nowrap text-[10px] tracking-[0.4em] uppercase" style={{ color:`${fg}18`, width:"max-content" }}>
          {[...Array(6)].map((_,i) => (
            <span key={i} className="flex items-center gap-12">
              <Bracket>Move</Bracket><span style={{ color:`${gold}35` }}>Â·</span>
              <Bracket>Empower</Bracket><span style={{ color:`${gold}35` }}>Â·</span>
              <Bracket>Become</Bracket><span style={{ color:`${gold}35` }}>Â·</span>
            </span>
          ))}
        </div>
      </div>
    </section>
  );
}

export { Hero };
export const HeroSection = Hero;

// â”€â”€â”€ Brand Story â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function BrandStory() {
  useReveal(".bs-r");
  // Floating card bob
  const floatRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    gsap.to(floatRef.current, { y:-10, duration:2.8, ease:"sine.inOut", yoyo:true, repeat:-1 });
  }, []);
  return (
    <section className="py-32 px-6 relative overflow-hidden">
      <Orb color={gold} size={600} style={{ top:"10%",right:"-20%",opacity:0.05 }} />
      <div className="max-w-7xl mx-auto grid md:grid-cols-2 gap-16 lg:gap-24 items-center">
        <div className="bs-r relative">
          <TiltCard className="rounded-3xl overflow-hidden bg-[#111118]" style={{ height:620 }} intensity={4}>
            <ParallaxImg src={meditationImg} alt="The AVRA Woman" className="w-full h-full" objectPosition="center" />
            <div className="absolute inset-0" style={{ background:"linear-gradient(180deg,transparent 45%,rgba(5,5,14,0.85) 100%)" }} />
          </TiltCard>
          {/* Floating card */}
          <div ref={floatRef} className="absolute bottom-6 left-6 right-6 rounded-2xl overflow-hidden" style={{ ...G.thick }}>
            <Shine />
            <div className="flex items-stretch">
              <img src={mindfulImg} alt="Mindful" className="w-24 h-24 object-cover shrink-0" />
              <div className="p-4">
                <Bracket className="text-[10px] tracking-widest uppercase mb-2 block"><span style={{ color:gold }}>The AVRA Woman</span></Bracket>
                <p className="text-xs leading-relaxed" style={{ color:`${fg}68` }}>"She is becoming. Every step intentional, every choice entirely her own."</p>
              </div>
            </div>
          </div>
        </div>

        <div className="bs-r">
          <SectionNum n="01" label="Our Story" />
          <h2 className="text-5xl md:text-6xl leading-[0.9] mb-8" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
            Stepping<br />Into <span style={{ color:gold }}>Her</span>
          </h2>
          <p className="text-sm leading-relaxed mb-5" style={{ color:`${fg}48` }}>
            Dear Gentle Woman â€” AVRA was born for you. For the woman becoming more intentional with every step, who seeks wellness with somewhere to truly belong. We are not just a brand. We are a movement.
          </p>
          <p className="text-sm leading-relaxed mb-10" style={{ color:`${fg}48` }}>
            Through curated collections, a thriving club, and transformative retreats, we bring women together through intentional and holistic living. <em>Stillness without guilt. Movement with purpose.</em>
          </p>
          <div className="grid grid-cols-2 gap-3 mb-10">
            {[beachFeet, selfCareImg].map((src,i) => (
              <TiltCard key={i} className="relative rounded-2xl overflow-hidden" style={{ height:140 }} intensity={6}>
                <RevealImg src={src} alt="" className="w-full h-full" />
                <div className="absolute inset-0 rounded-2xl" style={{ ...G.whisper }} />
              </TiltCard>
            ))}
          </div>
          <div className="grid grid-cols-3 gap-4">
            {[{v:"5K+",l:"Women"},{v:"48",l:"Events"},{v:"12",l:"Cities"}].map(({v,l}) => (
              <TiltCard key={l} className="relative rounded-2xl p-4 overflow-hidden text-center" style={{ ...G.gold }}>
                <Shine gold />
                <p className="text-3xl mb-1" style={{ fontFamily:bodoni, color:gold }}>{v}</p>
                <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}38` }}>{l}</p>
              </TiltCard>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

// â”€â”€â”€ Product Card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function ProductCard({ p, discount, onAdd }:{ p:Product; discount:MemberDiscount; onAdd:(rect:DOMRect,img:string)=>void }) {
  const [hovered, setHovered] = useState(false);
  const [added, setAdded]     = useState(false);
  const [activeImage, setActiveImage] = useState(0);
  const [showDetails, setShowDetails] = useState(false);
  const btnRef    = useRef<HTMLButtonElement>(null);
  const shineRef  = useRef<HTMLDivElement>(null);
  const gallery = (p.images.length > 0 ? p.images : [p.img1, p.img2]).filter(Boolean);
  const selectedImage = gallery[activeImage] ?? p.img1;
  const selectedImageUrl = resolveImage(selectedImage) ?? "";

  // Shimmer sweep on hover
  useEffect(() => {
    if (!shineRef.current) return;
    if (hovered) {
      gsap.fromTo(shineRef.current,
        { x:"-110%", opacity:1 },
        { x:"110%", duration:0.65, ease:"power2.out" }
      );
    }
  }, [hovered]);

  useEffect(() => {
    setActiveImage(0);
    setShowDetails(false);
  }, [p.id]);

  const handleAdd = () => {
    if (!btnRef.current) return;
    const rect = btnRef.current.getBoundingClientRect();
    onAdd(rect, selectedImageUrl);
    setAdded(true);
    setTimeout(() => setAdded(false), 1500);
  };

  return (
    <TiltCard className="flex-shrink-0 w-[280px] relative rounded-3xl overflow-hidden" style={{ ...G.soft }} intensity={6}>
      <Shine />

      {/* Shimmer stripe */}
      <div ref={shineRef} className="absolute top-0 left-0 w-24 h-full z-20 pointer-events-none skew-x-12"
        style={{ background:"linear-gradient(90deg,transparent,rgba(255,255,255,0.08),transparent)", opacity:0 }} />

      <div className="relative overflow-hidden bg-[#111118]" style={{ height:340 }}
        onMouseEnter={()=>setHovered(true)} onMouseLeave={()=>setHovered(false)}>
        <img src={selectedImageUrl} alt={p.name} className="absolute inset-0 w-full h-full object-cover object-top"
          style={{ transition:"transform 0.65s ease", transform:hovered?"scale(1.08)":"scale(1)" }} />

        {/* Hover overlay */}
        <div className="absolute inset-0 flex items-end p-5 transition-all duration-400"
          style={{ background:hovered?"rgba(5,5,14,0.55)":"transparent", backdropFilter:hovered?"blur(4px)":"none" }}>
          <div style={{ opacity:hovered?1:0, transform:hovered?"translateY(0)":"translateY(18px)", transition:"all 0.32s" }}>
            <button ref={btnRef} onClick={handleAdd}
              className="avra-btn flex items-center gap-2 text-[10px] font-bold tracking-widest uppercase px-5 py-2.5 rounded-full transition-colors"
              style={{ background:added?`rgba(191,155,94,0.95)`:goldGrad, color:bg, boxShadow:"0 4px 20px rgba(191,155,94,0.45)" }}>
              {added ? <><Check size={11}/> Added!</> : <><Plus size={11}/> Add to Bag</>}
            </button>
          </div>
        </div>

        {gallery.length > 1 && (
          <div className="absolute bottom-4 left-4 flex gap-2 z-30">
            {gallery.slice(0, 5).map((imageRef, index) => {
              const thumbnail = resolveImage(imageRef);
              return (
                <button
                  key={`${p.id}-${imageRef}-${index}`}
                  type="button"
                  aria-label={`View ${p.name} image ${index + 1}`}
                  onClick={() => setActiveImage(index)}
                  className="w-10 h-10 rounded-lg overflow-hidden"
                  style={{
                    border: activeImage === index ? `1px solid ${gold}` : "1px solid rgba(255,255,255,0.24)",
                    boxShadow: activeImage === index ? "0 0 0 2px rgba(191,155,94,0.2)" : "none",
                    background: "rgba(5,5,14,0.72)",
                  }}>
                  <img src={thumbnail} alt="" className="w-full h-full object-cover" />
                </button>
              );
            })}
          </div>
        )}

        {p.tag && (
          <div className="absolute top-4 left-4 rounded-full overflow-hidden px-3 py-1" style={{ ...G.goldHi }}>
            <Shine gold />
            <Bracket><span className="text-[9px] font-bold tracking-widest uppercase" style={{ color:gold }}>{p.tag}</span></Bracket>
          </div>
        )}

        {discount.percentage > 0 && (
          <div className="absolute top-4 right-4 rounded-full px-3 py-1" style={{ background:goldGrad, color:bg }}>
            <Bracket><span className="text-[9px] font-bold tracking-widest uppercase">-{discount.percentage}% member</span></Bracket>
          </div>
        )}
      </div>

      <div className="p-5">
        <p className="text-[10px] tracking-widest uppercase mb-1" style={{ color:gold }}>{p.category}</p>
        <h3 className="font-medium text-sm mb-1" style={{ color:fg }}>{p.name}</h3>
        <p className="text-xs leading-relaxed mb-3" style={{ color:`${fg}33` }}>{p.description}</p>
        <button
          type="button"
          onClick={() => setShowDetails(current => !current)}
          className="text-[10px] tracking-widest uppercase mb-3"
          style={{ color:gold }}>
          <Bracket>{showDetails ? "Hide Details" : "More Details"}</Bracket>
        </button>
        <AnimatePresence initial={false}>
          {showDetails && (
            <motion.p
              initial={{ opacity: 0, height: 0 }}
              animate={{ opacity: 1, height: "auto" }}
              exit={{ opacity: 0, height: 0 }}
              transition={{ duration: 0.2 }}
              className="text-xs leading-relaxed mb-3"
              style={{ color:`${fg}50` }}>
              {p.details}
            </motion.p>
          )}
        </AnimatePresence>
        <div className="flex items-center justify-between pt-3" style={{ borderTop:"1px solid rgba(255,255,255,0.06)" }}>
          <span className="flex items-baseline gap-2">
            <span className="text-xl" style={{ fontFamily:bodoni, color:gold }}>R{memberUnitPrice(p.price, discount.percentage).toLocaleString()}</span>
            {discount.percentage > 0 && <span className="text-xs line-through" style={{ color:`${fg}30` }}>R{p.price.toLocaleString()}</span>}
          </span>
          <button onClick={handleAdd} className="text-[10px] tracking-widest uppercase hover:text-[#BF9B5E] flex items-center gap-1 transition-colors" style={{ color:`${fg}28` }}>
            Add <ArrowRight size={10}/>
          </button>
        </div>
      </div>
    </TiltCard>
  );
}

// â”€â”€â”€ Drag Carousel â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function DragCarousel({ children }:{ children:React.ReactNode }) {
  const trackRef   = useRef<HTMLDivElement>(null);
  const isDragging = useRef(false);
  const startX     = useRef(0);
  const scrollLeft = useRef(0);
  const velocity   = useRef(0);
  const lastX      = useRef(0);

  const onDown = (e:React.MouseEvent) => {
    isDragging.current = true;
    startX.current     = e.pageX - (trackRef.current?.offsetLeft??0);
    scrollLeft.current = trackRef.current?.scrollLeft??0;
    lastX.current      = e.pageX;
    if (trackRef.current) trackRef.current.style.cursor = "grabbing";
  };
  const onUp = () => {
    isDragging.current = false;
    if (trackRef.current) {
      trackRef.current.style.cursor = "grab";
      // Momentum
      const momentum = velocity.current * 8;
      gsap.to(trackRef.current, { scrollLeft: trackRef.current.scrollLeft - momentum, duration:0.6, ease:"power2.out" });
    }
  };
  const onMove = (e:React.MouseEvent) => {
    if (!isDragging.current || !trackRef.current) return;
    e.preventDefault();
    const x = e.pageX - trackRef.current.offsetLeft;
    velocity.current = e.pageX - lastX.current;
    lastX.current    = e.pageX;
    trackRef.current.scrollLeft = scrollLeft.current - (x - startX.current) * 1.4;
  };

  return (
    <div>
      <div className="flex items-center gap-3 mb-6 px-6 md:px-0">
        <motion.div animate={{ x:[0,6,0] }} transition={{ repeat:Infinity, duration:1.8, ease:"easeInOut" }}
          className="w-9 h-9 rounded-full flex items-center justify-center" style={{ ...G.gold, border:`1px solid ${gold}40` }}>
          <ArrowRight size={13} style={{ color:gold }} />
        </motion.div>
        <Bracket><span className="text-[10px] tracking-[0.4em] uppercase" style={{ color:`${fg}28` }}>Drag to explore</span></Bracket>
      </div>
      <div ref={trackRef} className="flex gap-5 overflow-x-auto pb-6 select-none"
        style={{ scrollbarWidth:"none", cursor:"grab" }}
        onMouseDown={onDown} onMouseUp={onUp} onMouseLeave={onUp} onMouseMove={onMove}>
        {children}
      </div>
    </div>
  );
}

// â”€â”€â”€ Shop â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Shop({ products, discount, onAddToCart }:{ products:Product[]; discount:MemberDiscount; onAddToCart:(p:Product,rect:DOMRect,img:string)=>void }) {
  const [filter, setFilter] = useState("All");
  const gridRef  = useRef<HTMLDivElement>(null);
  const cats = ["All", ...Array.from(new Set(products.map(p=>p.category)))];
  const visible = filter==="All" ? products : products.filter(p=>p.category===filter);
  useReveal(".shop-hdr");

  useEffect(() => {
    if (!gridRef.current) return;
    gsap.from(gridRef.current.querySelectorAll(".drag-card"), {
      opacity:0, y:40, scale:0.95, duration:0.55, stagger:0.08, ease:"power2.out",
    });
  }, [filter, products]);

  return (
    <section id="shop" className="py-24 relative overflow-hidden">
      <Orb color={gold} size={600} style={{ top:"5%",right:"5%",opacity:0.04 }} />
      <div className="max-w-7xl mx-auto px-6">
        <div className="shop-hdr flex flex-col md:flex-row md:items-end justify-between gap-6 mb-10">
          <div>
            <SectionNum n="02" label="The Collection" />
            <h2 className="text-5xl md:text-6xl leading-[0.9]" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
              Shop <span style={{ color:gold }}>AVRA</span>
            </h2>
          </div>
          <div className="flex flex-wrap gap-2">
            {cats.map(cat => (
              <button key={cat} onClick={()=>setFilter(cat)}
                className="relative px-4 py-1.5 rounded-full text-[10px] tracking-widest uppercase overflow-hidden transition-all"
                style={filter===cat ? { background:goldGrad,color:bg } : { ...G.frosted,color:`${fg}48` }}>
                {filter!==cat && <Shine />}
                {filter===cat ? <Bracket>{cat}</Bracket> : cat}
              </button>
            ))}
          </div>
        </div>
      </div>
      <div className="max-w-7xl mx-auto px-6">
        <DragCarousel>
          <div ref={gridRef} className="flex gap-5">
            {visible.map(p => (
              <div key={p.id} className="drag-card">
                <ProductCard p={p} discount={discount} onAdd={(rect,img)=>onAddToCart(p,rect,img)} />
              </div>
            ))}
          </div>
        </DragCarousel>
      </div>
    </section>
  );
}

// â”€â”€â”€ Editorial Strip â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function EditorialStrip() {
  useReveal(".edt-r");
  return (
    <section className="py-16 px-6 relative overflow-hidden">
      <div className="max-w-7xl mx-auto edt-r">
        <TiltCard className="relative rounded-3xl overflow-hidden" style={{ height:"min(70vh,520px)" }} intensity={3}>
          <div style={{ ...G.soft, position:"absolute", inset:0, borderRadius:"inherit" }}>
            <Shine />
          </div>
          <ParallaxImg src={communityGroup} alt="AVRA women" className="w-full h-full" objectPosition="center 30%" />
          <div className="absolute inset-0" style={{ background:"linear-gradient(135deg,rgba(5,5,14,0.92) 0%,rgba(5,5,14,0.32) 50%,rgba(5,5,14,0.72) 100%)" }} />
          <Grain />
          <div className="absolute inset-0 flex flex-col items-center justify-center text-center px-6">
            <Bracket className="mb-6"><span className="text-[10px] tracking-[0.4em] uppercase" style={{ color:gold }}>Manifesto</span></Bracket>
            <h2 className="text-4xl md:text-6xl lg:text-7xl leading-[0.88] mb-8 max-w-3xl" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
              Our pieces are built to <span style={{ color:gold }}>last</span>,<br />not to be <em>replaced</em>
            </h2>
            <p className="text-sm max-w-sm leading-relaxed mb-8" style={{ color:`${fg}42` }}>
              Intentional, holistic, and beautifully alive. Every collection is designed for the woman who chooses becoming over arriving.
            </p>
            <GhostBtn href="#club">Discover the Club</GhostBtn>
          </div>
        </TiltCard>
      </div>
    </section>
  );
}

// â”€â”€â”€ Club â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Club({ tiers, onJoin, activePlanName, joiningPlanId, trialEndsAt, nextBillingDate }:
  { tiers:MembershipTier[]; onJoin:(tier:MembershipTier)=>void; activePlanName:string|null;
    joiningPlanId:string|null; trialEndsAt:string|null; nextBillingDate:string|null }) {
  useReveal(".club-card", { stagger:0.14 }, [tiers.length]);
  useReveal(".club-hdr");
  return (
    <section id="club" className="py-24 px-6 relative overflow-hidden">
      <Orb color={gold} size={700} style={{ top:"30%",left:"30%",opacity:0.05 }} />
      <div className="max-w-7xl mx-auto relative z-10">
        <div className="club-hdr mb-16">
          <SectionNum n="03" label="Membership" />
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
            <h2 className="text-5xl md:text-6xl leading-[0.9]" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
              The <span style={{ color:gold }}>AVRA</span> Club
            </h2>
            <p className="text-sm max-w-xs leading-relaxed" style={{ color:`${fg}38` }}>
              More than a membership - a community of women becoming together, with intention and grace.
            </p>
          </div>
        </div>
        <div className="grid md:grid-cols-3 gap-5">
          {tiers.map(plan => (
            <TiltCard key={plan.id} className="club-card relative rounded-3xl h-full flex flex-col p-8 overflow-hidden"
              style={plan.highlight ? { ...G.goldHi } : { ...G.frosted }} intensity={5}>
              <Shine gold={plan.highlight} />
              {plan.highlight && (
                <div className="absolute -top-3.5 left-1/2 -translate-x-1/2 px-5 py-1.5 rounded-full whitespace-nowrap"
                  style={{ background:goldGrad,color:bg,boxShadow:"0 4px 24px rgba(191,155,94,0.55)" }}>
                  <Bracket><span className="text-[9px] font-bold tracking-widest uppercase">Most Popular</span></Bracket>
                </div>
              )}
              <Bracket className="text-[10px] tracking-widest uppercase mb-3 block"><span style={{ color:gold }}>{plan.tagline}</span></Bracket>
              <h3 className="text-lg font-medium mb-3" style={{ color:fg }}>{plan.name}</h3>
              <div className="flex items-end gap-1 mb-2">
                <span className="text-5xl" style={{ fontFamily:bodoni, color:fg }}>R{plan.price}</span>
                <span className="text-xs mb-2" style={{ color:`${fg}28` }}>/month</span>
              </div>
              <p className="text-[10px] tracking-widest uppercase mb-6" style={{ color:gold }}>
                {activePlanName === plan.name && trialEndsAt
                  ? `Free until ${new Date(trialEndsAt).toLocaleDateString()}`
                  : activePlanName === plan.name && nextBillingDate
                    ? `Renews ${new Date(nextBillingDate).toLocaleDateString()}`
                    : "First month free"}
              </p>
              <div className="mb-6 h-px" style={{ background:plan.highlight?"rgba(191,155,94,0.3)":"rgba(255,255,255,0.07)" }} />
              <ul className="space-y-3 mb-10 flex-1">
                {plan.features.map(f => (
                  <li key={f} className="flex items-start gap-3 text-sm" style={{ color:`${fg}55` }}>
                    <div className="w-4 h-4 rounded-full flex items-center justify-center mt-0.5 shrink-0"
                      style={{ background:"rgba(191,155,94,0.2)",border:"1px solid rgba(191,155,94,0.3)" }}>
                      <Check size={9} style={{ color:gold }} />
                    </div>{f}
                  </li>
                ))}
              </ul>
              <button onClick={()=>onJoin(plan)} disabled={joiningPlanId !== null || activePlanName === plan.name}
                className="avra-btn relative w-full py-4 rounded-2xl text-xs font-semibold tracking-widest uppercase overflow-hidden transition-all disabled:opacity-70"
                style={plan.highlight ? { background:goldGrad,color:bg,boxShadow:"0 4px 28px rgba(191,155,94,0.45)" } : { ...G.frosted,color:fg }}>
                {!plan.highlight && <Shine />}
                <Bracket>
                  {activePlanName === plan.name ? "Your Plan"
                    : joiningPlanId === plan.id ? "Joining"
                    : activePlanName ? "Switch to This"
                    : "Start Free Month"}
                </Bracket>
              </button>
            </TiltCard>
          ))}
        </div>
      </div>
    </section>
  );
}

// â”€â”€â”€ Wellness â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Wellness({ programs }:{ programs:WellnessProgram[] }) {
  const [enrolledPlanId, setEnrolledPlanId] = useState<string|null>(null);
  useReveal(".well-r", {}, [programs.length]);
  const pillars = [
    { Icon:Heart,    title:"Mind & Stillness",   desc:"Guided meditation, journaling prompts, and mindfulness practices for the woman who craves peace." },
    { Icon:Zap,      title:"Body & Movement",    desc:"Run programs, strength guides, yoga flows, and movement rituals designed for real women." },
    { Icon:Sparkles, title:"Spirit & Community", desc:"Monthly circles, accountability pairings, and group energy that lifts every woman in the room." },
    { Icon:Shield,   title:"Nourishment & Rest", desc:"Whole-food guidance, sleep rituals, and recovery practices to replenish her fully." },
  ];

  const enroll = async (plan: WellnessProgram) => {
    setEnrolledPlanId(plan.id);
    try {
      await avraApi.wellness.enroll(plan.id);
    } catch (error) {
      console.warn("Unable to enroll through WellnessService", error);
    }
  };

  return (
    <section id="wellness" className="py-24 px-6 relative overflow-hidden">
      <Orb color={gold} size={500} style={{ bottom:"0%",left:"-10%",opacity:0.05 }} />
      <div className="max-w-7xl mx-auto grid md:grid-cols-2 gap-16 lg:gap-24 items-center">
        <div className="well-r">
          <SectionNum n="04" label="Wellness Plans" />
          <h2 className="text-5xl md:text-6xl leading-[0.9] mb-8" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
            Your Holistic<br /><span style={{ color:gold }}>Becoming</span>
          </h2>
          <p className="text-sm leading-relaxed mb-10" style={{ color:`${fg}48` }}>
            Stillness without guilt. Movement with purpose. Our wellness plans are designed for the whole woman â€” mind, body, and spirit.
          </p>
          <div className="space-y-3">
            {pillars.map(({Icon,title,desc}) => (
              <TiltCard key={title} className="well-r relative rounded-2xl p-4 flex gap-4 overflow-hidden" style={{ ...G.soft }} intensity={4}>
                <Shine />
                <div className="w-9 h-9 rounded-xl flex items-center justify-center shrink-0"
                  style={{ background:"rgba(191,155,94,0.15)",border:"1px solid rgba(191,155,94,0.22)" }}>
                  <Icon size={15} style={{ color:gold }} />
                </div>
                <div>
                  <p className="text-sm font-medium mb-0.5" style={{ color:fg }}>{title}</p>
                  <p className="text-xs leading-relaxed" style={{ color:`${fg}36` }}>{desc}</p>
                </div>
              </TiltCard>
            ))}
          </div>
        </div>
        <div className="well-r grid grid-cols-2 gap-4">
          <TiltCard className="rounded-3xl overflow-hidden bg-[#111118] relative" style={{ height:460, gridRow:"span 2" }} intensity={3}>
            <RevealImg src={pilatesOcean} alt="Women pilates by ocean" className="w-full h-full" />
            <div className="absolute inset-0" style={{ background:"linear-gradient(180deg,transparent 55%,rgba(5,5,14,0.6) 100%)" }} />
          </TiltCard>
          <TiltCard className="rounded-3xl overflow-hidden bg-[#111118] relative" style={{ height:216 }} intensity={5}>
            <RevealImg src={farmersMarket} alt="Nourishment" className="w-full h-full" />
          </TiltCard>
          <TiltCard className="relative rounded-3xl p-6 flex flex-col justify-center overflow-hidden" style={{ height:216, ...G.goldHi }} intensity={5}>
            <Shine gold />
            <p className="text-4xl mb-1" style={{ fontFamily:bodoni, color:gold }}>{programs.length}</p>
            <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}42` }}>Wellness Programs</p>
          </TiltCard>
          <div className="col-span-2 grid gap-3">
            {programs.slice(0,3).map(plan => (
              <TiltCard key={plan.id} className="well-r relative rounded-2xl p-4 overflow-hidden" style={{ ...G.soft }} intensity={4}>
                <Shine />
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-[10px] tracking-widest uppercase mb-1" style={{ color:gold }}>{plan.category} / {plan.level}</p>
                    <p className="text-sm font-semibold mb-1" style={{ color:fg }}>{plan.name}</p>
                    <p className="text-xs leading-relaxed" style={{ color:`${fg}36` }}>{plan.description}</p>
                  </div>
                  <button onClick={()=>enroll(plan)} className="avra-btn shrink-0 px-4 py-2 rounded-full text-[9px] font-bold tracking-widest uppercase" style={{ background:goldGrad, color:bg }}>
                    {enrolledPlanId === plan.id ? <Bracket>Started</Bracket> : <Bracket>Enroll</Bracket>}
                  </button>
                </div>
                <div className="flex items-center gap-3 mt-3 text-[10px] tracking-widest uppercase" style={{ color:`${fg}34` }}>
                  <span>{plan.durationDays} days</span>
                  <span>R{plan.price.toLocaleString()}</span>
                </div>
              </TiltCard>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
}

// â”€â”€â”€ Community â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Community({ communities }:{ communities:CommunityCircle[] }) {
  const navigate = useNavigate();
  const [joinedCommunityId, setJoinedCommunityId] = useState<string|null>(null);
  const [joiningCommunityId, setJoiningCommunityId] = useState<string|null>(null);
  const [communityMessage, setCommunityMessage] = useState<string|null>(null);
  useReveal(".comm-r", {}, [communities.length]);

  const joinCommunity = async (community?: CommunityCircle) => {
    if (!community) {
      setCommunityMessage("Communities are still loading. Opening the Community Hub.");
      navigate("/community");
      return;
    }

    setJoiningCommunityId(community.id);
    setCommunityMessage(null);
    try {
      await avraApi.communities.join(community.id);
      setJoinedCommunityId(community.id);
      setCommunityMessage(`Joined ${community.name}.`);
    } catch (error) {
      console.warn("Unable to join through CommunityService", error);
      setCommunityMessage(isApiUnavailable(error)
        ? "Community service is offline right now. Please try again shortly."
        : "Could not join this community right now.");
    } finally {
      setJoiningCommunityId(null);
    }
  };

  const featuredCommunity = communities[0];
  const featuredJoining = joiningCommunityId === featuredCommunity?.id;
  const featuredJoined = joinedCommunityId === featuredCommunity?.id;

  return (
    <section id="community" className="py-24 px-6 relative overflow-hidden">
      <div className="absolute inset-0">
        <img src={studioYoga} alt="" className="w-full h-full object-cover" style={{ opacity:0.13 }} />
        <div className="absolute inset-0" style={{ background:`linear-gradient(180deg,${bg} 0%,rgba(5,5,14,0.18) 50%,${bg} 100%)` }} />
      </div>
      <Orb color={gold} size={600} style={{ top:"30%",left:"40%",opacity:0.06 }} />
      <div className="relative z-10 max-w-4xl mx-auto text-center">
        <div className="comm-r">
          <SectionNum n="05" label="Community" />
          <h2 className="text-5xl md:text-7xl leading-[0.88] mb-8" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
            Women Who Are<br />Becoming, <span style={{ color:gold }}>Together</span>
          </h2>
          <p className="text-base leading-relaxed mb-14 max-w-xl mx-auto" style={{ color:`${fg}42` }}>
            A safe, sacred space for women at every stage of their becoming. Connect, share, and grow with women who truly understand the journey.
          </p>
        </div>
        <div className="grid sm:grid-cols-3 gap-4 mb-12">
          {communities.slice(0,3).map((community, index) => {
            const Icon = [Users, Calendar, Star][index] ?? Users;
            return (
            <TiltCard key={community.id} className="comm-r relative rounded-3xl p-7 overflow-hidden" style={{ ...G.thick }} intensity={5}>
              <Shine />
              <div className="w-11 h-11 rounded-2xl flex items-center justify-center mx-auto mb-4"
                style={{ background:"rgba(191,155,94,0.15)",border:"1px solid rgba(191,155,94,0.22)" }}>
                <Icon size={18} style={{ color:gold }} />
              </div>
              <p className="text-sm font-medium mb-1" style={{ color:fg }}>{community.name}</p>
              <p className="text-xs mb-4" style={{ color:`${fg}33` }}>{community.memberCount.toLocaleString()} members / {community.category}</p>
              <button onClick={()=>void joinCommunity(community)} className="avra-btn text-[9px] font-bold tracking-widest uppercase px-4 py-2 rounded-full" style={{ background:joinedCommunityId===community.id?"rgba(255,255,255,0.10)":goldGrad, color:joinedCommunityId===community.id?fg:bg }}>
                <Bracket>
                  {joiningCommunityId===community.id ? "Joining" : joinedCommunityId===community.id ? "Joined" : "Join"}
                </Bracket>
              </button>
            </TiltCard>
          );})}
        </div>
        <div className="grid grid-cols-2 gap-4 mb-12">
          {communities.slice(0,2).map((community) => (
            <TiltCard key={community.id} className="comm-r relative rounded-3xl overflow-hidden bg-[#111118]" style={{ height:220 }} intensity={4}>
              <RevealImg src={resolveImage(community.image) ?? ""} alt={community.name} className="w-full h-full" objectPosition="center top" />
              <div className="absolute inset-0 rounded-3xl" style={{ ...G.whisper }} />
              <div className="absolute top-0 left-4 right-4 h-px" style={{ background:shine }} />
            </TiltCard>
          ))}
        </div>
        <div className="comm-r space-y-3">
          <GoldBtn onClick={()=>void joinCommunity(featuredCommunity)}>
            <Bracket>
              <span className="flex items-center gap-2">
                {featuredCommunity
                  ? (featuredJoining ? "Joining" : featuredJoined ? "Joined" : "Join the Community")
                  : "Open Community Hub"}
                <ArrowRight size={11}/>
              </span>
            </Bracket>
          </GoldBtn>
          {communityMessage && (
            <p className="text-xs" style={{ color: `${fg}60` }}>{communityMessage}</p>
          )}
        </div>
      </div>
    </section>
  );
}

// â”€â”€â”€ Events â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Events({ events, isMember }:{ events:AvraEvent[]; isMember:boolean }) {
  const [registeredEventId, setRegisteredEventId] = useState<string|null>(null);
  const [openEventId, setOpenEventId] = useState<string|null>(null);
  const [checkoutEventId, setCheckoutEventId] = useState<string|null>(null);
  const [ticketCount, setTicketCount] = useState(1);
  const [cardLast4, setCardLast4] = useState("");
  const [processingPurchaseEventId, setProcessingPurchaseEventId] = useState<string|null>(null);
  const [issuedTicketsByEvent, setIssuedTicketsByEvent] = useState<Record<string, EventTicketDto[]>>({});
  const [eventMessage, setEventMessage] = useState<string|null>(null);
  useReveal(".evt-r", { stagger:0.12 }, [events.length]);
  useReveal(".evt-hdr");

  const getApiMessage = (error: unknown, fallback: string) => {
    if (!(error instanceof Error)) return fallback;
    try {
      const parsed = JSON.parse(error.message) as { message?: string };
      return parsed.message || fallback;
    } catch {
      return error.message || fallback;
    }
  };

  const rsvp = async (event: AvraEvent) => {
    if (event.membersOnly && !isMember) {
      setEventMessage("This event is for members only. Join the AVRA Club to unlock access.");
      return;
    }

    setRegisteredEventId(event.id);
    setEventMessage(null);
    try {
      await avraApi.events.register(event.id, 1, isMember);
      setEventMessage("Your RSVP has been confirmed.");
    } catch (error) {
      console.warn("Unable to register through EventsService", error);
      const detail = getApiMessage(error, "Could not complete registration right now.");
      setEventMessage(detail.includes("members only")
        ? "This event is reserved for members only."
        : detail);
    }
  };

  const beginCheckout = (event: AvraEvent) => {
    if (event.membersOnly && !isMember) {
      setEventMessage("This event is for members only. Join the AVRA Club to unlock access.");
      return;
    }

    setOpenEventId(event.id);
    setCheckoutEventId(current => current === event.id ? null : event.id);
    setTicketCount(1);
    setCardLast4("");
    setEventMessage(null);
  };

  const completePurchase = async (event: AvraEvent) => {
    if (processingPurchaseEventId === event.id) {
      return;
    }

    if (event.price === "Free") {
      await rsvp(event);
      return;
    }

    if (ticketCount <= 0) {
      setEventMessage("Please choose at least one ticket.");
      return;
    }

    if (!/^\d{4}$/.test(cardLast4.trim())) {
      setEventMessage("Enter the last 4 digits of your card.");
      return;
    }

    setProcessingPurchaseEventId(event.id);
    setEventMessage(null);

    try {
      const purchase = await avraApi.events.purchase(event.id, {
        numberOfTickets: ticketCount,
        paymentMethod: "Card",
        cardLast4: cardLast4.trim(),
      });

      setIssuedTicketsByEvent(current => ({ ...current, [event.id]: purchase.tickets }));
      setRegisteredEventId(event.id);
      setCheckoutEventId(null);
      setEventMessage(`Payment successful. ${purchase.tickets.length} ticket(s) issued with QR code.`);
    } catch (error) {
      console.warn("Unable to purchase tickets through EventsService", error);
      setEventMessage(getApiMessage(error, "Could not complete payment right now."));
    } finally {
      setProcessingPurchaseEventId(null);
    }
  };

  const toggleDetails = (eventId: string) => {
    setOpenEventId(current => current === eventId ? null : eventId);
  };

  return (
    <section id="events" className="py-24 px-6 relative overflow-hidden">
      <Orb color={gold} size={500} style={{ top:"-5%",right:"-10%",opacity:0.04 }} />
      <div className="max-w-7xl mx-auto">
        <div className="evt-hdr mb-16">
          <SectionNum n="06" label="Events & Retreats" />
          <h2 className="text-5xl md:text-6xl leading-[0.9]" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>
            Experience <span style={{ color:gold }}>AVRA</span>
          </h2>
          {eventMessage && <p className="text-xs mt-4" style={{ color:`${fg}58` }}>{eventMessage}</p>}
        </div>
        <div className="grid md:grid-cols-2 gap-5">
          {events.map(ev => (
            <TiltCard key={ev.id} className="evt-r relative rounded-3xl overflow-hidden" style={{ ...G.soft }} intensity={4}>
              <Shine />
              <div className="relative overflow-hidden bg-[#111118]" style={{ height:240 }}>
                <ParallaxImg src={resolveImage(ev.image) ?? ""} alt={ev.title} className="w-full h-full" strength={10} />
                <div className="absolute inset-0" style={{ background:"linear-gradient(180deg,transparent 35%,rgba(5,5,14,0.88) 100%)" }} />
                <div className="absolute top-4 right-4 rounded-full overflow-hidden px-3 py-1" style={{ ...G.goldHi }}>
                  <Shine gold />
                  <Bracket><span className="text-[9px] font-bold tracking-widest uppercase" style={{ color:gold }}>{ev.category}</span></Bracket>
                </div>
                {ev.membersOnly && (
                  <div className="absolute top-14 right-4 rounded-full overflow-hidden px-3 py-1" style={{ ...G.frosted }}>
                    <Bracket><span className="text-[9px] font-bold tracking-widest uppercase" style={{ color:fg }}>Members only</span></Bracket>
                  </div>
                )}
              </div>
              <div className="p-6">
                <h3 className="text-base font-semibold mb-4" style={{ color:fg }}>{ev.title}</h3>
                <div className="space-y-2 mb-5">
                  {[{Icon:Calendar,t:ev.date},{Icon:Clock,t:ev.time},{Icon:MapPin,t:ev.location}].map(({Icon,t}) => (
                    <div key={t} className="flex items-center gap-2 text-xs" style={{ color:`${fg}38` }}>
                      <Icon size={10} style={{ color:gold }} />{t}
                    </div>
                  ))}
                </div>
                <div className="flex items-center justify-between gap-3 pt-4" style={{ borderTop:"1px solid rgba(255,255,255,0.07)" }}>
                  <span className="text-2xl" style={{ fontFamily:bodoni, color:gold }}>
                    {ev.price==="Free" ? <Bracket>Free</Bracket> : `R${(ev.price as number).toLocaleString()}`}
                  </span>
                  <div className="flex items-center gap-2">
                    <button type="button" onClick={()=>toggleDetails(ev.id)}
                      className="avra-btn text-[10px] px-4 py-2 rounded-full tracking-widest uppercase"
                      style={{ ...G.frosted, color:fg }}>
                      <Bracket>{openEventId===ev.id ? "Hide Details" : "View Details"}</Bracket>
                    </button>
                    <GoldBtn small onClick={()=>void (ev.price === "Free" ? rsvp(ev) : beginCheckout(ev))}>
                      <Bracket>
                        {registeredEventId===ev.id
                          ? (ev.price === "Free" ? "RSVPed" : "Buy More")
                          : (ev.price === "Free" ? "RSVP" : "Buy Tickets")}
                      </Bracket>
                    </GoldBtn>
                  </div>
                </div>
                {openEventId === ev.id && (
                  <div className="mt-4 rounded-2xl p-4 text-xs leading-relaxed" style={{ ...G.frosted, color:`${fg}66` }}>
                    <p>{ev.description}</p>
                    {ev.membersOnly && !isMember && (
                      <p className="mt-2" style={{ color:gold }}>Join the AVRA Club to buy tickets for this members-only event.</p>
                    )}
                    {checkoutEventId === ev.id && ev.price !== "Free" && (
                      <div className="mt-4 rounded-2xl p-4" style={{ ...G.soft }}>
                        <p className="text-[10px] tracking-widest uppercase mb-3" style={{ color:`${fg}46` }}>Secure Checkout</p>
                        <div className="grid grid-cols-2 gap-3">
                          <label className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>
                            Tickets
                            <input type="number" min={1} value={ticketCount} onChange={event => setTicketCount(Number(event.target.value) || 1)}
                              className="mt-2 w-full rounded-xl px-3 py-2 text-xs"
                              style={{ ...G.frosted, color:fg }} />
                          </label>
                          <label className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>
                            Card last 4
                            <input maxLength={4} value={cardLast4} onChange={event => setCardLast4(event.target.value.replace(/\D/g, "").slice(0, 4))}
                              className="mt-2 w-full rounded-xl px-3 py-2 text-xs"
                              style={{ ...G.frosted, color:fg }} placeholder="1234" />
                          </label>
                        </div>
                        <div className="mt-3 flex items-center justify-between">
                          <span className="text-sm" style={{ color:gold }}>
                            Total {money((ev.price as number) * ticketCount)}
                          </span>
                          <GoldBtn small onClick={()=>void completePurchase(ev)} className="disabled:opacity-60"
                            >
                            <Bracket>{processingPurchaseEventId === ev.id ? "Processing" : "Pay & Issue Ticket"}</Bracket>
                          </GoldBtn>
                        </div>
                      </div>
                    )}
                    {(issuedTicketsByEvent[ev.id]?.length ?? 0) > 0 && (
                      <div className="mt-4 grid gap-3">
                        {issuedTicketsByEvent[ev.id].map(ticket => (
                          <div key={ticket.id} className="rounded-2xl p-3 flex items-center gap-3" style={{ ...G.soft }}>
                            <img src={`data:image/png;base64,${ticket.qrCodePngBase64}`} alt={`QR ticket ${ticket.ticketNumber}`}
                              className="w-20 h-20 rounded-lg bg-white p-1" />
                            <div>
                              <p className="text-[10px] tracking-widest uppercase" style={{ color:`${fg}46` }}>Ticket issued</p>
                              <p className="text-xs font-semibold" style={{ color:fg }}>{ticket.ticketNumber}</p>
                              <p className="text-[10px]" style={{ color:`${fg}46` }}>{new Date(ticket.issuedAt).toLocaleString()}</p>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                )}
              </div>
            </TiltCard>
          ))}
        </div>
      </div>
    </section>
  );
}

// â”€â”€â”€ Testimonials â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Testimonials() {
  useReveal(".testi-r", { stagger:0.12 });
  useReveal(".testi-hdr");
  const TESTI = [
    { name:"Monique R.", role:"AVRA Elite Member",  avatar:pilatesOcean,   text:"AVRA changed how I see myself. The community holds you accountable without judgment. I am truly becoming." },
    { name:"Destiny K.", role:"AVRA Club Member",   avatar:studioYoga,     text:"From the run series to the wellness guides â€” AVRA meets you exactly where you are. I feel seen." },
    { name:"Simone A.",  role:"AVRA Way Member",    avatar:lifestyleKitch, text:"The Stillness Retreat was life-altering. I left with tools and a tribe. This is more than a brand." },
  ];
  return (
    <section className="py-24 px-6 relative overflow-hidden">
      <Orb color={gold} size={400} style={{ bottom:"0%",right:"5%",opacity:0.04 }} />
      <div className="max-w-7xl mx-auto">
        <div className="testi-hdr mb-14">
          <SectionNum n="07" label="Her Words" />
          <h2 className="text-5xl leading-[0.9]" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>What <span style={{ color:gold }}>She</span> Says</h2>
        </div>
        <div className="grid md:grid-cols-3 gap-5">
          {TESTI.map(t => (
            <TiltCard key={t.name} className="testi-r relative rounded-3xl p-7 flex flex-col overflow-hidden h-full" style={{ ...G.frosted }} intensity={5}>
              <Shine />
              <div className="flex gap-1 mb-5">{[...Array(5)].map((_,k)=><Star key={k} size={12} fill={gold} style={{ color:gold }}/>)}</div>
              <p className="text-sm leading-relaxed flex-1 mb-6" style={{ color:`${fg}58` }}>"{t.text}"</p>
              <div className="flex items-center gap-3 pt-5" style={{ borderTop:"1px solid rgba(255,255,255,0.07)" }}>
                <img src={t.avatar} alt={t.name} className="w-10 h-10 rounded-full object-cover object-top bg-[#111118]"
                  style={{ border:"1px solid rgba(191,155,94,0.32)" }} />
                <div>
                  <p className="text-sm font-semibold" style={{ color:fg }}>{t.name}</p>
                  <p className="text-[10px] tracking-wider" style={{ color:`${fg}33` }}>{t.role}</p>
                </div>
              </div>
            </TiltCard>
          ))}
        </div>
      </div>
    </section>
  );
}

// â”€â”€â”€ Newsletter â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Newsletter() {
  const [email, setEmail] = useState("");
  const [done,  setDone]  = useState(false);
  useReveal(".nl-r");
  return (
    <section className="py-20 px-6">
      <div className="max-w-2xl mx-auto nl-r">
        <TiltCard className="relative rounded-3xl p-12 overflow-hidden text-center" style={{ ...G.goldHi }} intensity={3}>
          <Shine gold />
          <div className="absolute top-0 inset-x-0 h-48 pointer-events-none" style={{ background:"radial-gradient(ellipse at 50% 0%,rgba(191,155,94,0.14),transparent 70%)" }} />
          <Bracket className="mb-4 block text-[10px] tracking-[0.4em] uppercase"><span style={{ color:gold }}>Stay Close</span></Bracket>
          <h2 className="text-4xl mb-4 leading-[0.9]" style={{ fontFamily:bodoni, fontStyle:"italic", color:fg }}>Join the Journey</h2>
          <p className="text-sm mb-8 leading-relaxed" style={{ color:`${fg}48` }}>
            Receive wellness letters, early collection access, and invitations to exclusive AVRA events.
          </p>
          <AnimatePresence mode="wait">
            {done
              ? <motion.div key="ok" initial={{opacity:0,y:12}} animate={{opacity:1,y:0}}
                  className="flex items-center justify-center gap-2 text-sm" style={{ color:gold }}>
                  <Check size={14}/> <Bracket>Welcome to AVRA. We are glad you are here.</Bracket>
                </motion.div>
              : <motion.form key="form" initial={{opacity:0}} animate={{opacity:1}}
                  onSubmit={e=>{e.preventDefault();if(email)setDone(true);}}
                  className="flex gap-3 max-w-md mx-auto">
                  <input type="email" value={email} onChange={e=>setEmail(e.target.value)}
                    placeholder="Your email address" className="flex-1 px-5 py-3 rounded-full text-sm outline-none"
                    style={{ ...G.frosted, color:fg }} />
                  <GoldBtn onClick={()=>{}}><Bracket>Subscribe</Bracket></GoldBtn>
                </motion.form>
            }
          </AnimatePresence>
        </TiltCard>
      </div>
    </section>
  );
}

// â”€â”€â”€ Footer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
export function Footer() {
  useReveal(".foot-r");
  const cols = [
    { h:"Navigate", links:["Shop","The AVRA Club","Wellness Plans","Events","Community"] },
    { h:"Company",  links:["Our Story","Press","Careers","Contact","Privacy Policy"] },
  ];
  return (
    <footer className="pt-16 pb-10 px-6" style={{ borderTop:"1px solid rgba(255,255,255,0.05)" }}>
      <div className="max-w-7xl mx-auto foot-r">
        <div className="grid md:grid-cols-4 gap-12 mb-14">
          <div className="md:col-span-2">
            <p className="text-4xl tracking-[0.5em] mb-2" style={{ fontFamily:bodoni, fontStyle:"italic", color:gold }}>AVRA</p>
            <Bracket className="text-[10px] tracking-widest uppercase mb-4 block"><span style={{ color:`${fg}22` }}>Move. Empower. Become.</span></Bracket>
            <p className="text-sm leading-relaxed max-w-xs" style={{ color:`${fg}28` }}>
              A wellness lifestyle club for women who are becoming â€” intentional, holistic, and beautifully alive.
            </p>
          </div>
          {cols.map(col => (
            <div key={col.h}>
              <Bracket className="text-[10px] tracking-widest uppercase mb-5 block"><span style={{ color:gold }}>{col.h}</span></Bracket>
              <ul className="space-y-3">
                {col.links.map(l => <li key={l}><a href="#" className="text-sm hover:text-[#F5EDD8] transition-colors" style={{ color:`${fg}28` }}>{l}</a></li>)}
              </ul>
            </div>
          ))}
        </div>
        <div className="relative rounded-2xl px-6 py-4 overflow-hidden flex flex-col sm:flex-row items-center justify-between gap-4" style={{ ...G.whisper }}>
          <Shine />
          <Bracket><span className="text-xs tracking-wider" style={{ color:`${fg}20` }}>Â©2025_AVRA</span></Bracket>
          <div className="flex gap-3">
            {[Instagram,Twitter,Facebook].map((Icon,i) => (
              <a key={i} href="#" className="relative w-9 h-9 rounded-full flex items-center justify-center overflow-hidden hover:scale-110 transition-transform"
                style={{ ...G.frosted, color:`${fg}38` }}>
                <Shine /><Icon size={13}/>
              </a>
            ))}
          </div>
        </div>
      </div>
    </footer>
  );
}

