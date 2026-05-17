// Small JS interop helpers used by the Encyclopedia client.
window.encyclopedia = {
    downloadText(filename, text) {
        const blob = new Blob([text], { type: 'application/json' });
        const url  = URL.createObjectURL(blob);
        const a    = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    },

    // ---- Account persistence (browser-side only) -------------------------
    // The account JSON lives in localStorage so a page refresh doesn't sign
    // the user out. The server never reads or writes this storage.
    saveAccount(json) {
        try { localStorage.setItem('encyclopedia.account', json); return true; }
        catch (e) { console.warn('localStorage write failed', e); return false; }
    },
    loadAccount() {
        try { return localStorage.getItem('encyclopedia.account'); }
        catch (e) { console.warn('localStorage read failed', e); return null; }
    },
    clearAccount() {
        try { localStorage.removeItem('encyclopedia.account'); return true; }
        catch (e) { return false; }
    },
};
