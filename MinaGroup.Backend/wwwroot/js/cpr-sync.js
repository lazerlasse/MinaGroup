// cpr-sync.js
// Genbrugeligt script til at synkronisere et CPR-displayfelt
// ud fra en <select> med data-cpr attributter.
//
// Forventet HTML:
// <select id="UserSelect"> <option data-cpr="xxxxxx-xxxx"> … </option> </select>
// <input id="SelectedUserCpr" readonly />

document.addEventListener('DOMContentLoaded', function () {
    const userSelect = document.getElementById('UserSelect');
    const cprInput = document.getElementById('SelectedUserCpr');

    // Hvis elementerne ikke findes, gør vi ikke noget (tillader genbrug flere steder)
    if (!userSelect || !cprInput) return;

    function updateCpr() {
        const opt = userSelect.selectedOptions[0];
        const cpr = opt ? opt.getAttribute('data-cpr') : '';
        cprInput.value = cpr || '';
    }

    userSelect.addEventListener('change', updateCpr);

    // Init-run ved load
    updateCpr();
});