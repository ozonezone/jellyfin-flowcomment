import { debug, info } from "./utils";
import { loadUi, unloadUi } from "./uiInit";

let mediaSourceId: null | string = null;
let embyAuthValue: null | string = null;

(async () => {
  info("loaded");
  let previousRoutePath = "";
  document.addEventListener("viewshow", function () {
    let currentRoutePath = Emby.Page.currentRouteInfo.path;

    info(currentRoutePath);

    if (currentRoutePath !== previousRoutePath) {
      previousRoutePath = currentRoutePath;
      if (currentRoutePath?.startsWith("/video")) {
        if (embyAuthValue && mediaSourceId) {
          loadUi(embyAuthValue!, mediaSourceId!);
        }
        mediaSourceId = null;
        embyAuthValue = null;
      } else {
        unloadUi();
      }
    }
  });
})();

const { fetch: originalFetch } = window;

window.fetch = async (...args) => {
  let [resource, config] = args;

  // @ts-ignore
  let url = new URL(resource);
  let urlParts = url.pathname.split("/");
  let isPlaybackInfo = urlParts.pop() == "PlaybackInfo";

  const response = await originalFetch(resource, config);

  if (isPlaybackInfo) {
    mediaSourceId = new URLSearchParams(url.search).get("MediaSourceId");
    // @ts-ignore
    mediaSourceId = mediaSourceId ? mediaSourceId : urlParts.pop();

    debug(`Found media source ID: ${mediaSourceId}`);

    // @ts-ignore
    let auth = config.headers["X-Emby-Authorization"];
    embyAuthValue = auth ? auth : "";
    debug(`Using Emby auth value: ${embyAuthValue}`);
  }

  return response;
};
