import React from 'react';

// Light or dark, remembered in this browser, applied as the dark class on the html element
const STORAGE_KEY = 'audiophile.theme';
const ThemeContext = React.createContext(null);

function initialTheme() {
	try {
		const saved = localStorage.getItem(STORAGE_KEY);
		if (saved === 'light' || saved === 'dark') return saved;
	} catch {
		// Storage can be blocked, then the system setting decides
	}
	return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

export function ThemeProvider({ children }) {
	const [theme, setTheme] = React.useState(initialTheme);

	React.useEffect(() => {
		document.documentElement.classList.toggle('dark', theme === 'dark');
		try {
			localStorage.setItem(STORAGE_KEY, theme);
		} catch {
			// Not remembered, but the page still switches
		}
	}, [theme]);

	const value = React.useMemo(
		() => ({ theme, setTheme, toggleTheme: () => setTheme((t) => (t === 'dark' ? 'light' : 'dark')) }),
		[theme],
	);
	return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
	return React.useContext(ThemeContext);
}
