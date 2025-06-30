import TovikTranslateNode from './TovikTranslateNode.js';
import TovikLangSelectElement from './TovikLangSelectElement.js';
import TovikTranslateElement from './TovikTranslateElement.js';
import SparcEngine from './SparcEngine.js';
// do an initial ping to Sparc Engine to set the cookie
SparcEngine.hi().then(() => {
    customElements.define('tovik-t', TovikTranslateNode);
    customElements.define('tovik-language', TovikLangSelectElement);
    customElements.define('tovik', TovikTranslateElement);
    // If the document does not have a <kori-translate> element, create one and point it to the body
    if (!document.querySelector('tovik')) {
        var bodyElement = document.createElement('tovik');
        bodyElement.setAttribute('for', 'body');
        document.body.appendChild(bodyElement);
    }
});
//# sourceMappingURL=tovik.js.map