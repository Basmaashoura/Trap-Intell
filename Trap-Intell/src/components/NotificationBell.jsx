import { useState, useEffect, useRef, useCallback } from "react";
import { api } from "../services/api";
import styles from "./NotificationBell.module.css";

function timeAgo(dateStr) {
  if (!dateStr) return "—";
  const diff = Math.floor((Date.now() - new Date(dateStr)) / 1000);
  if (diff < 60) return `${diff}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  return `${Math.floor(diff / 86400)}d ago`;
}

const TYPE_COLORS = {
  0: "#3498db",
  1: "#e74c3c",
  2: "#f39c12",
  3: "#8a1bfa",
  4: "#22c55e",
  5: "#aab0c6",
};

export default function NotificationBell() {
  const [open, setOpen] = useState(false);
  const [notifications, setNotifications] = useState([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const panelRef = useRef(null);

  // ── Fetch unread count ─────────────────────────────────────
  const fetchCount = useCallback(async () => {
    try {
      const data = await api.get("/api/notifications/unread-count");
      setUnreadCount(data?.count ?? data?.unreadCount ?? 0);
    } catch {
      // silently fail
    }
  }, []);

  // ── Fetch notifications ────────────────────────────────────
  const fetchNotifications = useCallback(async () => {
    setLoading(true);
    try {
      const data = await api.get("/api/notifications", {
        pageNumber: 1,
        pageSize: 10,
      });
      const items = Array.isArray(data)
        ? data
        : (data.items ?? data.notifications ?? []);
      setNotifications(items);
    } catch {
      setNotifications([]);
    } finally {
      setLoading(false);
    }
  }, []);

  // ── Poll unread count every 30s ────────────────────────────
  useEffect(() => {
    fetchCount();
    const interval = setInterval(fetchCount, 30000);
    return () => clearInterval(interval);
  }, [fetchCount]);

  // ── Fetch on open ──────────────────────────────────────────
  useEffect(() => {
    if (open) fetchNotifications();
  }, [open, fetchNotifications]);

  // ── Close on outside click ─────────────────────────────────
  useEffect(() => {
    function handleClick(e) {
      if (panelRef.current && !panelRef.current.contains(e.target))
        setOpen(false);
    }
    if (open) document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [open]);

  // ── Mark one as read ──────────────────────────────────────
  const markRead = async (id) => {
    try {
      await api.put(`/api/notifications/${id}/read`);
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
      );
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch {
      /* ignore */
    }
  };

  // ── Mark all read ─────────────────────────────────────────
  const markAllRead = async () => {
    try {
      await api.put("/api/notifications/read-all");
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch {
      /* ignore */
    }
  };

  return (
    <div ref={panelRef} style={{ position: "relative" }}>
      {/* Bell button */}
      <button
        onClick={() => setOpen((o) => !o)}
        aria-label="Notifications"
        style={{
          position: "relative",
          background: "none",
          border: "none",
          cursor: "pointer",
          padding: 6,
          borderRadius: 8,
          color: open ? "#4044e4" : "#6b7280",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <svg
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>
        {unreadCount > 0 && (
          <span
            style={{
              position: "absolute",
              top: -4,
              right: -4,
              minWidth: 16,
              height: 16,
              borderRadius: 50,
              background: "#e74c3c",
              color: "#fff",
              fontSize: "0.6rem",
              fontWeight: 800,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              padding: "0 4px",
              lineHeight: 1,
              border: "2px solid #fff",
            }}
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {/* Dropdown panel */}
      {open && (
        <div
          style={{
            position: "absolute",
            top: "calc(100% + 10px)",
            right: 0,

            width: 360,
            maxWidth: "calc(100vw - 20px)",

            background: "#fff",
            borderRadius: 16,
            border: "1.5px solid #e8eaf0",
            boxShadow: "0 8px 32px rgba(0,0,0,0.12)",
            zIndex: 999,
            overflow: "hidden",
            animation: "fadeIn 0.15s ease",
          }}
        >
          {/* Header */}
          <div
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "space-between",
              padding: "16px 18px 12px",
              borderBottom: "1px solid #f0f1f7",
            }}
          >
            <div
              style={{ fontWeight: 800, fontSize: "0.95rem", color: "#1a1f36" }}
            >
              Notifications
              {unreadCount > 0 && (
                <span
                  style={{
                    marginLeft: 8,
                    background: "#e74c3c",
                    color: "#fff",
                    fontSize: "0.65rem",
                    fontWeight: 800,
                    padding: "2px 7px",
                    borderRadius: 50,
                  }}
                >
                  {unreadCount}
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                onClick={markAllRead}
                style={{
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  fontSize: "0.78rem",
                  fontWeight: 700,
                  color: "#4044e4",
                }}
              >
                Mark all read
              </button>
            )}
          </div>

          {/* List */}
          <div style={{ maxHeight: 380, overflowY: "auto" }}>
            {loading ? (
              <div
                style={{
                  padding: "32px 18px",
                  textAlign: "center",
                  color: "#aab0c6",
                  fontSize: "0.85rem",
                }}
              >
                Loading...
              </div>
            ) : notifications.length === 0 ? (
              <div style={{ padding: "40px 18px", textAlign: "center" }}>
                <div style={{ fontSize: 32, marginBottom: 8 }}>🔔</div>
                <div
                  style={{
                    fontSize: "0.85rem",
                    color: "#aab0c6",
                    fontWeight: 500,
                  }}
                >
                  No notifications yet
                </div>
              </div>
            ) : (
              notifications.map((n) => (
                <div
                  key={n.id}
                  onClick={() => !n.isRead && markRead(n.id)}
                  style={{
                    display: "flex",
                    gap: 12,
                    padding: "14px 18px",
                    borderBottom: "1px solid #f4f5fa",
                    cursor: "pointer",
                    background: n.isRead ? "#fff" : "#f8f8ff",
                    transition: "background 0.15s",
                  }}
                  onMouseEnter={(e) =>
                    (e.currentTarget.style.background = "#f4f5fa")
                  }
                  onMouseLeave={(e) =>
                    (e.currentTarget.style.background = n.isRead
                      ? "#fff"
                      : "#f8f8ff")
                  }
                >
                  {/* Dot */}
                  <div
                    style={{
                      width: 8,
                      height: 8,
                      borderRadius: "50%",
                      flexShrink: 0,
                      marginTop: 6,
                      background: n.isRead
                        ? "#e8eaf0"
                        : (TYPE_COLORS[n.type] ?? "#4044e4"),
                    }}
                  />

                  {/* Content */}
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div
                      style={{
                        fontSize: "0.85rem",
                        fontWeight: n.isRead ? 500 : 700,
                        color: "#1a1f36",
                        marginBottom: 3,
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {n.title ?? n.message ?? "Notification"}
                    </div>
                    {n.message && n.title && (
                      <div
                        style={{
                          fontSize: "0.78rem",
                          color: "#6b7280",
                          lineHeight: 1.4,
                          overflow: "hidden",
                          textOverflow: "ellipsis",
                          display: "-webkit-box",
                          WebkitLineClamp: 2,
                          WebkitBoxOrient: "vertical",
                        }}
                      >
                        {n.message}
                      </div>
                    )}
                    <div
                      style={{
                        fontSize: "0.72rem",
                        color: "#aab0c6",
                        marginTop: 4,
                      }}
                    >
                      {timeAgo(n.createdAt ?? n.sentAt)}
                    </div>
                  </div>

                  {/* Unread indicator */}
                  {!n.isRead && (
                    <div
                      style={{
                        width: 6,
                        height: 6,
                        borderRadius: "50%",
                        background: "#4044e4",
                        flexShrink: 0,
                        marginTop: 8,
                      }}
                    />
                  )}
                </div>
              ))
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div
              style={{
                padding: "12px 18px",
                borderTop: "1px solid #f0f1f7",
                textAlign: "center",
              }}
            >
              <button
                style={{
                  background: "none",
                  border: "none",
                  cursor: "pointer",
                  fontSize: "0.82rem",
                  fontWeight: 700,
                  color: "#4044e4",
                }}
              >
                View all notifications
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
