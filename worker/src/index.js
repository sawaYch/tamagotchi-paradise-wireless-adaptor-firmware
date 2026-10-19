const ORIGIN = "https://tools.bbamorachi.us";
const SHIM_TAG = '<script src="/__tama/shim.js"></script>';
const COOKIE_NAME = "tama_auth";
const COOKIE_MAX_AGE = 60 * 60 * 24 * 30;

const encoder = new TextEncoder();

async function sha256Hex(text) {
  const digest = await crypto.subtle.digest("SHA-256", encoder.encode(text));
  return [...new Uint8Array(digest)].map(b => b.toString(16).padStart(2, "0")).join("");
}

function safeEqual(a, b) {
  const ea = encoder.encode(a);
  const eb = encoder.encode(b);
  if (ea.length !== eb.length) return false;
  return crypto.subtle.timingSafeEqual(ea, eb);
}

function getCookie(request, name) {
  const header = request.headers.get("cookie") || "";
  for (const part of header.split(";")) {
    const [key, ...rest] = part.trim().split("=");
    if (key === name) return rest.join("=");
  }
  return null;
}

function notFound() {
  return new Response("Not found", {
    status: 404,
    headers: { "cache-control": "no-store", "x-robots-tag": "noindex" }
  });
}

export default {
  async fetch(request, env) {
    if (!env.ACCESS_KEY) return new Response("Not configured", { status: 503 });

    const url = new URL(request.url);
    const expected = await sha256Hex(env.ACCESS_KEY);

    const queryKey = url.searchParams.get("key");
    if (queryKey !== null) {
      if (!safeEqual(await sha256Hex(queryKey), expected)) return notFound();
      url.searchParams.delete("key");
      return new Response(null, {
        status: 302,
        headers: {
          location: url.toString(),
          "set-cookie": `${COOKIE_NAME}=${expected}; Path=/; Max-Age=${COOKIE_MAX_AGE}; HttpOnly; Secure; SameSite=Lax`,
          "cache-control": "no-store"
        }
      });
    }

    const cookie = getCookie(request, COOKIE_NAME);
    if (!cookie || !safeEqual(cookie, expected)) return notFound();

    if (url.pathname.startsWith("/__tama/")) return env.ASSETS.fetch(request);

    const target = new URL(url.pathname + url.search, ORIGIN);

    const headers = new Headers(request.headers);
    headers.delete("cookie");
    headers.set("origin", ORIGIN);
    headers.set("referer", ORIGIN + "/");

    const hasBody = request.method !== "GET" && request.method !== "HEAD";
    const upstream = await fetch(target, {
      method: request.method,
      headers,
      body: hasBody ? request.body : undefined,
      redirect: "manual"
    });

    const resHeaders = new Headers(upstream.headers);
    const location = resHeaders.get("location");
    if (location) resHeaders.set("location", location.replace(ORIGIN, url.origin));
    resHeaders.set("x-robots-tag", "noindex");

    const init = { status: upstream.status, statusText: upstream.statusText, headers: resHeaders };
    const contentType = resHeaders.get("content-type") || "";

    if (contentType.includes("text/html")) {
      resHeaders.delete("content-length");
      return new HTMLRewriter()
        .on("head", {
          element(el) {
            el.prepend(SHIM_TAG, { html: true });
          }
        })
        .transform(new Response(upstream.body, init));
    }

    return new Response(upstream.body, init);
  }
};
