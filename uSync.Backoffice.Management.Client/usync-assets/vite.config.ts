import { defineConfig } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';

export default defineConfig({
	build: {
		lib: {
			entry: 'src/index.ts', // your web component source file
			formats: ['es'],
			fileName: 'index',
		},
		outDir: '../wwwroot/App_Plugins/uSync',
		emptyOutDir: true,
		sourcemap: true,
		rollupOptions: {
			external: [/^@umbraco/],
			onwarn: () => {},
		},
	},
	base: '/App_Plugins/uSync/',
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
