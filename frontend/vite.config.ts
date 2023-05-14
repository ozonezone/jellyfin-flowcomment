import { resolve } from "path";
import { defineConfig } from "vite";
import solidPlugin from "vite-plugin-solid";

export default defineConfig(({ mode }) => ({
  build: {
    lib: {
      entry: resolve(__dirname, "src/index.ts"),
      name: "flowcomment",
      fileName: mode === "dev" ? "flowcomment-dev" : "flowcomment",
      formats: ["iife"],
    },
    minify: !(mode === "dev"),
  },
  plugins: [solidPlugin()],
}));
