import serve from "rollup-plugin-serve";
import { swc } from "rollup-plugin-swc3";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";

export default [{
  input: "src/index.ts",
  output: {
    file: "./dist/bundle.js",
    format: "cjs",
  },
  plugins: [
    nodeResolve(),
    swc({
      include: /\.[mc]?[jt]sx?$/,
      exclude: /node_modules/,
      tsconfig: "tsconfig.json",
      jsc: {},
    }),
    commonjs({
      include: [
        "node_modules/**",
      ],
      exclude: [
        "node_modules/process-es6/**",
      ],
    }),
    serve({
      open: false,
      contentBase: "dist",
      headers: {
        "Access-Control-Allow-Origin": "*",
      },
    }),
  ],
}, {
  input: "./load-serve.ts",
  output: {
    file: "../Jellyfin.Plugin.FlowComment/Api/flowcomment.js",
    format: "cjs",
  },
  plugins: [
    nodeResolve(),
    swc({
      include: /\.[mc]?[jt]sx?$/,
      exclude: /node_modules/,
      tsconfig: "tsconfig.json",
      jsc: {},
    }),
    commonjs({
      include: [
        "node_modules/**",
      ],
      exclude: [
        "node_modules/process-es6/**",
      ],
    }),
  ],
}];
