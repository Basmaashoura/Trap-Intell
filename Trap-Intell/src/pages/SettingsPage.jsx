import { useState, useEffect, useCallback } from "react";
import { useAuth } from "../context/AuthContext";
import { api } from "../services/api";
import styles from "./SettingsPage.module.css";

// ── Tab definitions ────────────────────────────────────────────
const TABS = [
  {
    id: "profile",
    label: "My Profile",
    icon: (
      <svg
        width="17"
        height="17"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <circle cx="12" cy="8" r="4" />
        <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" />
      </svg>
    ),
  },
  {
    id: "organization",
    label: "Organization",
    icon: (
      <svg
        width="17"
        height="17"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <rect x="2" y="7" width="20" height="14" rx="2" />
        <path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2" />
      </svg>
    ),
  },
  {
    id: "security",
    label: "Security",
    icon: (
      <svg
        width="17"
        height="17"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
      </svg>
    ),
  },
  {
    id: "notifications",
    label: "Notifications",
    icon: (
      <svg
        width="17"
        height="17"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" />
        <path d="M13.73 21a2 2 0 0 1-3.46 0" />
      </svg>
    ),
  },
  {
    id: "billing",
    label: "Billing",
    icon: (
      <svg
        width="17"
        height="17"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <rect x="1" y="4" width="22" height="16" rx="2" />
        <line x1="1" y1="10" x2="23" y2="10" />
      </svg>
    ),
  },
  {
    id: "api",
    label: "API Keys",
    icon: (
      <svg
        width="17"
        height="17"
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4" />
      </svg>
    ),
  },
];

// ── Reusable field ─────────────────────────────────────────────
function Field({ label, children, hint }) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
      <label
        style={{
          fontSize: "0.78rem",
          fontWeight: 600,
          color: "#555770",
          letterSpacing: "0.3px",
        }}
      >
        {label}
      </label>
      {children}
      {hint && (
        <p style={{ margin: 0, fontSize: "0.74rem", color: "#aab0c6" }}>
          {hint}
        </p>
      )}
    </div>
  );
}

function Input({ readOnly, ...props }) {
  return (
    <input
      {...props}
      readOnly={readOnly}
      style={{
        border: "1.5px solid #e8eaf0",
        borderRadius: 10,
        padding: "12px 14px",
        fontFamily: "Plus Jakarta Sans, sans-serif",
        fontSize: "0.9rem",
        color: readOnly ? "#aab0c6" : "#111326",
        background: readOnly ? "#f4f5fa" : "#fff",
        outline: "none",
        width: "100%",
        boxSizing: "border-box",
        cursor: readOnly ? "not-allowed" : "text",
        transition: "border-color 0.2s, box-shadow 0.2s",
        ...props.style,
      }}
      onFocus={(e) => {
        if (!readOnly) {
          e.target.style.borderColor = "#4044e4";
          e.target.style.boxShadow = "0 0 0 3px rgba(64,68,228,0.1)";
        }
      }}
      onBlur={(e) => {
        e.target.style.borderColor = "#e8eaf0";
        e.target.style.boxShadow = "none";
      }}
    />
  );
}

// ── Toast ──────────────────────────────────────────────────────
function useToast() {
  const [toast, setToast] = useState({ show: false, msg: "", type: "success" });
  const show = (msg, type = "success") => {
    setToast({ show: true, msg, type });
    setTimeout(() => setToast((t) => ({ ...t, show: false })), 3000);
  };
  return { toast, show };
}

// ── Coming soon placeholder ────────────────────────────────────
function ComingSoon({ title, description, icon }) {
  return (
    <div
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        minHeight: 360,
        gap: 14,
        color: "#555770",
        textAlign: "center",
      }}
    >
      <div style={{ opacity: 0.2 }}>{icon}</div>
      <h3
        style={{
          fontSize: "1rem",
          fontWeight: 700,
          color: "#111326",
          opacity: 0.5,
        }}
      >
        {title}
      </h3>
      <p style={{ fontSize: "0.85rem", margin: 0 }}>{description}</p>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════
