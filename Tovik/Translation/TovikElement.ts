import db from './TovikDb.js';
import TovikEngine from './TovikEngine.js';

export default class TovikElement extends HTMLElement {
    observer;
    forceReload = false;
    #observedElement;
    #originalLang;

    constructor() {
        super();
    }

    async connectedCallback() {
        this.#observedElement = this;
        this.#originalLang = this.lang || document.documentElement.lang;

        // if the attribute 'for' is set, observe the element with that selector
        if (this.hasAttribute('for')) {
            const selector = this.getAttribute('for');
            this.#observedElement = document.querySelector(selector);
        }

        await this.wrapTextNodes(this.#observedElement);
        document.addEventListener('tovik-language-changed', async (event: any) => {
            await this.wrapTextNodes(this.#observedElement, true);
        });

        document.addEventListener('tovik-content-changed', async (event: any) => {
            console.log('tovik-content-changed event received');
            await this.wrapTextNodes(this.#observedElement);
        });


        this.observer = new MutationObserver(this.#observer);
        this.observer.observe(this.#observedElement, { childList: true, characterData: false, subtree: true });
    }

    disconnectedCallback() {
        if (this.observer)
            this.observer.disconnect();
    }

    async wrapTextNodes(element, forceReload = false) {
        var nodes = [];
        var treeWalker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT, forceReload ? this.#tovikForceReloadIgnoreFilter : this.#tovikIgnoreFilter);
        while (treeWalker.nextNode()) {
            const node = treeWalker.currentNode;
            if (this.isValid(node)) {
                node['translating'] = true;
                nodes.push(node);
            }
        }

        await this.queueForTranslation(nodes);
    }

    isValid(node) {
        return node
            && node.textContent
            && /\p{Letter}/u.test(node.textContent) // Check if the text contains any letter
            && !(node.parentElement && node.parentElement.tagName === 'TOVIK-T');
    }

    #observer = mutations => {
        document.dispatchEvent(new CustomEvent('tovik-content-changed'));
    };

    #tovikIgnoreFilter = function (node) {
        var approvedNodes = ['#text'];

        if (!approvedNodes.includes(node.nodeName) || node.translating || node.translated || node.parentNode.nodeName == 'SCRIPT')
            return NodeFilter.FILTER_SKIP;

        var closest = node.parentElement.closest('[translate="no"]');
        if (closest)
            return NodeFilter.FILTER_SKIP;

        return NodeFilter.FILTER_ACCEPT;
    }

    #tovikForceReloadIgnoreFilter = function (node) {
        var approvedNodes = ['#text'];

        if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT')
            return NodeFilter.FILTER_SKIP;

        var closest = node.parentElement.closest('[translate="no"]');
        if (closest)
            return NodeFilter.FILTER_SKIP;

        return NodeFilter.FILTER_ACCEPT;
    }

    async queueForTranslation(textNodes) {
        let pendingTranslations = [];
        console.log('oh lawd', textNodes);

        await Promise.all(textNodes.map(async textNode => {
            if (!textNode.textContent)
                return;

            if (!textNode.originalText) {
                textNode.originalText = textNode.textContent.trim();
            }

            console.log('mapping', textNode.originalText);
            textNode.hash = TovikEngine.idHash(textNode.originalText);
            const translation = await db.translations.get(textNode.hash);
            if (translation) {
                textNode.textContent = translation.text;
            } else {
                // Queue for bulk translation if not in cache
                if (!pendingTranslations.some(node => node.hash === textNode.hash)) {
                    pendingTranslations.push(textNode);
                }
            }
        }));

        console.log('oh lawd!', pendingTranslations);
        if (pendingTranslations.length > 0) {
            await this.processBulkTranslations(pendingTranslations);
        }
    }

    async processBulkTranslations(pendingTranslations) {
        if (pendingTranslations.length === 0) return;

        console.log(`Processing bulk translation for ${pendingTranslations.length} nodes`);

        const textsToTranslate = pendingTranslations.map(node => ({
            hash: node.hash,
            text: node.originalText
        }));

        const newTranslations = await TovikEngine.bulkTranslate(textsToTranslate, this.#originalLang);
        for (const node of pendingTranslations) {
            const translation = newTranslations.find(t => t.id === node.hash);
            console.log('checking for', newTranslations, node.hash, translation);
            if (translation) {
                node.textContent = translation.text;
                node.translating = false;
                node.translated = true;
                db.translations.put(translation);
            }
        }
    }
}
