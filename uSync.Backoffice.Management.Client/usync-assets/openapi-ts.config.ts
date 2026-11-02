import { defineConfig } from '@hey-api/openapi-ts';
import { defaultPlugins } from '@hey-api/openapi-ts';

export default defineConfig({
	input: 'http://localhost:16903/umbraco/swagger/uSync/swagger.json',
	output: {
		path: 'src/api',
		postProcess: ['prettier'],
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
			operations: {
				strategy: 'byTags',
				container: 'class',
				containerName: '{{name}}Service',
			},
		},
	],
});
