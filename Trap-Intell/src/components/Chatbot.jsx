import { useState, useEffect, useRef } from "react";
import styles from "./ChatBot.module.css";

const QUICK_PROMPTS = [
  { icon: "🛡️", text: "Active threats?" },
  { icon: "🍯", text: "Honeypot status?" },
  { icon: "🚨", text: "Recent alerts?" },
  { icon: "📊", text: "Attack summary?" },
];

const INITIAL_MESSAGE = {
  id: 1,
  role: "bot",
  text: "Hi! I'm your TrapIntell AI assistant. I can help you analyze threats, review alerts, and monitor your honeypots. What would you like to know?",
};

export default function ChatBot() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([INITIAL_MESSAGE]);
  const [input, setInput] = useState("");
  const [typing, setTyping] = useState(false);
  const messagesEndRef = useRef(null);
  const inputRef = useRef(null);

  // ── Scroll to bottom ───────────────────────────────────────
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, typing]);

  // ── Focus input on open ────────────────────────────────────
  useEffect(() => {
    if (open) setTimeout(() => inputRef.current?.focus(), 100);
  }, [open]);

  // ── Send message ───────────────────────────────────────────
  const send = async (text) => {
    const trimmed = text.trim();
    if (!trimmed || typing) return;

    setInput("");
    setMessages((prev) => [
      ...prev,
      {
        id: Date.now(),
        role: "user",
        text: trimmed,
      },
    ]);
    setTyping(true);

    // Simulate AI response — replace with real API call later
    setTimeout(
      () => {
        const response = getResponse(trimmed);
        setMessages((prev) => [
          ...prev,
          {
            id: Date.now() + 1,
            role: "bot",
            text: response,
          },
        ]);
        setTyping(false);
      },
      1000 + Math.random() * 800,
    );
  };

  const handleKeyDown = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      send(input);
    }
  };

  return (
    <>
      {/* Chat panel */}
      <div className={`${styles.chatPanel} ${open ? styles.open : ""}`}>
        {/* Header */}
        <div className={styles.chatHeader}>
          <div className={styles.chatHeaderIcon}>
            <svg
              width="22"
              height="22"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <rect x="2" y="3" width="20" height="14" rx="2" />
              <path d="M8 21h8M12 17v4" />
              <circle cx="9" cy="10" r="1" fill="currentColor" />
              <circle cx="15" cy="10" r="1" fill="currentColor" />
              <path d="M9 13c0 0 1 1 3 1s3-1 3-1" />
            </svg>
          </div>
          <div className={styles.chatHeaderInfo}>
            <div className={styles.chatHeaderTitle}>
              <span className={styles.chatHeaderSparkle}>✦</span>
              TrapIntell AI
            </div>
            <div className={styles.chatHeaderSub}>
              Security Intelligence Assistant
            </div>
          </div>
          <div className={styles.chatOnlineBadge}>
            <span className={styles.chatOnlineDot} />
            Online
          </div>
        </div>

        {/* Messages */}
        <div className={styles.chatMessages}>
          {messages.map((msg) => (
            <div
              key={msg.id}
              className={`${styles.msgRow} ${msg.role === "user" ? styles.user : styles.bot}`}
            >
              <div className={styles.msgAvatar}>
                {msg.role === "bot" ? "AI" : "AH"}
              </div>
              <div className={styles.msgBubble}>{msg.text}</div>
            </div>
          ))}

          {typing && (
            <div
              className={`${styles.msgRow} ${styles.bot} ${styles.msgTyping}`}
            >
              <div className={styles.msgAvatar}>AI</div>
              <div className={styles.msgBubble}>
                <span className={styles.typingDot} />
                <span className={styles.typingDot} />
                <span className={styles.typingDot} />
              </div>
            </div>
          )}
          <div ref={messagesEndRef} />
        </div>

        {/* Quick prompts */}
        <div className={styles.chatQuickPrompts}>
          {QUICK_PROMPTS.map((p) => (
            <button
              key={p.text}
              className={styles.quickPrompt}
              onClick={() => send(p.text)}
              disabled={typing}
            >
              {p.icon} {p.text}
            </button>
          ))}
        </div>

        {/* Input */}
        <div className={styles.chatInputWrap}>
          <input
            ref={inputRef}
            className={styles.chatInput}
            placeholder="Ask about threats, alerts, honeypots..."
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={typing}
          />
          <button
            className={styles.btnChatSend}
            onClick={() => send(input)}
            disabled={!input.trim() || typing}
          >
            <svg
              width="14"
              height="14"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <line x1="22" y1="2" x2="11" y2="13" />
              <polygon points="22 2 15 22 11 13 2 9 22 2" />
            </svg>
            Send
          </button>
        </div>
      </div>

      {/* FAB */}
      <button
        className={`${styles.fab} ${open ? styles.chatOpen : ""}`}
        onClick={() => setOpen((o) => !o)}
        aria-label="AI Assistant"
      >
        {open ? (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <line x1="18" y1="6" x2="6" y2="18" />
            <line x1="6" y1="6" x2="18" y2="18" />
          </svg>
        ) : (
          <svg
            width="24"
            height="24"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <rect x="2" y="3" width="20" height="14" rx="2" />
            <path d="M8 21h8M12 17v4" />
            <circle cx="9" cy="10" r="1" fill="currentColor" />
            <circle cx="15" cy="10" r="1" fill="currentColor" />
            <path d="M9 13c0 0 1 1 3 1s3-1 3-1" />
          </svg>
        )}
      </button>
    </>
  );
}

// ── Simple response logic ──────────────────────────────────────
function getResponse(text) {
  const t = text.toLowerCase();
  if (t.includes("threat") || t.includes("actor"))
    return "Based on current data, there are 2 active threat actors targeting your honeypots. APT28 (Fancy Bear) has the highest risk score at 95.5. Would you like more details?";
  if (t.includes("honeypot") || t.includes("status"))
    return "You have 3 active honeypots: SSH-Honeypot-DMZ-01, HTTP-Honeypot-Web-01, and SMB-Honeypot-Internal-01. All are healthy and connected.";
  if (t.includes("alert"))
    return "There are currently 3 open alerts: 1 Critical (SSH Brute Force), 2 Medium (Lateral Movement, Reconnaissance). The critical alert requires immediate attention.";
  if (t.includes("attack") || t.includes("summary"))
    return "In the last 24 hours: 3 attack events captured. Top attack types: BruteForce from Russia (High severity), Reconnaissance from US (Low), LateralMovement from Ukraine (High).";
  if (t.includes("hello") || t.includes("hi"))
    return "Hello! I'm here to help you monitor your security posture. Ask me about threats, alerts, honeypots, or attack patterns.";
  return "I can help you analyze threats, check honeypot status, review alerts, and summarize attack patterns. What specific information do you need?";
}
