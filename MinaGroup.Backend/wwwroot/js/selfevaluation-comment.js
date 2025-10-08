document.addEventListener("DOMContentLoaded", function () {
    const approveBtn = document.getElementById("approveBtn");
    const confirmApproveBtn = document.getElementById("confirmApproveBtn");

    const commentField = document.getElementById("SelfEvaluation_CommentFromLeader");
    const sickReasonField = document.getElementById("SelfEvaluation_SickReason");
    const noShowReasonField = document.getElementById("SelfEvaluation_NoShowReason");
    const offWorkReasonField = document.getElementById("SelfEvaluation_OffWorkReason");

    const isSickEl = document.getElementById("SelfEvaluation_IsSick");
    const isNoShowEl = document.getElementById("SelfEvaluation_IsNoShow");
    const isOffWorkEl = document.getElementById("SelfEvaluation_IsOffWork");

    const sickSection = document.getElementById("sickReasonSection");
    const noShowSection = document.getElementById("noShowReasonSection");
    const offWorkSection = document.getElementById("offWorkReasonSection");

    const form = approveBtn?.closest("form");
    const approveModalEl = document.getElementById("approveModal");
    if (!approveBtn || !confirmApproveBtn || !form || !approveModalEl) return;

    const approveModal = new bootstrap.Modal(approveModalEl);

    function updateVisibility() {
        const sick = isSickEl?.checked;
        const noShow = isNoShowEl?.checked;
        const offWork = isOffWorkEl?.checked;

        // Skjul alle årsagsfelter som standard
        sickSection.style.display = "none";
        noShowSection.style.display = "none";
        offWorkSection.style.display = "none";

        // Deaktiver alle felter som standard
        sickReasonField.disabled = true;
        noShowReasonField.disabled = true;
        offWorkReasonField.disabled = true;

        // Aktivér kun relevant årsagsfelt
        if (sick) {
            sickSection.style.display = "block";
            sickReasonField.disabled = false;
            commentField.disabled = true;
        } else if (noShow) {
            noShowSection.style.display = "block";
            noShowReasonField.disabled = false;
            commentField.disabled = true;
        } else if (offWork) {
            offWorkSection.style.display = "block";
            offWorkReasonField.disabled = false;
            commentField.disabled = true;
        } else {
            // Ingen af de tre markeret: kun commentFromLeader er aktiv
            commentField.disabled = false;
        }
    }

    function validateBeforeSubmit() {
        const sick = isSickEl?.checked;
        const noShow = isNoShowEl?.checked;
        const offWork = isOffWorkEl?.checked;

        // Valider kun aktivt felt
        if (sick && !sickReasonField.value.trim()) return false;
        if (noShow && !noShowReasonField.value.trim()) return false;
        if (offWork && !offWorkReasonField.value.trim()) return false;

        // Valider kun CommentFromLeader hvis ingen af de tre er markeret
        if (!sick && !noShow && !offWork && !commentField.value.trim()) return false;

        return true;
    }

    approveBtn.addEventListener("click", function (e) {
        if (!validateBeforeSubmit()) {
            e.preventDefault();
            approveModal.show();
        }
    });

    confirmApproveBtn.addEventListener("click", function () {
        approveModal.hide();
        form.submit();
    });

    // Init
    updateVisibility();

    // Opdater når nogen af checkboksene ændres
    [isSickEl, isNoShowEl, isOffWorkEl].forEach(el => el?.addEventListener("change", updateVisibility));
});