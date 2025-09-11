// wwwroot/js/selfevaluation-form.js
document.addEventListener("DOMContentLoaded", function () {
    // element references
    const isSickEl = document.getElementById("SelfEvaluation_IsSick");
    const notSickSection = document.getElementById("notSickSection");

    const hadBreakEl = document.getElementById("SelfEvaluation_HadBreak");
    const breakDurationSection = document.getElementById("breakDurationSection");
    const breakDurationEl = document.getElementById("SelfEvaluation_BreakDuration");

    const hadDiscomfortEl = document.getElementById("SelfEvaluation_HadDiscomfort");
    const discomfortDescriptionSection = document.getElementById("discomfortDescriptionSection");

    const arrivalEl = document.getElementById("SelfEvaluation_ArrivalTime");
    const departureEl = document.getElementById("SelfEvaluation_DepartureTime");
    const totalHoursHidden = document.getElementById("SelfEvaluation_TotalHours"); // hidden input (asp-for)
    const totalHoursDisplay = document.getElementById("totalHoursDisplay");

    // Utility: parse "HH:mm" or "HH:mm:ss" -> minutes (integer) or null
    function parseTimeToMinutes(timeStr) {
        if (!timeStr) return null;
        const parts = timeStr.split(':');
        if (parts.length < 2) return null;
        const h = parseInt(parts[0], 10);
        const m = parseInt(parts[1], 10);
        if (isNaN(h) || isNaN(m)) return null;
        return h * 60 + m;
    }

    function formatMinutesToHHMMSS(mins) {
        if (mins == null || isNaN(mins)) return "";
        const sign = mins < 0 ? "-" : "";
        const abs = Math.abs(mins);
        const h = Math.floor(abs / 60);
        const m = abs % 60;
        const hh = String(h).padStart(2, "0");
        const mm = String(m).padStart(2, "0");
        return `${sign}${hh}:${mm}:00`;
    }

    function formatMinutesToHHMM(mins) {
        if (mins == null || isNaN(mins)) return "";
        const sign = mins < 0 ? "-" : "";
        const abs = Math.abs(mins);
        const h = Math.floor(abs / 60);
        const m = abs % 60;
        return `${sign}${String(h).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
    }

    // Toggle sections (hide/show + disable inputs inside)
    function setSectionVisibility(sectionEl, visible) {
        if (!sectionEl) return;
        sectionEl.style.display = visible ? "block" : "none";

        // disable/enable children so they don't post when hidden
        const fields = sectionEl.querySelectorAll("input, select, textarea, button");
        fields.forEach(f => {
            // keep evaluation date and IsSick outside this section so safe to disable them here
            f.disabled = !visible;
        });
    }

    function toggleSick() {
        const sick = isSickEl && isSickEl.checked;
        setSectionVisibility(notSickSection, !sick);
    }

    function toggleBreak() {
        const hadBreak = hadBreakEl && hadBreakEl.checked;
        setSectionVisibility(breakDurationSection, hadBreak);
        // if we're hiding break, also clear the value to avoid leftover values
        if (!hadBreak && breakDurationEl) {
            breakDurationEl.value = "";
        }
        calculateTotalHours();
    }

    function toggleDiscomfort() {
        const hadDiscomfort = hadDiscomfortEl && hadDiscomfortEl.checked;
        setSectionVisibility(discomfortDescriptionSection, hadDiscomfort);
        if (!hadDiscomfort) {
            const el = discomfortDescriptionSection ? discomfortDescriptionSection.querySelector("input, textarea") : null;
            if (el) el.value = "";
        }
    }

    // Calculate total hours = departure - arrival - break (minutes)
    function calculateTotalHours() {
        if (!arrivalEl || !departureEl) return;

        const arrival = parseTimeToMinutes(arrivalEl.value);
        const departure = parseTimeToMinutes(departureEl.value);
        const breakMinutes = breakDurationEl ? parseTimeToMinutes(breakDurationEl.value) : 0;

        if (arrival == null || departure == null) {
            // Not enough data
            totalHoursDisplay.value = "";
            if (totalHoursHidden) totalHoursHidden.value = "";
            return;
        }

        // If departure is earlier than arrival assume next day? currently treat as negative -> empty
        let diff = departure - arrival - (isNaN(breakMinutes) ? 0 : breakMinutes);

        if (diff <= 0) {
            totalHoursDisplay.value = "";
            if (totalHoursHidden) totalHoursHidden.value = "";
            return;
        }

        totalHoursDisplay.value = formatMinutesToHHMM(diff);
        if (totalHoursHidden) totalHoursHidden.value = formatMinutesToHHMMSS(diff);
    }

    // Safe wiring (some elements might not exist)
    if (isSickEl) {
        isSickEl.addEventListener("change", toggleSick);
    }
    if (hadBreakEl) {
        hadBreakEl.addEventListener("change", toggleBreak);
    }
    if (hadDiscomfortEl) {
        hadDiscomfortEl.addEventListener("change", toggleDiscomfort);
    }
    if (arrivalEl) arrivalEl.addEventListener("input", calculateTotalHours);
    if (departureEl) departureEl.addEventListener("input", calculateTotalHours);
    if (breakDurationEl) breakDurationEl.addEventListener("input", calculateTotalHours);

    // Initialize on load (keeps server-side posted values showing when page reloads with validation errors)
    toggleSick();
    toggleBreak();
    toggleDiscomfort();
    calculateTotalHours();
});
