import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      entry: "src/index.ts",
      formats: ["es"],
      fileName: "history",
    },
    outDir: "../wwwroot/App_Plugins/uSync.History",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/, /^@jumoo\/uSync/],
      onwarn: () => {},
    },
  },
  base: "/uSync.History/",
  mode: "production",
  plugins: [],
});
