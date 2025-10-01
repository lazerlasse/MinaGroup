document.addEventListener("DOMContentLoaded", function () {
    const approveBtn = document.getElementById("approveBtn");
    const confirmApproveBtn = document.getElementById("confirmApproveBtn");
    const commentField = document.getElementById("SelfEvaluation_CommentFromLeader");
    const sickReasonField = document.getElementById("SelfEvaluation_SickReason");
    const noShowReasonField = document.getElementById("SelfEvaluation_NoShowReason");

    const isSickEl = document.getElementById("SelfEvaluation_IsSick");
    const isNoShowEl = document.getElementById("SelfEvaluation_IsNoShow");

    const sickSection = document.getElementById("sickReasonSection");
    const noShowSection = document.getElementById("noShowReasonSection");
    const leaderSection = document.getElementById("commentFromLeaderSection");

    const form = approveBtn?.closest("form");
    const approveModalEl = document.getElementById("approveModal");
    if (!approveBtn || !confirmApproveBtn || !form || !approveModalEl) return;

    const approveModal = new bootstrap.Modal(approveModalEl);

    function updateVisibility() {
        const sick = isSickEl?.checked;
        const noShow = isNoShowEl?.checked;

        if (sick) {
            sickSection.style.display = "block";
            noShowSection.style.display = "none";
            leaderSection.style.display = "none";
            commentField.disabled = true;
            noShowReasonField.value = "";
            commentField.value = "";
        } else if (noShow) {
            sickSection.style.display = "none";
            noShowSection.style.display = "block";
            leaderSection.style.display = "none";
            commentField.disabled = true;
            sickReasonField.value = "";
            commentField.value = "";
        } else {
            sickSection.style.display = "none";
            noShowSection.style.display = "none";
            leaderSection.style.display = "block";
            commentField.disabled = false;
            sickReasonField.value = "";
            noShowReasonField.value = "";
        }
    }

    function validateBeforeSubmit() {
        const sick = isSickEl?.checked;
        const noShow = isNoShowEl?.checked;

        if (sick && !sickReasonField.value.trim()) return false;
        if (noShow && !noShowReasonField.value.trim()) return false;
        if (!sick && !noShow && !commentField.value.trim()) return false;

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

    updateVisibility();
});