const delay = (ms = 600) => new Promise((r) => setTimeout(r, ms));

export const api = {
  get: async (endpoint) => {
    await delay();

    if (endpoint.includes("/attacks/summary"))
      return { total24h: 142, criticalCount: 7 };

    if (endpoint.includes("/honeypots/summary"))
      return { activeCount: 5, totalCount: 6, healthStatus: "Healthy" };

    if (endpoint.includes("/alerts/summary"))
      return { openCount: 23, criticalCount: 4 };

    if (endpoint.includes("/threat-actors/summary"))
      return { activeCount: 12, newLast7Days: 3 };

    if (endpoint.includes("/alerts"))
      return {
        items: [
          {
            id: 1,
            title: "SSH Brute Force",
            createdAt: new Date().toISOString(),
            severity: "Critical",
          },
          {
            id: 2,
            title: "SQL Injection",
            createdAt: new Date(Date.now() - 180000).toISOString(),
            severity: "High",
          },
          {
            id: 3,
            title: "Port Scan",
            createdAt: new Date(Date.now() - 600000).toISOString(),
            severity: "Medium",
          },
        ],
      };

    if (endpoint.includes("/threat-actors"))
      return {
        items: [
          { id: 1, name: "APT-28", category: "State-Sponsored", riskScore: 95 },
          { id: 2, name: "Lazarus", category: "Financial", riskScore: 88 },
          { id: 3, name: "OilRig", category: "Espionage", riskScore: 74 },
          {
            id: 4,
            name: "Sandworm",
            category: "Infrastructure",
            riskScore: 61,
          },
        ],
      };

    return null;
  },

  post: async () => null,
  put: async () => null,
  delete: async () => null,
};

export default api;
