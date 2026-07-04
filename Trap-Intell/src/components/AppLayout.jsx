import { useState } from "react";
import { Outlet } from "react-router-dom";
import Sidebar from "./Sidebar";
import Topbar from "./Topbar";
import styles from "./AppLayout.module.css";
import { useAuth } from "../context/AuthContext";
import ChatBot from "./Chatbot";

export default function AppLayout() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const { subscriptionActive } = useAuth();

  return (
    <div className={styles.shell}>
      <Sidebar open={sidebarOpen} />

      {/* overlay — closes sidebar on mobile when tapped */}
      {sidebarOpen && (
        <div className={styles.overlay} onClick={() => setSidebarOpen(false)} />
      )}

      <div className={styles.main}>
        <Topbar onMenuClick={() => setSidebarOpen((o) => !o)} />
        <main className={styles.content}>
          <Outlet />
        </main>
      </div>
      <ChatBot />
    </div>
  );
}
