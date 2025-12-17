const pageLink = document.querySelectorAll('[data-page-number]');

const paginationURL = new URL(window.location.href);

pageLink.forEach(link => {
    link.addEventListener('click', (e) => {
        e.preventDefault()
        const page = link.getAttribute('data-page-number').trim();
        paginationURL.searchParams.set('page', page);
        window.location.href = paginationURL.href;
    });
});
