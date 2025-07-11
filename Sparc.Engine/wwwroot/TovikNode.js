import db from './TovikDb.js';
import SparcEngine from './SparcEngine.js';
export default class TovikNode extends HTMLElement {
    #original;
    #originalLang;
    #translated;
    constructor() {
        super();
    }
    connectedCallback() {
        this.#original = this.textContent.trim();
        this.#originalLang = this.lang || document.documentElement.lang;
        document.addEventListener('tovik-language-changed', this.#languageChangedCallback);
        this.askForTranslation();
    }
    disconnectedCallback() {
        document.removeEventListener('tovik-language-changed', this.#languageChangedCallback);
    }
    #languageChangedCallback = (event) => {
        this.askForTranslation();
    };
    askForTranslation() {
        const hash = SparcEngine.idHash(this.#original);
        db.translations.get(hash).then(translation => {
            if (translation) {
                console.log('found!', this.#original);
                this.render(translation);
            }
            else {
                console.log('not found!', this.#original);
                this.classList.add('tovik-translating');
                SparcEngine.translate(this.#original, this.#originalLang)
                    .then(newTranslation => {
                    this.render(newTranslation);
                    db.translations.put(newTranslation);
                });
                this.classList.remove('tovik-translating');
            }
        });
    }
    render(translation) {
        this.#translated = translation.text;
        if (this.#translated) {
            this.textContent = this.#translated;
        }
        else {
            this.textContent = this.#original;
        }
    }
}
//# sourceMappingURL=TovikNode.js.map