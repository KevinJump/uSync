import { defineConfig } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';

export default defineConfig({
	build: {
		lib: {
			entry: 'src/index.ts', // your web component source file
			formats: ['es'],
		},
		outDir: '../wwwroot/App_Plugins/uSync',
		emptyOutDir: true,
		sourcemap: true,
		rollupOptions: {
			external: [/^@umbraco/],
			onwarn: () => {},
		},
	},
	base: '/usync/',
	mode: 'production',
	plugins: [
		viteStaticCopy({
			targets: [
				{
					src: 'src/icons/svg/*.js',
					dest: 'icons',
				},
			],
		}),
	],
});
