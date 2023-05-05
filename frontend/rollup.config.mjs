import { swc } from "rollup-plugin-swc3";
import { nodeResolve } from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";

export default {
  input: "src/index.ts",
  output: {
    file: "../Jellyfin.Plugin.FlowComment/Api/flowcomment.js",
    format: "iife",
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
};
