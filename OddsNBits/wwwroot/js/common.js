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

window.addEventListener("load", () => {
    console.log("Qr code stuff");
    const uri = document.getElementById("qrCodeData").getAttribute('data-url');
    new QRCode(document.getElementById("qrCode"),
        {
            text: uri,
            width: 150,
            height: 150
        });
});