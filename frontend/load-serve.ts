console.log("Loading script...");

const script = document.createElement("script");
script.src = "http://localhost:10001/bundle.js";
script.type = "module";
document.head.appendChild(script);

console.log("Script injected");
