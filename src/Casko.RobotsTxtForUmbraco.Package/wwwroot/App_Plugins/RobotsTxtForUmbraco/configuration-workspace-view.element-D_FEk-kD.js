var H = Object.create;
var h = Object.defineProperty;
var J = Object.getOwnPropertyDescriptor;
var U = (d, t) => (t = Symbol[d]) ? t : /* @__PURE__ */ Symbol.for("Symbol." + d), g = (d) => {
  throw TypeError(d);
};
var K = (d, t, i) => t in d ? h(d, t, { enumerable: !0, configurable: !0, writable: !0, value: i }) : d[t] = i;
var L = (d, t) => h(d, "name", { value: t, configurable: !0 });
var j = (d) => [, , , H(d?.[U("metadata")] ?? null)], q = ["class", "method", "getter", "setter", "accessor", "field", "value", "get", "set"], b = (d) => d !== void 0 && typeof d != "function" ? g("Function expected") : d, N = (d, t, i, l, r) => ({ kind: q[d], name: t, metadata: l, addInitializer: (a) => i._ ? g("Already initialized") : r.push(b(a || null)) }), O = (d, t) => K(t, U("metadata"), d[3]), B = (d, t, i, l) => {
  for (var r = 0, a = d[t >> 1], c = a && a.length; r < c; r++) t & 1 ? a[r].call(i) : l = a[r].call(i, l);
  return l;
}, E = (d, t, i, l, r, a) => {
  var c, s, A, n, p, e = t & 7, x = !!(t & 8), m = !!(t & 16), v = e > 3 ? d.length + 1 : e ? x ? 1 : 2 : 0, D = q[e + 5], I = e > 3 && (d[v - 1] = []), G = d[v] || (d[v] = []), u = e && (!m && !x && (r = r.prototype), e < 5 && (e > 3 || !m) && J(e < 4 ? r : { get [i]() {
    return M(this, a);
  }, set [i](o) {
    return S(this, a, o);
  } }, i));
  e ? m && e < 4 && L(a, (e > 2 ? "set " : e > 1 ? "get " : "") + i) : L(r, i);
  for (var f = l.length - 1; f >= 0; f--)
    n = N(e, i, A = {}, d[3], G), e && (n.static = x, n.private = m, p = n.access = { has: m ? (o) => Q(r, o) : (o) => i in o }, e ^ 3 && (p.get = m ? (o) => (e ^ 1 ? M : R)(o, r, e ^ 4 ? a : u.get) : (o) => o[i]), e > 2 && (p.set = m ? (o, y) => S(o, r, y, e ^ 4 ? a : u.set) : (o, y) => o[i] = y)), s = (0, l[f])(e ? e < 4 ? m ? a : u[D] : e > 4 ? void 0 : { get: u.get, set: u.set } : r, n), A._ = 1, e ^ 4 || s === void 0 ? b(s) && (e > 4 ? I.unshift(s) : e ? m ? a = s : u[D] = s : r = s) : typeof s != "object" || s === null ? g("Object expected") : (b(c = s.get) && (u.get = c), b(c = s.set) && (u.set = c), b(c = s.init) && I.unshift(c));
  return e || O(d, r), u && h(r, i, u), m ? e ^ 4 ? a : u : r;
};
var z = (d, t, i) => t.has(d) || g("Cannot " + i), Q = (d, t) => Object(t) !== t ? g('Cannot use the "in" operator on this value') : d.has(t), M = (d, t, i) => (z(d, t, "read from private field"), i ? i.call(d) : t.get(d));
var S = (d, t, i, l) => (z(d, t, "write to private field"), l ? l.call(d, i) : t.set(d, i), i), R = (d, t, i) => (z(d, t, "access private method"), i);
import { LitElement as T, html as X, css as Y, customElement as Z } from "@umbraco-cms/backoffice/external/lit";
var F, P, _;
F = [Z("casko-robots-txt-configuration-workspace-view")];
class w extends (_ = T) {
  render() {
    return X`
      <uui-box headline="Configuration">
        <dl>
          <dt>Configuration section</dt>
          <dd>RobotsTxt</dd>
          <dt>Public rewrite</dt>
          <dd>/robots.txt</dd>
          <dt>Delivery API</dt>
          <dd>/umbraco/delivery/api/v1/robotstxt</dd>
          <dt>Storage</dt>
          <dd>Umbraco Media folder: Robots Txt</dd>
        </dl>
      </uui-box>
    `;
  }
  static styles = Y`
    :host {
      display: block;
      padding: var(--uui-size-layout-1);
    }

    dl {
      display: grid;
      grid-template-columns: minmax(12rem, 18rem) 1fr;
      gap: var(--uui-size-space-3) var(--uui-size-space-5);
      margin: 0;
    }

    dt {
      font-weight: 700;
    }

    dd {
      margin: 0;
    }
  `;
}
P = j(_), w = E(P, 0, "CaskoRobotsTxtConfigurationWorkspaceViewElement", F, w), B(P, 1, w);
export {
  w as CaskoRobotsTxtConfigurationWorkspaceViewElement,
  w as default
};
//# sourceMappingURL=configuration-workspace-view.element-D_FEk-kD.js.map
