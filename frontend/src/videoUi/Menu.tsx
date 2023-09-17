import { JSX } from "solid-js/jsx-runtime";

export function MenuContainer({ children }: { children: JSX.Element }) {
  return (
    <div
      class="focuscontainer dialog"
      style="animation: 140ms ease-out 0s 1 normal both running scaleup; position: relative;"
    >
      <div class="actionSheetContent">
        <div class="actionSheetScroller scrollY" style="">
          {children}
        </div>
      </div>
    </div>
  );
}

type MenuButtonProps = {
  children: JSX.Element;
  onClick: () => any;
};
export function MenuItemButton(props: MenuButtonProps) {
  return (
    <button
      type="button"
      class="listItem listItem-button actionSheetMenuItem emby-button"
      onClick={props.onClick}
    >
      {props.children}
    </button>
  );
}

type MenuItemProps = {
  children: JSX.Element;
};
export function MenuItem(props: MenuItemProps) {
  return (
    <div class="listItem listItem-button actionSheetMenuItem emby-button">
      {props.children}
    </div>
  );
}
