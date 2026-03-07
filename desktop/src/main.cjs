const { app, BrowserWindow, ipcMain, globalShortcut } = require('electron');
const fs = require('fs');
const http = require('http');
const path = require('path');

let server;

function getMimeType(filePath) {
	const ext = path.extname(filePath).toLowerCase();
	switch (ext) {
		case '.html':
			return 'text/html; charset=utf-8';
		case '.js':
			return 'text/javascript; charset=utf-8';
		case '.css':
			return 'text/css; charset=utf-8';
		case '.json':
			return 'application/json; charset=utf-8';
		case '.svg':
			return 'image/svg+xml';
		case '.png':
			return 'image/png';
		case '.jpg':
		case '.jpeg':
			return 'image/jpeg';
		case '.webp':
			return 'image/webp';
		case '.ico':
			return 'image/x-icon';
		case '.woff2':
			return 'font/woff2';
		case '.woff':
			return 'font/woff';
		case '.ttf':
			return 'font/ttf';
		default:
			return 'application/octet-stream';
	}
}

function getWebRootDir() {
	// In packaged apps, electron-builder copies extraResources to process.resourcesPath
	if (app.isPackaged) return path.join(process.resourcesPath, 'web');

	// For local testing (npm run start), load the built output from web/build
	return path.resolve(__dirname, '../../web/build');
}

function safeJoin(rootDir, urlPath) {
	// Prevent path traversal (.., absolute paths, etc.)
	const raw = decodeURIComponent(urlPath);
	const withoutQuery = raw.split('?')[0].split('#')[0];
	const normalized = path.normalize(withoutQuery).replace(/^([/\\])+/, '');
	const joined = path.join(rootDir, normalized);

	if (!joined.startsWith(rootDir)) return null;
	return joined;
}

function startStaticServer(rootDir) {
	return new Promise((resolve, reject) => {
		server = http.createServer((req, res) => {
			const requestedPath = (req.url ?? '/');
			const filePath = safeJoin(rootDir, requestedPath === '/' ? '/index.html' : requestedPath);
			if (!filePath) {
				res.writeHead(400);
				res.end('Bad Request');
				return;
			}

			const tryServe = (candidatePath, fallbackToIndex) => {
				fs.readFile(candidatePath, (err, data) => {
					if (err) {
						if (fallbackToIndex) {
							const indexPath = path.join(rootDir, 'index.html');
							fs.readFile(indexPath, (indexErr, indexData) => {
								if (indexErr) {
									res.writeHead(500);
									res.end('Missing index.html');
									return;
								}
								res.writeHead(200, { 'Content-Type': getMimeType(indexPath) });
								res.end(indexData);
							});
							return;
						}

						res.writeHead(404);
						res.end('Not Found');
						return;
					}

					res.writeHead(200, { 'Content-Type': getMimeType(candidatePath) });
					res.end(data);
				});
			};

			// First try the real file; if it doesn't exist, fall back to index.html for SPA routing.
			tryServe(filePath, true);
		});

		server.on('error', reject);
		server.listen(0, '127.0.0.1', () => {
			const address = server.address();
			if (!address || typeof address === 'string') return reject(new Error('Failed to bind server port'));
			resolve(address.port);
		});
	});
}

function createMainWindow() {
	const win = new BrowserWindow({
		width: 1200,
		height: 800,
		frame: false,
		titleBarStyle: 'hidden',
		backgroundColor: '#0d0d0d',
		webPreferences: {
			contextIsolation: true,
			nodeIntegration: false,
			preload: path.join(__dirname, 'preload.cjs'),
		}
	});

	// Window control IPC handlers
	ipcMain.on('window:minimize', () => win.minimize());
	ipcMain.on('window:maximize', () => {
		if (win.isMaximized()) win.unmaximize();
		else win.maximize();
	});
	ipcMain.on('window:close', () => win.close());
	ipcMain.handle('window:isMaximized', () => win.isMaximized());

	// Global shortcut for quick capture
	globalShortcut.register('CommandOrControl+Shift+N', () => {
		win.webContents.send('quick-capture');
		if (win.isMinimized()) win.restore();
		win.focus();
	});

	// Log console messages from renderer
	win.webContents.on('console-message', (event, level, message, line, sourceId) => {
		console.log(`[Renderer ${level}]:`, message, sourceId ? `(${sourceId}:${line})` : '');
	});

	// Log page load errors
	win.webContents.on('did-fail-load', (event, errorCode, errorDescription, validatedURL) => {
		console.error('Page failed to load:', errorCode, errorDescription, validatedURL);
	});

	return win;
}

async function bootstrap() {
	const win = createMainWindow();

	const startUrl = process.env.ELECTRON_START_URL;
	if (startUrl) {
		console.log('Loading from dev server:', startUrl);
		await win.loadURL(startUrl);
		win.webContents.openDevTools();
		return;
	}

	const rootDir = getWebRootDir();
	if (!fs.existsSync(rootDir)) {
		await win.loadURL(
			'data:text/html;charset=utf-8,' +
			encodeURIComponent(
				`<h2>Missing web build output</h2>
				<p>Expected: <code>${rootDir}</code></p>
				<p>Run <code>npm --prefix web install</code> and <code>npm --prefix web run build</code>, then try again.</p>`
			)
		);
		return;
	}

	const port = await startStaticServer(rootDir);
	console.log('Static server running on port:', port);
	console.log('Loading from:', `http://127.0.0.1:${port}/`);
	await win.loadURL(`http://127.0.0.1:${port}/`);
}

app.whenReady().then(bootstrap);

app.on('window-all-closed', () => {
	if (process.platform !== 'darwin') app.quit();
});

app.on('before-quit', () => {
	globalShortcut.unregisterAll();
	if (server) server.close();
});
