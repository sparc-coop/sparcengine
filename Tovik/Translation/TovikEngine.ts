import MD5 from "./MD5.js";
import db from './TovikDb.js';

const baseUrl = window.location.href.includes('localhost')
    ? 'https://localhost:7185'
    : 'https://engine.sparc.coop';

export default class TovikEngine {
    static userLang;
    static rtlLanguages = ['ar', 'fa', 'he', 'ur', 'ps', 'ku', 'dv', 'yi', 'sd', 'ug'];

    static async hi() {
        var profile = await db.profiles.get('default');
        if (!profile) {
            profile = { id: 'default', language: navigator.language };
            await db.profiles.add(profile);
        }

        this.setLanguage(profile.language);
        document.addEventListener('tovik-user-language-changed', async (event: CustomEvent) => {
            await this.setLanguage(event.detail);
        });
    }

    static async getLanguages() {
        return await this.fetch('translate/languages'); 
    }

    static async setLanguage(language) {
        if (this.userLang != language) {
            await db.profiles.put({ id: 'default', language: language });
            this.userLang = language;
        }

        document.documentElement.lang = this.userLang;
        document.documentElement.setAttribute('dir', this.rtlLanguages.some(x => this.userLang.startsWith(x)) ? 'rtl' : 'ltr');
        document.dispatchEvent(new CustomEvent('tovik-language-changed', { detail: this.userLang }));
    }

    static idHash(text, lang = null) {
        if (!lang)
            lang = this.userLang;

        return MD5(text.trim() + ':' + lang);
    }

    static async translate(text, fromLang) {
        const request = {
            id: this.idHash(text, fromLang),
            Domain: window.location.host,
            LanguageId: fromLang,
            Language: { Id: fromLang },
            Text: text
        };

        return await this.fetch('translate', request, this.userLang);
    }

    static async bulkTranslate(items, fromLang) {
        var progress = document.querySelectorAll('.language-select-progress-bar');
        for (let i = 0; i < progress.length; i++) {
            progress[i].classList.add('show');
        }

        const requests = items.map(item => ({
            id: item.hash || this.idHash(item.text, fromLang),
            Domain: window.location.host,
            LanguageId: fromLang,
            Language: { Id: fromLang },
            Text: item.text
        }));

        var result = await this.fetch('translate/bulk', requests, this.userLang);

        for (let i = 0; i < progress.length; i++) {
            progress[i].classList.remove('show');
        }

        return result;
    }

    static async fetch(url: string, body: any = null, language: string = null) {
        const options: any = {
            credentials: 'include',
            method: body ? 'POST' : 'GET',
            headers: new Headers()
        };

        if (body) {
            options.headers.append('Content-Type', 'application/json');
            options.body = JSON.stringify(body);
        }

        if (language) {
            options.headers.append('Accept-Language', language); 
        }

        const response = await fetch(`${baseUrl}/${url}`, options);

        if (response.ok)
            return await response.json();
        else
            throw new Error(`Failed to fetch data from ${url}`);
    }
}