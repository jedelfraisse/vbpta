// Backs GlobalSettings.razor's logo-template box editor. The box a user
// drags is defined as a percentage of the template <img>'s own rendered
// size, so it stays correct no matter what CSS width the preview happens
// to be shown at — this is the only piece Blazor can't do without JS,
// since getBoundingClientRect() isn't exposed to C#.
export function getRect(el) {
    const rect = el.getBoundingClientRect();
    return { width: rect.width, height: rect.height };
}
