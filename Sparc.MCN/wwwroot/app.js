function scrollToElement(id) {
    var content = document.getElementById(id);
    console.log(content);

    var show = setTimeout(checkClass(content, "show"), 1000);

    if (show) {
        content.scrollIntoView({ behavior: "smooth", block: "end", inline: "nearest" });
        console.log("scrolling");

    } else {
        return;
    }
}

function checkClass(elem, className) {
    if (elem.classList.contains(className)) {
        return true;
    } else {
        return false;
    }
}