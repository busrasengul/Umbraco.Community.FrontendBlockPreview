import { defineConfig } from "vite";

export default defineConfig({
  build: {
    lib: {
      // The manifest is registered from C# (FrontendBlockPreviewManifestReader) and points at this file
      entry: "src/block-preview.element.ts",
      formats: ["es"],
      fileName: "block-preview",
    },
    outDir: "../wwwroot/App_Plugins/FrontendBlockPreview",
    emptyOutDir: true,
    sourcemap: true,
    rollupOptions: {
      external: [/^@umbraco/],
    },
  },
});
