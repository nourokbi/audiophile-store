// The one place the shop talks to the API, Vite forwards /api to localhost:5040 in development and VITE_API_URL sets it in production
const BASE = import.meta.env.VITE_API_URL ?? '/api';

// The session is kept in localStorage so a refresh keeps you logged in, an httpOnly cookie is the safer choice for later
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
		// With storage blocked the session still lives until the tab closes
	}
	listeners.forEach((fn) => fn(next));
}

// Lets AuthContext re-render when a refresh fails and the session is dropped
export function onSessionChange(fn) {
	listeners.add(fn);
	return () => listeners.delete(fn);
}

// On a 401 the refresh token is traded once for a new pair and the call repeated, parallel calls share that one refresh
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

// Turns the API's ProblemDetails into one readable sentence
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
