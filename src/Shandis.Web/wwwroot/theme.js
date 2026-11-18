(() => {
    const storageKey = "shandis-theme";
    const applyTheme = theme => {
        const selected = theme === "dark" ? "dark" : "light";
        document.documentElement.dataset.theme = selected;
        document.documentElement.style.colorScheme = selected;
        return selected;
    };

    const saved = localStorage.getItem(storageKey);
    applyTheme(saved || "light");

    window.shandisTheme = {
        toggle: () => {
            const current = document.documentElement.dataset.theme || "light";
            const next = current === "dark" ? "light" : "dark";
            localStorage.setItem(storageKey, next);
            return applyTheme(next);
        },
        current: () => document.documentElement.dataset.theme || "light"
    };
})();
