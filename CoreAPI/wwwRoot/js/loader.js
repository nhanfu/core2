import { IsFunction } from "./core/utils.js";

export const getMetaInfo = (name, root) => {
    const metaTag = root.querySelector(`script[data-${name}]`);
    return metaTag != null ? metaTag.dataset[name] : root.dataset[name];
    return null;
};

export const getComName = (root) => {
    const metaTags = Array.from(root.querySelectorAll(`[data-meta]`));
    return metaTags.map((tag) => tag.getAttribute("data-meta"));
};

export async function comQuery(meta, com) {
    if (meta.Query == null) return null;
    const o = {};
    var isFn = IsFunction(meta.PreQuery, o);
    var body = {
        ComId: meta.Id,
        Params: isFn ? JSON.stringify(o.v.call(null, com)) : null,
        AnnonymousTenant: meta.TenantCode ?? 'system',
        AnnonymousEnv: meta.Env ?? 'test',
        MetaConn: 'default',
        DataConn: 'bl',
        WrapQuery: false
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
                ComId: 'Com',
                Action: feature,
                AnnonymousTenant: tenant,
                AnnonymousEnv: env,
                Params: JSON.stringify(params)
            }),
        });
        if (response.ok) {
            const res = await response.json();
            const components = res[0];
            components.map(com => {
                const fnVal = {};
                const isRendererFn = IsFunction(com.Renderer, fnVal);
                if (!isRendererFn) return;
                const container = meta.find(x => x.dataset.meta == com.FieldName);
                if (container == null) return;
                fnVal.v.call(null, container, com, comQuery);
            });
        }
    } catch (error) {
        console.error(`Error loading feature: ${error}`);
        return null;
    }
}

await resolveComponents(document);