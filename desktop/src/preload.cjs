const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
	minimize: () => ipcRenderer.send('window:minimize'),
	maximize: () => ipcRenderer.send('window:maximize'),
	close: () => ipcRenderer.send('window:close'),
	isMaximized: () => ipcRenderer.invoke('window:isMaximized'),
	onQuickCapture: (callback) => {
		ipcRenderer.on('quick-capture', callback);
		return () => ipcRenderer.removeListener('quick-capture', callback);
	},
	isElectron: true,
});
