// Minimal JS interop for Blazor components
window.scrollToBottom = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) el.scrollTop = el.scrollHeight;
};
