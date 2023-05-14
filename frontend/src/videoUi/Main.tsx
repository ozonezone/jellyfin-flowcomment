import { createEffect, createSignal } from "solid-js";
import { info } from "../utils";
import { mediaSourceIdState } from "../mediaSourceIdState";
import { MenuButton } from "./MenuButton";
import { commentCanvas } from ".";

export const commentEnabledState = createSignal(true);
export const commentAvailableState = createSignal(false);

export function Main() {
  const [mediaSourceId] = mediaSourceIdState;
  const [showComment] = commentEnabledState;
  const [loading, setLoading] = createSignal(false);

  createEffect(async () => {
    commentCanvas.disable();
    if (showComment()) {
      setLoading(true);
      await commentCanvas.fetchAndDrawComments(mediaSourceId());
      setLoading(false);
    }
  });

  createEffect(() => {
    if (showComment()) {
      commentCanvas.enable();
    } else {
      commentCanvas.disable();
    }
  });

  return (
    <>
      <MenuButton disabled={loading()} />
    </>
  );
}
