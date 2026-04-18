const { app, BrowserWindow, ipcMain, globalShortcut } = require('electron');
const fs = require('fs');
const http = require('http');
const path = require('path');
const WINDOW_STATE_FILE = 'window-state.json';
const DEFAULT_WINDOW_STATE = {
	width: 1400,
	height: 920,
	minWidth: 1080,
	minHeight: 720,
};

let server;
let serverPort = null;
let mainWindow = null;
let ipcHandlersRegistered = false;

function getWindowStatePath() {
	return path.join(app.getPath('userData'), WINDOW_STATE_FILE);
}

function loadWindowState() {
	const defaults = {
		width: DEFAULT_WINDOW_STATE.width,
		height: DEFAULT_WINDOW_STATE.height,
		x: undefined,
		y: undefined,
		isMaximized: false,
	};

	try {
		const raw = fs.readFileSync(getWindowStatePath(), 'utf8');
		const parsed = JSON.parse(raw);
		return {
			width: typeof parsed.width === 'number' ? parsed.width : defaults.width,
			height: typeof parsed.height === 'number' ? parsed.height : defaults.height,
			x: typeof parsed.x === 'number' ? parsed.x : undefined,
			y: typeof parsed.y === 'number' ? parsed.y : undefined,
			isMaximized: parsed.isMaximized === true,
		};
	} catch {
		return defaults;
	}
}

function saveWindowState(win) {
	if (!win || win.isDestroyed()) return;

	const bounds = win.getBounds();
	const state = {
		width: bounds.width,
		height: bounds.height,
		x: bounds.x,
		y: bounds.y,
		isMaximized: win.isMaximized(),
	};

	try {
		fs.mkdirSync(path.dirname(getWindowStatePath()), { recursive: true });
		fs.writeFileSync(getWindowStatePath(), JSON.stringify(state, null, 2), 'utf8');
	} catch (err) {
		console.warn('[WindowState] Failed to persist state:', err);
	}
}

function publishMaximizedState(win) {
	if (!win || win.isDestroyed()) return;
	win.webContents.send('window:maximized-changed', win.isMaximized());
}

function registerIpcHandlers() {
	if (ipcHandlersRegistered) return;
	ipcHandlersRegistered = true;

	ipcMain.on('window:minimize', (event) => {
		const win = BrowserWindow.fromWebContents(event.sender);
		win?.minimize();
	});

	ipcMain.on('window:maximize', (event) => {
		const win = BrowserWindow.fromWebContents(event.sender);
		if (!win) return;

		if (win.isMaximized()) win.unmaximize();
		else win.maximize();
		publishMaximizedState(win);
	});

	ipcMain.on('window:close', (event) => {
		const win = BrowserWindow.fromWebContents(event.sender);
		win?.close();
	});

	ipcMain.handle('window:isMaximized', (event) => {
		const win = BrowserWindow.fromWebContents(event.sender);
		return win ? win.isMaximized() : false;
	});
}

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
			serverPort = address.port;
			resolve(address.port);
		});
	});
}

async function ensureStaticServer(rootDir) {
	if (server && serverPort !== null) {
		return serverPort;
	}

	return startStaticServer(rootDir);
}

function createMainWindow() {
	const savedState = loadWindowState();
	const windowOptions = {
		width: savedState.width,
		height: savedState.height,
		minWidth: DEFAULT_WINDOW_STATE.minWidth,
		minHeight: DEFAULT_WINDOW_STATE.minHeight,
		show: false,
		frame: false,
		titleBarStyle: 'hidden',
		backgroundColor: '#f7f6f3',
		autoHideMenuBar: true,
		webPreferences: {
			contextIsolation: true,
			nodeIntegration: false,
			preload: path.join(__dirname, 'preload.cjs'),
		}
	};

	if (typeof savedState.x === 'number') windowOptions.x = savedState.x;
	if (typeof savedState.y === 'number') windowOptions.y = savedState.y;

	const win = new BrowserWindow(windowOptions);
	if (savedState.isMaximized) {
		win.maximize();
	}

	const persistWindowState = () => saveWindowState(win);
	win.on('resize', persistWindowState);
	win.on('move', persistWindowState);
	win.on('close', persistWindowState);
	win.on('maximize', () => {
		persistWindowState();
		publishMaximizedState(win);
	});
	win.on('unmaximize', () => {
		persistWindowState();
		publishMaximizedState(win);
	});

	win.once('ready-to-show', () => {
		win.show();
		publishMaximizedState(win);
	});

	// Global shortcut for quick capture
	globalShortcut.unregister('CommandOrControl+Shift+N');
	const shortcutRegistered = globalShortcut.register('CommandOrControl+Shift+N', () => {
		win.webContents.send('quick-capture');
		if (win.isMinimized()) win.restore();
		win.focus();
	});
	if (!shortcutRegistered) {
		console.warn('Failed to register quick-capture shortcut');
	}

	// Log console messages from renderer
	win.webContents.on('console-message', (event, level, message, line, sourceId) => {
		console.log(`[Renderer ${level}]:`, message, sourceId ? `(${sourceId}:${line})` : '');
	});

	// Log page load errors
	win.webContents.on('did-fail-load', (event, errorCode, errorDescription, validatedURL) => {
		console.error('Page failed to load:', errorCode, errorDescription, validatedURL);
	});

	win.on('closed', () => {
		if (mainWindow === win) {
			mainWindow = null;
		}
	});

	return win;
}

async function bootstrap() {
	registerIpcHandlers();
	const win = createMainWindow();
	mainWindow = win;

	const startUrl = process.env.ELECTRON_START_URL;
	if (startUrl) {
		console.log('Loading from dev server:', startUrl);
		await win.loadURL(startUrl);
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

	const port = await ensureStaticServer(rootDir);
	console.log('Static server running on port:', port);
	console.log('Loading from:', `http://127.0.0.1:${port}/`);
	await win.loadURL(`http://127.0.0.1:${port}/`);
}

const hasSingleInstanceLock = app.requestSingleInstanceLock();
if (!hasSingleInstanceLock) {
	app.quit();
} else {
	app.whenReady()
		.then(bootstrap)
		.catch((err) => {
			console.error('Bootstrap failed:', err);
			app.quit();
		});

	app.on('second-instance', () => {
		if (!mainWindow) return;
		if (mainWindow.isMinimized()) mainWindow.restore();
		mainWindow.focus();
	});

	app.on('activate', () => {
		if (BrowserWindow.getAllWindows().length > 0) return;
		bootstrap().catch((err) => {
			console.error('Bootstrap failed on activate:', err);
		});
	});
}

app.on('window-all-closed', () => {
	if (process.platform !== 'darwin') app.quit();
});

app.on('before-quit', () => {
	globalShortcut.unregisterAll();
	if (server) {
		server.close();
		server = null;
		serverPort = null;
	}
});
