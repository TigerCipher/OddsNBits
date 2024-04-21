document.addEventListener("DOMContentLoaded", _ => {
    const topnav = document.getElementsByClassName('oddsnav')[0];
    if (topnav) {
        window.onscroll = () => {
            if (window.scrollY > 50) {
                // add classes scrollednav py-0
                topnav.classList.add('scrollednav', 'py-0');
            } else {
                // remove classes scrollednav py-0
                topnav.classList.remove('scrollednav', 'py-0');
            }
        }
    }
});