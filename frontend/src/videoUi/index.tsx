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
  drawInterval: null | number = null;

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
    if (this.drawInterval) clearInterval(this.drawInterval);
  }

  async fetchAndDrawComments(mediaSourceId: string): Promise<string> {
    if (this.canvas == null) this.canvas = this.createCanvas();
    if (this.drawInterval) clearInterval(this.drawInterval);
    if (this.niconicomments) this.niconicomments.clear();

    const comments = await fetchComments(mediaSourceId);

    this.niconicomments = new NiconiComments(this.canvas, comments.data, {
      format: "v1",
    });
    const videoElem = document.querySelector("video");
    this.drawInterval = setInterval(
      () => {
        this.niconicomments!.drawCanvas(
          Math.floor(videoElem!.currentTime * 100),
        );
      },
      10,
    );

    return comments.videoId;
  }

  disable() {
    if (this.drawInterval) clearInterval(this.drawInterval);
    if (this.niconicomments) this.niconicomments.clear();
  }

  enable() {
    if (!this.niconicomments) {
      info("No niconicomments instance");
      return;
    }
    const videoElem = document.querySelector("video");
    this.drawInterval = setInterval(
      () => {
        this.niconicomments!.drawCanvas(
          Math.floor(videoElem!.currentTime * 100),
        );
      },
      10,
    );
  }
}
