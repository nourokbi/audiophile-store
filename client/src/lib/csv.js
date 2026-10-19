// Downloads rows as a CSV file the browser saves, the first row is the header
export function downloadCsv(fileName, rows) {
	const escape = (value) => {
		const text = String(value ?? '');
		return /[",\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
	};
	const csv = rows.map((row) => row.map(escape).join(',')).join('\r\n');
	const url = URL.createObjectURL(new Blob([csv], { type: 'text/csv;charset=utf-8' }));
	const link = Object.assign(document.createElement('a'), { href: url, download: fileName });
	link.click();
	URL.revokeObjectURL(url);
}
