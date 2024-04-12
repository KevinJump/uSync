import { defineConfig } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';
import dts from 'vite-plugin-dts';

/**
 * library deployment vite.
 */
export default defineConfig({
	build: {
		lib: {
			entry: 'src/index.ts', // your web component source file
			name: 'usync',
			fileName: 'usync',
			formats: ['es'],
		},
		outDir: './dist',
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
		dts(),
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
