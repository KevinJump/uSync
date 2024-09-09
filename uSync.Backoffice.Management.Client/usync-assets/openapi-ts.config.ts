import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
	client: 'legacy/fetch',
	input: 'http://localhost:16855/umbraco/swagger/uSync/swagger.json',
	output: {
		format: 'prettier',
		path: 'src/api',
	},
	services: {
		asClass: true,
	},
	schemas: {
		type: 'json',
	},
	types: {
		enums: 'javascript',
	},
});
