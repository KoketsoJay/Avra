import { createRoot } from "react-dom/client";
import AdminApp from "@/admin/AdminApp";
import "@/styles/index.css";

document.title = "AVRA Admin";

createRoot(document.getElementById("root")!).render(<AdminApp />);
