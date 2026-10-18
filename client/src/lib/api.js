// The one place the shop talks to the .NET API.
//
// In development Vite forwards /api/* to http://localhost:5040 (see vite.config.js), so the
// browser only ever sees one origin and the API needs no CORS setup. In production set
// VITE_API_URL to the API's address.
const BASE = import.meta.env.VITE_API_URL ?? '/api';

// The session (tokens + user) is kept in localStorage so a page refresh keeps you logged in.
// Simple, but readable by any script on the page; an httpOnly cookie for the refresh token is
// the safer production choice (roadmap Phase 4/7).
const SESSION_KEY = 'audiophile.session';

export class ApiError extends Error {
	constructor(status, message, problem) {
		super(message);
		this.status = status;
		this.problem = problem;
	}
}

let session = readSession();
const listeners = new Set();

function readSession() {
	try {
		return JSON.parse(localStorage.getItem(SESSION_KEY)) ?? null;
	} catch {
		return null;
	}
}

export function getSession() {
	return session;
}

export function setSession(next) {
	session = next;
	try {
		if (next) localStorage.setItem(SESSION_KEY, JSON.stringify(next));
		else localStorage.removeItem(SESSION_KEY);
	} catch {
		// Private mode or blocked storage: the session still lives until the tab closes.
	}
	listeners.forEach((fn) => fn(next));
}

// Lets AuthContext re-render when a refresh fails and the session is dropped.
export function onSessionChange(fn) {
	listeners.add(fn);
	return () => listeners.delete(fn);
}

// The access token lives 15 minutes. When a call gets 401, swap the refresh token for a new
// pair once and repeat the call. Parallel 401s share one refresh: refresh tokens are single-use,
// and the API treats a second use as theft and ends every session.
let refreshing = null;

async function refreshSession() {
	if (!session?.refreshToken) return false;
	refreshing ??= fetch(`${BASE}/auth/refresh`, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({ refreshToken: session.refreshToken }),
	})
		.then(async (res) => {
			if (!res.ok) {
				setSession(null);
				return false;
			}
			setSession(await res.json());
			return true;
		})
		.catch(() => false)
		.finally(() => {
			refreshing = null;
		});
	return refreshing;
}

// ProblemDetails → one readable sentence for the UI.
function messageFrom(problem, status) {
	if (problem?.errors) {
		const first = Object.values(problem.errors).flat()[0];
		if (first) return first;
	}
	return problem?.detail || problem?.title || `Request failed (${status})`;
}

export async function api(path, { method = 'GET', body, headers = {}, retry = true } = {}) {
	const res = await fetch(`${BASE}${path}`, {
		method,
		headers: {
			...(body !== undefined && { 'Content-Type': 'application/json' }),
			...(session?.accessToken && { Authorization: `Bearer ${session.accessToken}` }),
			...headers,
		},
		body: body === undefined ? undefined : JSON.stringify(body),
	});

	if (res.status === 401 && retry && session?.refreshToken && !path.startsWith('/auth/')) {
		if (await refreshSession()) return api(path, { method, body, headers, retry: false });
	}

	const text = await res.text();
	const data = text ? JSON.parse(text) : null;
	if (!res.ok) throw new ApiError(res.status, messageFrom(data, res.status), data);
	return data;
}
