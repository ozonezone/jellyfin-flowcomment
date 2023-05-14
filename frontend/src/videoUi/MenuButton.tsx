import { createEffect, createSignal, For } from "solid-js";
import { MenuContainer, MenuItem, MenuItemButton } from "./Menu";
import { commentEnabledState } from "./Main";
import { mediaSourceIdState } from "../mediaSourceIdState";
import { error, setVideoId } from "../utils";
import { commentCanvas } from ".";

export function MenuButton({ disabled }: { disabled: boolean }) {
  const [open, setOpen] = createSignal(false);
  const [showComment, setShowComment] = commentEnabledState;
  const [mediaSourceId] = mediaSourceIdState;

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
      <div
        style={{
          display: open() ? "" : "none",
          position: "fixed",
          bottom: "50px",
          right: "30px",
        }}
      >
        <MenuContainer>
          <MenuItem>
            <div
              style={{
                "display": "flex",
                "gap": "3",
                "flex-direction": "row",
                "justify-content": "space-between",
              }}
            >
              <p>Enabled</p>
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
        </MenuContainer>
      </div>
    </div>
  );
}
