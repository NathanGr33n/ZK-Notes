import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		adapter: adapter({
			// SPA fallback so client-side routing works when reloading deep links
			fallback: 'index.html'
		})
	}
};

export default config;
