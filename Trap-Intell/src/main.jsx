import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import App from "./App.jsx";
import { Toaster } from "react-hot-toast";

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <App />
    <Toaster
      position="top-center"
      toastOptions={{
        success: {
          duration: 5000,
          style: {
            background: "#edfbf2",
            border: "1.5px solid #a8edbe",
            borderRadius: "12px",
            padding: "14px 20px",
            minWidth: "340px",
            maxWidth: "560px",
            boxShadow: "0 8px 32px rgba(42, 206, 95, 0.12)",
            color: "#111326",
            fontFamily: '"Plus Jakarta Sans", sans-serif',
            fontSize: "0.92rem",
            fontWeight: "500",
          },
          iconTheme: {
            primary: "#2ace5f",
            secondary: "#d2f5de",
          },
        },
      }}
    />
  </StrictMode>,
);
