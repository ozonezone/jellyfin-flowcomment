import { info } from "./utils";
import { VideoUi } from "./videoUi/index.jsx";
import { error } from "./utils.js";
import { initMediaSourceIdWatcher } from "./mediaSourceIdState";

(async () => {
  info("loaded");

  initMediaSourceIdWatcher();
  videoUiController();
})();

function videoUiController() {
  let previousRoutePath = "";
  let videoUi: null | VideoUi = null;

  document.addEventListener("viewshow", function () {
    let currentRoutePath = Emby.Page.currentRouteInfo.path;

    info(currentRoutePath);

    if (currentRoutePath !== previousRoutePath) {
      previousRoutePath = currentRoutePath;
      if (currentRoutePath?.startsWith("/video")) {
        let videoControlElem: null | HTMLElement = null;
        document.querySelectorAll<HTMLElement>(".osdTimeText").forEach(
          (element) => {
            if (element.offsetParent != null) {
              videoControlElem = element.parentElement;
            }
          },
        );
        if (videoControlElem == null) {
          error("Failed to find control parent");
          return;
        }

        const videoContainer: HTMLElement | null = document.querySelector(
          ".videoPlayerContainer",
        );
        if (videoContainer == null) {
          error("Failed to find video container");
          return;
        }

        videoUi = new VideoUi(videoControlElem as HTMLElement, videoContainer);
      } else {
        if (videoUi) {
          videoUi.destroy();
        }
      }
    }
  });
}
