(function () {
    const badges = Array.from(document.querySelectorAll(".se-upload-badge"));
    if (badges.length === 0) return;

    const ids = badges
        .map(b => parseInt(b.dataset.seId, 10))
        .filter(n => !isNaN(n));

    if (ids.length === 0) return;

    function badgeClassForState(state) {
        switch (state) {
            case "Succeeded": return "bg-success";
            case "Failed": return "bg-danger";
            case "Processing": return "bg-primary";
            case "Retrying": return "bg-warning text-dark";
            case "Queued": return "bg-secondary";
            case "Skipped": return "bg-info text-dark";
            case "Cancelled": return "bg-dark";
            case "None": return "bg-light text-dark";
            default: return "bg-secondary";
        }
    }

    function labelForState(state) {
        switch (state) {
            case "Succeeded": return "Uploadet";
            case "Failed": return "Fejlet";
            case "Processing": return "Uploader…";
            case "Retrying": return "Retry…";
            case "Queued": return "I kø";
            case "Skipped": return "Sprunget over";
            case "Cancelled": return "Annulleret";
            case "None": return "Ingen job";
            default: return state;
        }
    }

    async function fetchAndRender() {
        // Bevar nuværende path + query, og tilføj kun handler + ids
        const url = new URL(window.location.href);
        url.searchParams.set("handler", "UploadStatuses");

        // Ryd gamle ids (hvis scriptet kører flere gange)
        url.searchParams.delete("ids");
        ids.forEach(id => url.searchParams.append("ids", String(id)));

        const res = await fetch(url.toString(), { cache: "no-store" });
        if (!res.ok) return;

        const data = await res.json();

        data.forEach(item => {
            const badge = document.querySelector(`.se-upload-badge[data-se-id="${item.selfEvaluationId}"]`);
            if (!badge) return;

            badge.className = "badge se-upload-badge";
            badge.classList.add(...badgeClassForState(item.state).split(" "));
            badge.textContent = labelForState(item.state);
            badge.title = item.message || "";
        });

        const endStates = ["Succeeded", "Failed", "Cancelled", "None", "Skipped"];
        const allDone = data.every(x => endStates.includes(x.state));
        if (!allDone) setTimeout(fetchAndRender, 2000);
    }

    fetchAndRender();
})();