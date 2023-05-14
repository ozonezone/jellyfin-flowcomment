import { createSignal } from "solid-js";

export const mediaSourceIdState = createSignal("");

/// This function should be called only once.
export function initMediaSourceIdWatcher() {
  const { fetch: originalFetch } = window;

  const setMediaSourceId = mediaSourceIdState[1];
  window.fetch = async (...args) => {
    let [resource, config] = args;

    // @ts-ignore
    let url = new URL(resource);
    let urlParts = url.pathname.split("/");
    let isPlaybackInfo = urlParts.pop() == "PlaybackInfo";

    const response = await originalFetch(resource, config);

    if (isPlaybackInfo) {
      let mediaSourceId = new URLSearchParams(url.search).get("MediaSourceId");
      mediaSourceId = mediaSourceId ? mediaSourceId : urlParts.pop()!;
      setMediaSourceId(mediaSourceId);
    }

    return response;
  };
}