// MAIN COMPONENT
// ══════════════════════════════════════════════════════════════
export default function SettingsPage() {
  const { user, orgId } = useAuth();
  const search = new URLSearchParams(window.location.search);

  const [activeTab, setActiveTab] = useState(search.get("tab") || "profile");
  const { toast, show: showToast } = useToast();

  return (
    <div>
      {/* Page header */}
      <div style={{ marginBottom: 28 }}>
        <h1
          style={{
            margin: "0 0 4px",
            fontSize: "1.5rem",
            fontWeight: 800,
            color: "#111326",
            textTransform: "uppercase",
            letterSpacing: "1px",
          }}
        >
          Settings
        </h1>
        <p style={{ margin: 0, fontSize: "0.9rem", color: "#555770" }}>
          Manage your account, organization, and system preferences.
        </p>
      </div>

      {/* Settings layout */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "220px 1fr",
          gap: 24,
          alignItems: "start",
        }}
      >
        {/* Left nav */}
        <nav style={{ display: "flex", flexDirection: "column", gap: 2 }}>
          {TABS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 12,
                padding: "11px 14px",
                borderRadius: 12,
                cursor: "pointer",
                border: "none",
                width: "100%",
                textAlign: "left",
                fontFamily: "Plus Jakarta Sans, sans-serif",
                fontSize: "0.88rem",
                fontWeight: 600,
                background:
                  activeTab === tab.id ? "rgba(64,68,228,0.08)" : "none",
                color: activeTab === tab.id ? "#4044e4" : "#555770",
                transition: "background 0.15s, color 0.15s",
              }}
              onMouseEnter={(e) => {
                if (activeTab !== tab.id)
                  e.currentTarget.style.background = "#f4f5fa";
              }}
              onMouseLeave={(e) => {
                if (activeTab !== tab.id)
                  e.currentTarget.style.background = "none";
              }}
            >
              <span
                style={{
                  opacity: activeTab === tab.id ? 1 : 0.55,
                  flexShrink: 0,
                }}
              >
                {tab.icon}
              </span>
              {tab.label}
            </button>
          ))}
        </nav>

        {/* Right panel */}
        <div
          style={{
            background: "#fff",
            border: "1.5px solid #e8eaf0",
            borderRadius: 18,
            padding: "36px 40px 32px",
            minHeight: 460,
            animation: "riseUp 0.35s cubic-bezier(0.22,1,0.36,1) both",
          }}
        >
          {activeTab === "profile" && (
            <ProfileTab user={user} orgId={orgId} showToast={showToast} />
          )}
          {activeTab === "organization" && (
            <OrgTab orgId={orgId} showToast={showToast} />
          )}
          {activeTab === "security" && <SecurityTab showToast={showToast} />}
          {activeTab === "notifications" && (
            <NotificationsTab user={user} showToast={showToast} />
          )}
          {activeTab === "billing" && (
            <ComingSoon
              title="Billing & Subscription"
              description="View invoices, update payment methods and manage your plan."
              icon={
                <svg
                  width="52"
                  height="52"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <rect x="1" y="4" width="22" height="16" rx="2" />
                  <line x1="1" y1="10" x2="23" y2="10" />
                </svg>
              }
            />
          )}
          {activeTab === "api" && (
            <ComingSoon
              title="API Keys"
              description="Generate and manage API keys for programmatic access."
              icon={
                <svg
                  width="52"
                  height="52"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4" />
                </svg>
              }
            />
          )}
        </div>
      </div>

      {/* Toast */}
      <div
        style={{
          position: "fixed",
          bottom: 28,
          left: "50%",
          transform: `translateX(-50%) translateY(${toast.show ? 0 : 20}px)`,
          background: toast.type === "error" ? "#e74c3c" : "#111326",
          color: "#fff",
          fontFamily: "Plus Jakarta Sans, sans-serif",
          fontSize: "0.85rem",
          fontWeight: 600,
          padding: "12px 24px",
          borderRadius: 50,
          boxShadow: "0 8px 28px rgba(0,0,0,0.2)",
          opacity: toast.show ? 1 : 0,
          pointerEvents: "none",
          transition: "opacity 0.25s, transform 0.25s",
          zIndex: 300,
          whiteSpace: "nowrap",
          display: "flex",
          alignItems: "center",
          gap: 8,
        }}
      >
        {toast.type === "success" ? (
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <polyline points="20 6 9 17 4 12" />
          </svg>
        ) : (
          <svg
            width="15"
            height="15"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
        )}
        {toast.msg}
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════
// PROFILE TAB
// ══════════════════════════════════════════════════════════════
function ProfileTab({ user, orgId, showToast }) {
  const [form, setForm] = useState({
    firstName: "",
    lastName: "",
    phone: "",
    jobTitle: "",
    department: "",
  });
  const [loading, setLoading] = useState(false);
  const [saved, setSaved] = useState(false);

  // Pre-fill from localStorage user
  useEffect(() => {
    if (user) {
      setForm({
        firstName: user.firstName ?? "",
        lastName: user.lastName ?? "",
        phone: user.phoneNumber ?? "",
        jobTitle: user.jobTitle ?? "",
        department: user.department ?? "",
      });
    }
  }, [user]);

  const handleChange = (e) => {
    setForm((p) => ({ ...p, [e.target.name]: e.target.value }));
    setSaved(false);
  };

  const save = async () => {
    setLoading(true);
    try {
      await api.put(`/api/users/${user?.id}/profile`, {
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        phoneNumber: form.phone.trim(),
        jobTitle: form.jobTitle.trim(),
        department: form.department.trim(),
      });
      setSaved(true);
      showToast("Profile saved successfully");
    } catch (err) {
      showToast(err.message ?? "Failed to save profile", "error");
    } finally {
      setLoading(false);
    }
  };

  const initials =
    (
      (user?.firstName?.[0] ?? "") + (user?.lastName?.[0] ?? "")
    ).toUpperCase() || "?";
  const fullName =
    [form.firstName, form.lastName].filter(Boolean).join(" ") || "—";

  return (
    <div>
      {/* Profile header */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 24,
          marginBottom: 32,
          paddingBottom: 28,
          borderBottom: "1px solid #f0f1f7",
        }}
      >
        <div
          style={{
            width: 80,
            height: 80,
            borderRadius: "50%",
            background: "linear-gradient(135deg,#6a1bfa,#4044e4)",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: "#fff",
            fontSize: "1.5rem",
            fontWeight: 800,
            flexShrink: 0,
          }}
        >
          {initials}
        </div>
        <div>
          <div
            style={{
              fontSize: "1.4rem",
              fontWeight: 800,
              color: "#4044e4",
              letterSpacing: "-0.3px",
              marginBottom: 4,
            }}
          >
            {fullName}
          </div>
          <div
            style={{ fontSize: "0.88rem", color: "#555770", marginBottom: 4 }}
          >
            {user?.role ?? "—"}
          </div>
          <div style={{ fontSize: "0.8rem", color: "#aab0c6" }}>
            {user?.email}
          </div>
        </div>
      </div>

      {/* Form */}
      <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
        <div
          style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}
        >
          <Field label="First Name">
            <Input
              name="firstName"
              value={form.firstName}
              onChange={handleChange}
              placeholder="First name"
              disabled={loading}
            />
          </Field>
          <Field label="Last Name">
            <Input
              name="lastName"
              value={form.lastName}
              onChange={handleChange}
              placeholder="Last name"
              disabled={loading}
            />
          </Field>
        </div>

        <div
          style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}
        >
          <Field label="Email Address">
            <Input
              value={user?.email ?? ""}
              readOnly
              style={{ fontFamily: "monospace", fontSize: "0.85rem" }}
            />
          </Field>
          <Field label="Role">
            <Input value={user?.role ?? ""} readOnly />
          </Field>
        </div>

        <div
          style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}
        >
          <Field label="Phone Number" hint="Optional">
            <Input
              name="phone"
              value={form.phone}
              onChange={handleChange}
              placeholder="+1 (123) 456-7890"
              type="tel"
              disabled={loading}
            />
          </Field>
          <Field label="Job Title" hint="Optional">
            <Input
              name="jobTitle"
              value={form.jobTitle}
              onChange={handleChange}
              placeholder="e.g. Security Analyst"
              disabled={loading}
            />
          </Field>
        </div>

        <Field label="Department" hint="Optional">
          <Input
            name="department"
            value={form.department}
            onChange={handleChange}
            placeholder="e.g. Security Operations"
            disabled={loading}
          />
        </Field>

        <div style={{ height: 1, background: "#f0f1f7", margin: "4px 0" }} />

        <button
          onClick={save}
          disabled={loading}
          style={{
            width: "100%",
            padding: "14px 24px",
            background: saved ? "#2ace5f" : "#4044e4",
            color: "#fff",
            border: "none",
            borderRadius: 50,
            fontFamily: "Plus Jakarta Sans, sans-serif",
            fontSize: "0.95rem",
            fontWeight: 700,
            cursor: loading ? "not-allowed" : "pointer",
            boxShadow: saved
              ? "0 6px 20px rgba(42,206,95,0.3)"
              : "0 6px 24px rgba(64,68,228,0.32)",
            opacity: loading ? 0.7 : 1,
            transition: "background 0.3s, box-shadow 0.3s",
          }}
        >
          {loading ? "Saving…" : saved ? "✓ Saved" : "Save Profile"}
        </button>
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════
// ORGANIZATION TAB
// ══════════════════════════════════════════════════════════════
function OrgTab({ orgId, showToast }) {
  const [org, setOrg] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!orgId) return;
    api
      .get(`/api/organizations/${orgId}`)
      .then((d) => setOrg(d))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [orgId]);

  if (loading)
    return (
      <div style={{ display: "flex", flexDirection: "column", gap: 16 }}>
        {Array.from({ length: 4 }).map((_, i) => (
          <div
            key={i}
            style={{
              height: 48,
              borderRadius: 10,
              background:
                "linear-gradient(90deg,#f0f0f0 25%,#e4e4e4 50%,#f0f0f0 75%)",
              backgroundSize: "200% 100%",
              animation: "shimmer 1.4s infinite",
            }}
          />
        ))}
      </div>
    );

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
      <h2
        style={{
          margin: "0 0 4px",
          fontSize: "1.05rem",
          fontWeight: 800,
          color: "#111326",
        }}
      >
        Organization Details
      </h2>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}>
        <Field label="Organization Name">
          <Input value={org?.name ?? "—"} readOnly />
        </Field>
        <Field label="Status">
          <Input value={org?.status ?? "—"} readOnly />
        </Field>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}>
        <Field label="Industry">
          <Input value={org?.industry ?? "—"} readOnly />
        </Field>
        <Field label="Type">
          <Input value={org?.type ?? "—"} readOnly />
        </Field>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 20 }}>
        <Field label="Domain">
          <Input
            value={org?.domain ?? "—"}
            readOnly
            style={{ fontFamily: "monospace", fontSize: "0.85rem" }}
          />
        </Field>
        <Field label="Contact Email">
          <Input value={org?.contactEmail ?? "—"} readOnly />
        </Field>
      </div>

      <Field label="Organization ID">
        <Input
          value={orgId ?? "—"}
          readOnly
          style={{
            fontFamily: "monospace",
            fontSize: "0.78rem",
            color: "#9098b1",
          }}
        />
      </Field>

      <div
        style={{
          padding: "14px 16px",
          background: "#f8f9fe",
          border: "1.5px solid #e8eaf0",
          borderRadius: 10,
          fontSize: "0.82rem",
          color: "#555770",
        }}
      >
        ℹ️ Organization settings can only be updated by your administrator.
        Contact <strong>support@trap-intel.com</strong> for changes.
      </div>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════
