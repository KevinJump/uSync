import { defineConfig } from "vite";
import { checker } from "vite-plugin-checker";
import { nodeResolve } from "@rollup/plugin-node-resolve";

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
  plugins: [
    nodeResolve(),
    checker({
      typescript: true,
    }),
  ],
});
