import { defineConfig } from '@hey-api/openapi-ts';
import { defaultPlugins } from '@hey-api/openapi-ts';

export default defineConfig({
	input: 'http://localhost:28580/umbraco/swagger/uSync/swagger.json',
	output: {
		format: 'prettier',
		path: 'src/api',
	},
	plugins: [
		...defaultPlugins,
		{
			name: '@hey-api/client-fetch',
			exportFromIndex: true,
			throwOnError: true,
		},
		{
			name: '@hey-api/typescript',
			enums: 'typescript',
			readOnlyWriteOnlyBehavior: 'off',
		},
		{
			name: '@hey-api/sdk',
			asClass: true,
			classNameBuilder: '{{name}}Service',
		},
	],
});
