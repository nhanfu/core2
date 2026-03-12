import { SqlViewModel } from "../models/sqlViewModel.js";
import { Client } from "../clients/client.js";
import { Str } from "./ext.js";

export class LangSelect {
    static langProp = "langprop";
    static langKey = "langkey";
    static langParam = "para";
    static langCode = "langcode";
    static active = "active";
    static _webConfig = {};
    static _dictionaries = {};

    static _culture;

    static get culture() {
        if (LangSelect._culture !== undefined && LangSelect._culture !== null) {
            return LangSelect._culture.replace("\"", "");
        }
        const res = localStorage.getItem("culture");
        if (res !== null) {
            LangSelect._culture = res.replace("\"", "");
        }
        return res !== null ? res.replace("\"", "") : null;
    }

    static set culture(value) {
        if (LangSelect._culture === value) {
            return;
        }
        LangSelect._culture = value !== null ? value.replace("\"", "") : null;
        localStorage.setItem("culture", value !== null ? value.replace("\"", "") : null);
    }

    static setCultureAndTranslate(code) {
        LangSelect.culture = code;
        LangSelect.translate();
    }

    /** @returns {string} key Label */
    static get(key, featureName) {
        if (key === null || key === "") {
            return "";
        }
        if (LangSelect.culture === null) {
            return key;
        }
        const dictionary = LangSelect._dictionaries;
        if (dictionary === undefined || dictionary === null) {
            const tempDictionary = localStorage.getItem(LangSelect.culture);
            if (tempDictionary !== null) {
                LangSelect._dictionaries = tempDictionary;
            }
        }
        if (dictionary[key + "_" + featureName]) {
            return dictionary[key + "_" + featureName] ? dictionary[key + "_" + featureName] : key;
        }
        else {
            return dictionary[key] ? dictionary[key] : key;
        }
    }

    static async translate() {
        var data = await fetch(Client.api + "/api/dictionary");
        var items = await data.json();
        LangSelect.dictionaryLoaded(items);
    }

    static dictionaryLoaded(dictionaryItems) {
        const map = dictionaryItems.filter((x, i, arr) => arr.findIndex(y => y.Key === x.Key) === i).reduce((acc, cur) => {
            acc[cur.Key] = cur.Value;
            return acc;
        }, {});
        LangSelect._dictionaries = map;
        localStorage.setItem(LangSelect.culture, JSON.stringify(map));
        LangSelect.travel(document).forEach(x => {
            const props = x[LangSelect.langProp];
            if (props === null || props === undefined || props === "") {
                return;
            }
            props.split(",").forEach(propName => {
                const template = x[LangSelect.langKey + propName];
                const parameters = x[LangSelect.langParam + propName];
                const translated = map[template] !== undefined ? map[template] : template;
                if (parameters !== undefined && parameters !== null && parameters.length > 0) {
                    x[propName] = Str.format(translated, parameters);
                }
                else {
                    x[propName] = translated;
                }
            });
        });
    }

    static *travel(node) {
        yield node;
        for (const element of node.childNodes) {
            yield* this.travel(element);
        }
    }
}
