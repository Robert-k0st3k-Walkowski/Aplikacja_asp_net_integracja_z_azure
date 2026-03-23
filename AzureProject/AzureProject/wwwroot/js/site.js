// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const fileInput = document.getElementById('imageFileInput');
    const form = document.getElementById('carForm');
    const marka = document.getElementById('MarkaInput');
    const model = document.getElementById('ModelInput');
    const kolor = document.getElementById('KolorInput');
    const nadwozie = document.getElementById('NadwozieSelect');
    /*
    const imgHidden = document.getElementById('ImageFilename');
    const uriHidden = document.getElementById('ImageUri');
    const livePreview = document.getElementById('livePreview');
    */

    if (!fileInput) return;

    fileInput.addEventListener('change', async (e) => {
        if (!fileInput.files || fileInput.files.length === 0) return;
        const tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';

        const data = new FormData();
        data.append('imageFile', fileInput.files[0]);

        try {
            const res = await fetch('/Home/AnalyzeImage', {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: data
            });
            const j = await res.json();
            if (!j.success) return;

            if (j.marka && (!marka.value || marka.value.trim().length === 0)) marka.value = j.marka;
            if (j.model && (!model.value || model.value.trim().length === 0)) model.value = j.model;
            if (j.kolor && (!kolor.value || kolor.value.trim().length === 0)) kolor.value = j.kolor;
            if (j.rodzajNadwozia && nadwozie) {
                for (const opt of nadwozie.options) {
                    if (opt.value.toLowerCase() === j.rodzajNadwozia.toLowerCase()) {
                        nadwozie.value = opt.value;
                        break;
                    }
                }
            }

            /*
            if (j.imageFilename) imgHidden.value = j.imageFilename;
            if (j.imageUri) {
                uriHidden.value = j.imageUri;
                livePreview.src = j.imageUri;
                livePreview.style.display = 'block';
            }
            */
        } catch (err) {
            console.warn('AnalyzeImage failed', err);
        }
    });
})();