import { createEffect, createResource, createSignal, Show } from "solid-js";
import { MenuContainer, MenuItem, MenuItemButton } from "./Menu";
import { commentEnabledState, commentOffsetState } from "./Main";
import { mediaSourceIdState } from "../mediaSourceIdState";
import { error, getVideoId, setVideoId } from "../utils";
import { commentCanvas } from ".";
import { Portal } from "solid-js/web";

export function MenuButton({ disabled }: { disabled: boolean }) {
  const [open, setOpen] = createSignal(false);
  const [showComment, setShowComment] = commentEnabledState;
  const [offset, setOffset] = commentOffsetState;
  const [mediaSourceId] = mediaSourceIdState;

  const [videoId] = createResource(mediaSourceId, getVideoId);

  createEffect(() => {
    console.log({ videoId: videoId() });
  });

  createEffect(() => {
    disabled && setOpen(false);
  });
  return (
    <div style={{ position: "relative" }}>
      <button
        class="btnVideoOsdSettings autoSize paper-icon-button-light"
        onClick={() => setOpen(!open())}
        disabled={disabled}
      >
        <span class="material-icons chat">
        </span>
      </button>
      <Portal>
        <div
          style={{
            display: open() ? "" : "none",
            position: "fixed",
            bottom: "50px",
            right: "30px",
          }}
        >
          <MenuContainer>
            <Show when={videoId()}>
              <MenuItem>
                {videoId()}
              </MenuItem>
            </Show>
            <MenuItem>
              <div
                style={{
                  "display": "flex",
                  "gap": "3",
                  "flex-direction": "row",
                  "justify-content": "space-between",
                }}
              >
                <label>Enabled</label>
                <input
                  type="checkbox"
                  checked={showComment()}
                  onChange={() => setShowComment((prev) => !prev)}
                />
              </div>
            </MenuItem>
            <MenuItemButton
              onClick={async () => {
                const input = window.prompt("Enter videoid", "");
                if (!input) return;

                let videoId = "";
                try {
                  const url = new URL(input);
                  let v = url.pathname.split("/").pop();
                  if (!v) {
                    throw Error("No videoid found");
                  }
                  videoId = v;
                } catch {
                  videoId = input;
                }

                try {
                  await setVideoId(mediaSourceId(), videoId);
                  await commentCanvas.fetchAndDrawComments(mediaSourceId());
                } catch (e) {
                  error(e);
                  return;
                }
              }}
            >
              Set niconico videoid
            </MenuItemButton>
            <MenuItem>
              <div
                style={{
                  "display": "flex",
                  "gap": "3",
                  "flex-direction": "row",
                  "justify-content": "space-between",
                }}
              >
                <label>Offset</label>
                <input
                  type="number"
                  value={offset()}
                  onChange={(e) => {
                    setOffset(Number(e.currentTarget.value));
                  }}
                />
              </div>
            </MenuItem>
          </MenuContainer>
        </div>
      </Portal>
    </div>
  );
}
