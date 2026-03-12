import { Component } from "../models/component.js";
import { Utils } from "./utils.js";

export const getMetaInfo = (name, root) => {
    const metaTag = root.querySelector(`script[data-${name}]`);
    return metaTag != null ? metaTag.dataset[name] : root.dataset[name];
};

export const getComName = (root) => {
    const metaTags = Array.from(root.querySelectorAll(`[data-meta]`));
    return metaTags.map((tag) => tag.getAttribute("data-meta"));
};

export async function comQuery(meta, com) {
    if (meta.Query == null) return null;
    var params = Utils.isFunction(meta.preQuery);
    var body = {
        comId: meta.Id,
        Params: params,
        annonymousTenant: meta.tenantCode ?? 'system',
        annonymousEnv: meta.Env ?? 'test',
        metaConn: 'default',
        dataConn: 'bl',
        wrapQuery: false
    };
    var res = await fetch('/api/user/comquery', {
        method: 'POST', headers: { "Content-Type": "application/json", }, body: JSON.stringify(body)
    });
    if (res.ok) return await res.json();
}

export async function resolveComponents(root) {
    const tenant = getMetaInfo("tenant", root) ?? "system";
    const env = getMetaInfo("env", root) ?? "test";
    const feature = getMetaInfo("feature", root) ?? "index";
    const meta = Array.from(root.querySelectorAll('[data-meta]'));
    const params = meta.map(x => {
        return { meta: x.dataset.meta, feature: x.dataset.feature ?? feature }
    });
    try {
        const response = await fetch(`/api/user/comp/`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                comId: 'Com',
                Action: feature,
                annonymousTenant: tenant,
                annonymousEnv: env,
                Params: JSON.stringify(params)
            }),
        });
        if (response.ok) {
            const res = await response.json();
            const components = res[0];
            components.map((/** @type {Component} */ com) => {
                const isRendererFn = Utils.isFunction(com.Renderer, false, root);
                if (!isRendererFn) return;
                const container = meta.find(x => x.dataset.meta == com.fieldName);
                if (container == null) return;
                isRendererFn.call(null, container, com, comQuery);
            });
        }
    } catch (error) {
        console.error(`Error loading feature: ${error}`);
        return null;
    }
}

await resolveComponents(document);