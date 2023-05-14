console.log("Loading script...");

const script = document.createElement("script");
script.src = "http://localhost:5173/dist/flowcomment-dev.iife.js";
script.type = "module";
document.head.appendChild(script);

console.log("Script injected");
