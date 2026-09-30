// Shared helpers for the Audiophile demo pages. Same-origin calls to the API.

const Api = {
  async request(path, options = {}) {
    const res = await fetch(path, {
      ...options,
      headers: { "Content-Type": "application/json", ...(options.headers || {}) },
    });
    const text = await res.text();
    const body = text ? JSON.parse(text) : null;
    if (!res.ok) {
      // ProblemDetails from the API: show its detail, it is written for people.
      throw new Error(body?.detail || body?.title || `Request failed (${res.status})`);
    }
    return body;
  },
  products: () => Api.request("/products"),
  orders: () => Api.request("/orders"),
  order: (id) => Api.request(`/orders/${id}`),
  placeOrder: (body, idempotencyKey) =>
    Api.request("/orders", { method: "POST", body: JSON.stringify(body), headers: { "Idempotency-Key": idempotencyKey } }),
  sync: (id) => Api.request(`/orders/${id}/payment/sync`, { method: "POST" }),
};

const money = (n) =>
  `${Number(n).toLocaleString("en-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 })} EGP`;

// Mirrors ShippingPolicy on the server, for display only; the server decides.
const shippingFor = (subTotal) => (subTotal >= 5000 ? 0 : 50);

const METHOD_LABELS = {
  Card: "Card",
  Kiosk: "Fawry / cash outlet",
  VodafoneCash: "Mobile wallet",
  CashOnDelivery: "Cash on delivery",
};

const ORDER_TONE = {
  Pending: "warn", Confirmed: "ok", Processing: "info", Shipped: "info", Delivered: "ok", Cancelled: "bad",
};
const PAYMENT_TONE = {
  Pending: "warn", Succeeded: "ok", Failed: "bad", PartiallyRefunded: "info", Refunded: "info", Voided: "bad",
};

const pill = (text, tone) => `<span class="pill ${tone}">${escapeHtml(text)}</span>`;

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}

// localStorage can be unavailable (private mode); the pages still work without it.
const Store = {
  get(key, fallback) {
    try { return JSON.parse(localStorage.getItem(key)) ?? fallback; } catch { return fallback; }
  },
  set(key, value) {
    try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* ignore */ }
  },
};
