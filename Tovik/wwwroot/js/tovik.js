//document.addEventListener('click', (e) => {
//    var target = e.target;

//    if (target.closest('.actions-btn')) {
//        var id = target.getAttribute('data-id');
//        var actionsId = id + '-actions';
//        var menu = document.getElementById(actionsId);

//        console.log('Actions button clicked for ID:', id);

//        if (menu) {
//            menu.classList.toggle('show');
//        }
//    }
//});

function hideAllDomainActions() {
    var menus = document.querySelectorAll('.actions-menu');
    menus.forEach(menu => {
        menu.classList.remove('show');
    });
}

function goBack() {
    window.history.back();
}