// The one place the shop talks to the API, Vite forwards /api to localhost:5040 in development and VITE_API_URL sets it in production
const BASE = import.meta.env.VITE_API_URL ?? '/api';

export class ApiError extends Error {
	constructor(status, message, problem) {
		super(message);
		this.status = status;
		this.problem = problem;
	}
}

// The session is { accessToken, user } in memory only, so a script injected into the page finds no token in storage
// The refresh token sits in an httpOnly cookie the browser sends to /auth by itself, and a page load restores the session from it
let session = null;
const listeners = new Set();

export function setSession(next) {
	session = next;
	listeners.forEach((fn) => fn(next));
}

// Lets AuthContext re-render when a refresh fails and the session is dropped
export function onSessionChange(fn) {
	listeners.add(fn);
	return () => listeners.delete(fn);
}

// Trades the cookie for a new session, used on page load and once after a 401, parallel calls share that one refresh
let refreshing = null;

export async function refreshSession() {
	refreshing ??= fetch(`${BASE}/auth/refresh`, { method: 'POST' })
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

	if (res.status === 401 && retry && session && !path.startsWith('/auth/')) {
		if (await refreshSession()) return api(path, { method, body, headers, retry: false });
	}

	const text = await res.text();
	const data = text ? JSON.parse(text) : null;
	if (!res.ok) throw new ApiError(res.status, messageFrom(data, res.status), data);
	return data;
}
