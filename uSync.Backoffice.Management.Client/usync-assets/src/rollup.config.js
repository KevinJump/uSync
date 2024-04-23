import commonjs from '@rollup/plugin-commonjs';
import { format } from 'path';
import esbuild from 'rollup-plugin-esbuild';

const api = {
	input: './src/api/index.ts',
	output: {
		dir: './lib-cms/api',
		format: 'es',
	},
	plugins: [commonjs(), esbuild({ minify: true, sourceMap: true })],
};

const signalR = {
	input: './node_modules/@microsoft/signalr/dist/browser/signalr.js',
	output: {
		dir: './lib-cms/external/signalr',
		format: 'ex',
	},
};

export default [api, signalR];
