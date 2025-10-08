// wwwroot/js/selfevaluation-form.js
document.addEventListener("DOMContentLoaded", function () {
    // element references
    const isSickEl = document.getElementById("SelfEvaluation_IsSick");
    const isNoShowEl = document.getElementById("SelfEvaluation_IsNoShow");
    const isOffWorkEl = document.getElementById("SelfEvaluation_IsOffWork");

    const showFormSection = document.getElementById("showFormSection");
    const sickReasonSection = document.getElementById("sickReasonSection");
    const noShowReasonSection = document.getElementById("noShowReasonSection");
    const offWorkReasonSection = document.getElementById("offWorkReasonSection");

    const hadBreakEl = document.getElementById("SelfEvaluation_HadBreak");
    const breakDurationSection = document.getElementById("breakDurationSection");
    const breakDurationEl = document.getElementById("SelfEvaluation_BreakDuration");

    const hadDiscomfortEl = document.getElementById("SelfEvaluation_HadDiscomfort");
    const discomfortDescriptionSection = document.getElementById("discomfortDescriptionSection");

    const arrivalEl = document.getElementById("SelfEvaluation_ArrivalTime");
    const departureEl = document.getElementById("SelfEvaluation_DepartureTime");
    const totalHoursHidden = document.getElementById("SelfEvaluation_TotalHours");
    const totalHoursDisplay = document.getElementById("totalHoursDisplay");

    const aidEl = document.getElementById("SelfEvaluation_Aid");
    const aidDescriptionSection = document.getElementById("aidDescriptionSection");
    const aidDescriptionEl = document.getElementById("SelfEvaluation_AidDescription");

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

        const fields = sectionEl.querySelectorAll("input, select, textarea, button");
        fields.forEach(f => f.disabled = !visible);
    }

    function clearSectionInputs(sectionEl) {
        if (!sectionEl) return;
        const fields = sectionEl.querySelectorAll("input, select, textarea");
        fields.forEach(f => {
            if (f.type === "checkbox" || f.type === "radio") {
                f.checked = false;
            } else if (f.tagName.toLowerCase() === "select") {
                f.value = "";
            } else {
                f.value = "";
            }
        });
    }

    // toggle sick/noshow/offwork and form
    function toggleFormSection() {
        const sick = isSickEl && isSickEl.checked;
        const noShow = isNoShowEl && isNoShowEl.checked;
        const offWork = isOffWorkEl && isOffWorkEl.checked;

        // Gensidig udelukkelse
        if (this === isSickEl && sick) {
            if (isNoShowEl) isNoShowEl.checked = false;
            if (isOffWorkEl) isOffWorkEl.checked = false;
        } else if (this === isNoShowEl && noShow) {
            if (isSickEl) isSickEl.checked = false;
            if (isOffWorkEl) isOffWorkEl.checked = false;
        } else if (this === isOffWorkEl && offWork) {
            if (isSickEl) isSickEl.checked = false;
            if (isNoShowEl) isNoShowEl.checked = false;
        }

        const activeSick = isSickEl && isSickEl.checked;
        const activeNoShow = isNoShowEl && isNoShowEl.checked;
        const activeOffWork = isOffWorkEl && isOffWorkEl.checked;

        const shouldHideForm = activeSick || activeNoShow || activeOffWork;
        setSectionVisibility(showFormSection, !shouldHideForm);

        // SickReason kun synlig hvis IsSick
        setSectionVisibility(sickReasonSection, activeSick);
        if (!activeSick) clearSectionInputs(sickReasonSection);

        // NoShowReason kun synlig hvis IsNoShow
        setSectionVisibility(noShowReasonSection, activeNoShow);
        if (!activeNoShow) clearSectionInputs(noShowReasonSection);

        // OffWorkReason kun synlig hvis IsOffWork
        setSectionVisibility(offWorkReasonSection, activeOffWork);
        if (!activeOffWork) clearSectionInputs(offWorkReasonSection);

        if (shouldHideForm) {
            clearSectionInputs(showFormSection);
            if (aidEl) aidEl.value = "";
            if (aidDescriptionEl) aidDescriptionEl.value = "";
        }
    }

    function toggleBreak() {
        const hadBreak = hadBreakEl && hadBreakEl.checked;
        setSectionVisibility(breakDurationSection, hadBreak);
        if (!hadBreak && breakDurationEl) {
            breakDurationEl.value = "";
        }
        calculateTotalHours();
    }

    function toggleDiscomfort() {
        const hadDiscomfort = hadDiscomfortEl && hadDiscomfortEl.checked;
        setSectionVisibility(discomfortDescriptionSection, hadDiscomfort);
        if (!hadDiscomfort) {
            clearSectionInputs(discomfortDescriptionSection);
        }
    }

    function toggleAid() {
        if (!aidEl || !aidDescriptionSection) return;
        const value = aidEl.value;
        const needsDescription = (value === "Ja – hvilke?" || value === "Har brug for noget – hvad?");
        setSectionVisibility(aidDescriptionSection, needsDescription);

        if (!needsDescription && aidDescriptionEl) {
            aidDescriptionEl.value = "";
        }
    }

    // Calculate total hours = departure - arrival - break (minutes)
    function calculateTotalHours() {
        if (!arrivalEl || !departureEl) return;

        const arrival = parseTimeToMinutes(arrivalEl.value);
        const departure = parseTimeToMinutes(departureEl.value);
        const breakMinutes = breakDurationEl ? parseTimeToMinutes(breakDurationEl.value) : 0;

        if (arrival == null || departure == null) {
            totalHoursDisplay.value = "";
            if (totalHoursHidden) totalHoursHidden.value = "";
            return;
        }

        let diff = departure - arrival - (isNaN(breakMinutes) ? 0 : breakMinutes);

        if (diff <= 0) {
            totalHoursDisplay.value = "";
            if (totalHoursHidden) totalHoursHidden.value = "";
            return;
        }

        totalHoursDisplay.value = formatMinutesToHHMM(diff);
        if (totalHoursHidden) totalHoursHidden.value = formatMinutesToHHMMSS(diff);
    }

    // Safe wiring
    if (isSickEl) isSickEl.addEventListener("change", toggleFormSection);
    if (isNoShowEl) isNoShowEl.addEventListener("change", toggleFormSection);
    if (isOffWorkEl) isOffWorkEl.addEventListener("change", toggleFormSection);

    if (hadBreakEl) hadBreakEl.addEventListener("change", toggleBreak);
    if (hadDiscomfortEl) hadDiscomfortEl.addEventListener("change", toggleDiscomfort);
    if (aidEl) aidEl.addEventListener("change", toggleAid);

    if (arrivalEl) arrivalEl.addEventListener("input", calculateTotalHours);
    if (departureEl) departureEl.addEventListener("input", calculateTotalHours);
    if (breakDurationEl) breakDurationEl.addEventListener("input", calculateTotalHours);

    // Initialize on load
    toggleFormSection();
    toggleBreak();
    toggleDiscomfort();
    toggleAid();
    calculateTotalHours();
});