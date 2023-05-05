import NiconiComments from "@xpadev-net/niconicomments";
import { debug, error, fetchComments, setVideoId } from "./utils";

let canvas: HTMLCanvasElement | null = null;
let nicocomments: null | NiconiComments = null;
let drawInterval: null | number = null;

export async function loadUi(embyAuth: string, mediaSource: string) {
  debug("Initializing ui");

  let videoControlElem: null | HTMLElement = null;
  document.querySelectorAll<HTMLElement>(".osdTimeText").forEach((element) => {
    if (element.offsetParent != null) {
      videoControlElem = element.parentElement;
    }
  });
  if (videoControlElem == null) {
    error("Failed to find control parent");
    return;
  }
  let videoControlElem2: HTMLElement = videoControlElem;

  const idInputOpenButton = document.createElement("button");
  idInputOpenButton.setAttribute("is", "paper-icon-button-light");
  idInputOpenButton.className =
    "btnVideoOsdSettings autoSize paper-icon-button-light";
  let icon = document.createElement("span");
  icon.className = "material-icons chat";
  idInputOpenButton.appendChild(icon);
  idInputOpenButton.onclick = async () => {
    const videoId = window.prompt("Enter videoid", "");
    if (!videoId) return;
    try {
      await setVideoId(mediaSource, videoId);
    } catch (e) {
      error(e);
      return;
    }
    await fetchAndDrawComments(mediaSource);
  };
  videoControlElem2.appendChild(idInputOpenButton);

  const canvasContainer = document.createElement("div");
  canvasContainer.style.position = "fixed";
  canvasContainer.style.top = "0";
  canvasContainer.style.bottom = "0";
  canvasContainer.style.left = "0";
  canvasContainer.style.right = "0";
  canvasContainer.style.display = "flex";
  canvasContainer.style.justifyContent = "center";
  canvasContainer.style.alignItems = "center";

  canvas = document.createElement("canvas");
  canvas.width = 1920;
  canvas.height = 1080;
  canvas.style.objectFit = "contain";
  canvas.style.maxWidth = "100%";
  canvas.style.maxHeight = "100%";

  canvasContainer.appendChild(canvas);
  document.querySelector(".videoPlayerContainer")!.appendChild(canvasContainer);

  await fetchAndDrawComments(mediaSource);
}

async function fetchAndDrawComments(mediaSource: string) {
  if (canvas == null) return;
  if (drawInterval) clearInterval(drawInterval);
  if (nicocomments) nicocomments = null;

  const comments = await fetchComments(mediaSource);

  nicocomments = new NiconiComments(canvas!, comments, {
    format: "v1",
  });
  const videoElem = document.querySelector("video");
  drawInterval = setInterval(
    () => {
      nicocomments!.drawCanvas(
        Math.floor(videoElem!.currentTime * 100),
      );
    },
    10,
  );
}

export function unloadUi() {
  debug("Unloading ui");

  if (canvas != null) {
    canvas.remove();
    canvas = null;
  }

  if (drawInterval) clearInterval(drawInterval);
  if (nicocomments) nicocomments = null;
}