// SECURITY TAB
// ══════════════════════════════════════════════════════════════
function SecurityTab({ showToast }) {
  const [form, setForm] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [show, setShow] = useState({
    current: false,
    new: false,
    confirm: false,
  });
  const [loading, setLoading] = useState(false);

  const handleChange = (e) =>
    setForm((p) => ({ ...p, [e.target.name]: e.target.value }));
  const toggleShow = (k) => setShow((p) => ({ ...p, [k]: !p[k] }));

  const validate = () => {
    if (!form.currentPassword) return "Current password is required.";
    if (!form.newPassword) return "New password is required.";
    if (form.newPassword.length < 8)
      return "Password must be at least 8 characters.";
    if (!/[A-Z]/.test(form.newPassword))
      return "Include at least one uppercase letter.";
    if (!/[a-z]/.test(form.newPassword))
      return "Include at least one lowercase letter.";
    if (!/[0-9]/.test(form.newPassword)) return "Include at least one number.";
    if (!/[^A-Za-z0-9]/.test(form.newPassword))
      return "Include at least one special character.";
    if (form.newPassword !== form.confirmPassword)
      return "Passwords do not match.";
    return null;
  };

  const save = async () => {
    const err = validate();
    if (err) {
      showToast(err, "error");
      return;
    }
    setLoading(true);
    try {
      await api.post("/api/users/change-password", {
        currentPassword: form.currentPassword,
        newPassword: form.newPassword,
        confirmNewPassword: form.confirmPassword,
      });
      setForm({ currentPassword: "", newPassword: "", confirmPassword: "" });
      showToast("Password changed successfully");
    } catch (err) {
      showToast(err.message ?? "Failed to change password", "error");
    } finally {
      setLoading(false);
    }
  };

  const PwField = ({ label, fieldKey, name, placeholder }) => (
    <Field label={label}>
      <div style={{ position: "relative" }}>
        <Input
          name={name}
          type={show[fieldKey] ? "text" : "password"}
          value={form[name]}
          onChange={handleChange}
          placeholder={placeholder}
          disabled={loading}
          style={{ paddingRight: 44 }}
        />
        <button
          type="button"
          onClick={() => toggleShow(fieldKey)}
          style={{
            position: "absolute",
            right: 12,
            top: "50%",
            transform: "translateY(-50%)",
            background: "none",
            border: "none",
            cursor: "pointer",
            color: "#9098b1",
            display: "flex",
            alignItems: "center",
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
            {show[fieldKey] ? (
              <>
                <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94" />
                <path d="M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19" />
                <line x1="1" y1="1" x2="23" y2="23" />
              </>
            ) : (
              <>
                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" />
                <circle cx="12" cy="12" r="3" />
              </>
            )}
          </svg>
        </button>
      </div>
    </Field>
  );

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 20 }}>
      <h2
        style={{
          margin: "0 0 4px",
          fontSize: "1.05rem",
          fontWeight: 800,
          color: "#111326",
        }}
      >
        Change Password
      </h2>
      <p style={{ margin: "0 0 8px", fontSize: "0.85rem", color: "#555770" }}>
        Choose a strong password that you haven't used before.
      </p>

      <PwField
        label="Current Password"
        fieldKey="current"
        name="currentPassword"
        placeholder="Enter current password"
      />
      <PwField
        label="New Password"
        fieldKey="new"
        name="newPassword"
        placeholder="Enter new password"
      />
      <PwField
        label="Confirm New Password"
        fieldKey="confirm"
        name="confirmPassword"
        placeholder="Repeat new password"
      />

      {/* Match indicator */}
      {form.confirmPassword && (
        <p
          style={{
            margin: "-8px 0 0",
            fontSize: "0.78rem",
            fontWeight: 600,
            color:
              form.newPassword === form.confirmPassword ? "#22c55e" : "#e74c3c",
          }}
        >
          {form.newPassword === form.confirmPassword
            ? "✓ Passwords match"
            : "✗ Passwords don't match"}
        </p>
      )}

      <p style={{ margin: 0, fontSize: "0.74rem", color: "#9098b1" }}>
        Min 8 chars · 1 uppercase · 1 lowercase · 1 number · 1 special character
      </p>

      <div style={{ height: 1, background: "#f0f1f7" }} />

      <button
        onClick={save}
        disabled={loading}
        style={{
          width: "100%",
          padding: "14px 24px",
          background: "#4044e4",
          color: "#fff",
          border: "none",
          borderRadius: 50,
          fontFamily: "Plus Jakarta Sans, sans-serif",
          fontSize: "0.95rem",
          fontWeight: 700,
          cursor: loading ? "not-allowed" : "pointer",
          boxShadow: "0 6px 24px rgba(64,68,228,0.32)",
          opacity: loading ? 0.7 : 1,
        }}
      >
        {loading ? "Updating…" : "Update Password"}
      </button>
    </div>
  );
}

