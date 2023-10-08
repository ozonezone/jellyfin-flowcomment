import { render } from "solid-js/web";
import { Main } from "./Main";
import { fetchComments, info } from "../utils";
import NiconiComments from "@xpadev-net/niconicomments";

export let commentCanvas: CommentCanvas = null!;

export class VideoUi {
  destroySolid;
  constructor(
    controlParent: HTMLElement,
    videoContainer: HTMLElement,
  ) {
    commentCanvas = new CommentCanvas(videoContainer);
    this.destroySolid = render(() => (
      <div id="solid-root">
        <Main />
      </div>
    ), controlParent);

    info("mounted videoui");
  }
  destroy() {
    this.destroySolid();
    commentCanvas.destroy();

    info("unmounted videoui");
  }
}

class CommentCanvas {
  container: HTMLElement;
  canvas: null | HTMLCanvasElement = null;
  niconicomments: null | NiconiComments = null;
  animationFrame: null | number = null;
  offset = 0;

  previousTime = 0;
  indexNow = 0;

  constructor(container: HTMLElement) {
    this.container = container;
  }
  createCanvas() {
    const canvasContainer = document.createElement("div");
    canvasContainer.style.position = "fixed";
    canvasContainer.style.top = "0";
    canvasContainer.style.bottom = "0";
    canvasContainer.style.left = "0";
    canvasContainer.style.right = "0";
    canvasContainer.style.display = "flex";
    canvasContainer.style.justifyContent = "center";
    canvasContainer.style.alignItems = "center";

    const canvas = document.createElement("canvas");
    canvas.width = 1920;
    canvas.height = 1080;
    canvas.style.objectFit = "contain";
    canvas.style.maxWidth = "100%";
    canvas.style.maxHeight = "100%";

    canvasContainer.appendChild(canvas);
    this.container.appendChild(canvasContainer);

    return canvas;
  }
  destroy() {
    if (this.canvas) {
      this.canvas.remove();
    }
    if (this.animationFrame) cancelAnimationFrame(this.animationFrame);
  }

  async fetchAndDrawComments(mediaSourceId: string): Promise<string> {
    if (this.canvas == null) this.canvas = this.createCanvas();
    if (this.animationFrame) cancelAnimationFrame(this.animationFrame);
    if (this.niconicomments) this.niconicomments.clear();

    const comments = await fetchComments(mediaSourceId);

    this.niconicomments = new NiconiComments(this.canvas, comments.data, {
      format: "v1",
    });

    this.startComment();

    return comments.videoId;
  }

  startComment() {
    const videoElem = document.querySelector("video");

    this.previousTime = videoElem!.currentTime;
    this.indexNow = performance.now();

    const frame = () => {
      if (!videoElem!.paused) {
        let delta = 0;
        if (videoElem!.currentTime == this.previousTime) {
          delta = performance.now() - this.indexNow;
        } else {
          this.previousTime = videoElem!.currentTime;
          this.indexNow = performance.now();
        }

        this.niconicomments!.drawCanvas(
          Math.round(
            videoElem!.currentTime * 100 + delta / 10 + this.offset * 100,
          ),
        );
      }
      this.animationFrame = requestAnimationFrame(frame);
    };
    this.animationFrame = requestAnimationFrame(frame);
  }

  disable() {
    if (this.animationFrame) cancelAnimationFrame(this.animationFrame);
    if (this.niconicomments) this.niconicomments.clear();
  }

  enable() {
    if (!this.niconicomments) {
      info("No niconicomments instance");
      return;
    }
    this.startComment();
  }
}
