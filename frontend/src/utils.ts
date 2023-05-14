import { v1Thread } from "@xpadev-net/niconicomments";

let previous = "";
export function observerUrl(callback: (url: string) => any) {
  const observer = new MutationObserver(function () {
    if (previous !== location.href) {
      previous = location.href;
      callback(location.href);
    }
  });
  observer.observe(document.body, { childList: true, subtree: true });

  previous = location.href;
  callback(location.href);
}

export async function fetchComments(
  itemId: string,
): Promise<{ data: v1Thread[]; videoId: string }> {
  const res = await ApiClient.getJSON(
    await ApiClient.getUrl("FlowComment/FetchComments/" + itemId, true),
  );
  return res;
}

export async function setVideoId(
  itemId: string,
  videoId: string,
): Promise<v1Thread[]> {
  const res = await ApiClient.fetch({
    url: await ApiClient.getUrl(
      `FlowComment/SetNicoVideoId?itemId=${itemId}&videoId=${videoId}`,
      true,
    ),
    method: "POST",
  });
  return res;
}

export function debug(msg: any) {
  console.debug("[flowcomment] ", msg);
}

export function error(msg: any) {
  console.error("[flowcomment] ", msg);
}

export function info(msg: any) {
  console.info("[flowcomment] ", msg);
}