// ══════════════════════════════════════════════════════════════
// NOTIFICATIONS TAB
// ══════════════════════════════════════════════════════════════
function NotificationsTab({ user, showToast }) {
  const [prefs, setPrefs] = useState(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);

  // Load from user object (already in localStorage)
  useEffect(() => {
    if (user) {
      setPrefs({
        emailEnabled:
          user.notif_email_enabled ?? user.prefEmailNotifications ?? true,
        pushEnabled:
          user.notif_push_enabled ?? user.prefPushNotifications ?? true,
        alertCreated: user.notif_alert_created ?? true,
        alertEscalation: user.notif_alert_escalation ?? true,
        alertAssignment: user.notif_alert_assignment ?? true,
        highSeverity: user.notif_high_severity_attack ?? true,
        malware: user.notif_malware_detection ?? true,
        weeklyDigest: user.notif_weekly_summary ?? true,
        monthlyDigest: user.notif_monthly_summary ?? true,
      });
    }
  }, [user]);

  const toggle = (key) => setPrefs((p) => ({ ...p, [key]: !p[key] }));

  const save = async () => {
    setSaving(true);
    try {
      await api.put("/api/notifications/settings", {
        emailEnabled: prefs.emailEnabled,
        pushEnabled: prefs.pushEnabled,
        alertCreated: prefs.alertCreated,
        alertEscalation: prefs.alertEscalation,
        alertAssignment: prefs.alertAssignment,
        highSeverityAttack: prefs.highSeverity,
        malwareDetection: prefs.malware,
        weeklySummary: prefs.weeklyDigest,
        monthlySummary: prefs.monthlyDigest,
      });
      showToast("Notification preferences saved");
    } catch (err) {
      showToast(err.message ?? "Failed to save preferences", "error");
    } finally {
      setSaving(false);
    }
  };

  if (!prefs) return <div style={{ color: "#aab0c6" }}>Loading…</div>;

  const Toggle = ({ label, desc, k }) => (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "14px 0",
        borderBottom: "1px solid #f0f1f7",
      }}
    >
      <div>
        <div style={{ fontSize: "0.88rem", fontWeight: 600, color: "#111326" }}>
          {label}
        </div>
        {desc && (
          <div style={{ fontSize: "0.78rem", color: "#9098b1", marginTop: 2 }}>
            {desc}
          </div>
        )}
      </div>
      <button
        type="button"
        onClick={() => toggle(k)}
        style={{
          position: "relative",
          width: 44,
          height: 26,
          borderRadius: 50,
          border: "none",
          cursor: "pointer",
          flexShrink: 0,
          background: prefs[k] ? "#4044e4" : "#e8eaf0",
          transition: "background 0.25s",
        }}
      >
        <span
          style={{
            position: "absolute",
            top: 3,
            left: prefs[k] ? "calc(100% - 23px)" : 3,
            width: 20,
            height: 20,
            borderRadius: "50%",
            background: "#fff",
            boxShadow: "0 1px 4px rgba(0,0,0,0.2)",
            transition: "left 0.25s cubic-bezier(0.34,1.56,0.64,1)",
          }}
        />
      </button>
    </div>
  );

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
      <h2
        style={{
          margin: "0 0 4px",
          fontSize: "1.05rem",
          fontWeight: 800,
          color: "#111326",
        }}
      >
        Notification Preferences
      </h2>
      <p style={{ margin: "0 0 20px", fontSize: "0.85rem", color: "#555770" }}>
        Choose what notifications you receive.
      </p>

      <h3
        style={{
          margin: "0 0 4px",
          fontSize: "0.78rem",
          fontWeight: 700,
          color: "#aab0c6",
          textTransform: "uppercase",
          letterSpacing: "0.6px",
        }}
      >
        Channels
      </h3>
      <Toggle
        k="emailEnabled"
        label="Email Notifications"
        desc="Receive notifications via email"
      />
      <Toggle
        k="pushEnabled"
        label="Push Notifications"
        desc="Browser and mobile push notifications"
      />

      <h3
        style={{
          margin: "20px 0 4px",
          fontSize: "0.78rem",
          fontWeight: 700,
          color: "#aab0c6",
          textTransform: "uppercase",
          letterSpacing: "0.6px",
        }}
      >
        Alert Events
      </h3>
      <Toggle
        k="alertCreated"
        label="Alert Created"
        desc="When a new alert is generated"
      />
      <Toggle
        k="alertEscalation"
        label="Alert Escalation"
        desc="When an alert is escalated"
      />
      <Toggle
        k="alertAssignment"
        label="Alert Assignment"
        desc="When an alert is assigned to you"
      />
      <Toggle
        k="highSeverity"
        label="High Severity Attacks"
        desc="Critical and high severity attacks"
      />
      <Toggle
        k="malware"
        label="Malware Detection"
        desc="When malware is uploaded to a honeypot"
      />

      <h3
        style={{
          margin: "20px 0 4px",
          fontSize: "0.78rem",
          fontWeight: 700,
          color: "#aab0c6",
          textTransform: "uppercase",
          letterSpacing: "0.6px",
        }}
      >
        Digests
      </h3>
      <Toggle
        k="weeklyDigest"
        label="Weekly Summary"
        desc="Weekly activity digest"
      />
      <Toggle
        k="monthlyDigest"
        label="Monthly Summary"
        desc="Monthly activity digest"
      />

      <div style={{ height: 1, background: "#f0f1f7", margin: "12px 0" }} />

      <button
        onClick={save}
        disabled={saving}
        style={{
          width: "100%",
          padding: "14px 24px",
          background: "#4044e4",
          color: "#fff",
          border: "none",
          borderRadius: 50,
          fontFamily: "Plus Jakarta Sans, sans-serif",
          fontSize: "0.95rem",
          fontWeight: 700,
          cursor: saving ? "not-allowed" : "pointer",
          boxShadow: "0 6px 24px rgba(64,68,228,0.32)",
          opacity: saving ? 0.7 : 1,
        }}
      >
        {saving ? "Saving…" : "Save Preferences"}
      </button>
    </div>
  );
}
