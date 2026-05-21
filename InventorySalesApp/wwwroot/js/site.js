// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', () => {
    const forms = document.querySelectorAll('.place-order-form');

    forms.forEach((form) => {
        form.addEventListener('submit', (event) => {
            if (!form.checkValidity()) {
                return;
            }

            const submitButton = form.querySelector('.submit-order-button');
            const submitText = submitButton?.querySelector('.submit-button-text');

            if (!submitButton || submitButton.disabled) {
                event.preventDefault();
                return;
            }

            submitButton.disabled = true;

            if (submitText) {
                submitText.textContent = 'Submitting...';
            }

            submitButton.insertAdjacentHTML(
                'afterbegin',
                '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>'
            );
        });
    });

    const counter = document.getElementById('refresh-counter');

    if (counter) {
        let remaining = 30;

        setInterval(() => {
            remaining--;
            counter.textContent = remaining;

            if (remaining <= 0) {
                location.reload();
            }
        }, 1000);
    }
});
