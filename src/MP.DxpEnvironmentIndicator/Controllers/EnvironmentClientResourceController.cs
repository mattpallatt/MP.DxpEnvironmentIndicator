using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Controllers;

// Serves the two client-side scripts the shell needs — the admin settings overlay bootstrap and the
// top-bar environment badge. Served from a controller (rather than packaged static assets) so the
// class library is self-contained — there is no host-side ClientResources folder to deploy.
//
// Requires an authenticated user (not a specific role): both scripts are only ever loaded inside the
// already-authenticated CMS shell, so the auth cookie rides along — while keeping EnvIndicator.js
// (which names the environment) off any anonymous, bot-discoverable endpoint.
[Authorize]
public class EnvironmentClientResourceController(IEnvironmentResolver resolver, IEnvironmentSettingsService settings) : Controller
{
    // The CSS selector for the top-bar label the badge is placed next to. Overridable from the
    // settings page (advanced) in case a CMS update moves it; this is the built-in fallback.
    // Targets .flex--1.truncate anywhere inside the OPN web component — does not assume a specific
    // section alignment class, which differs between CMS 12 and CMS 13.
    public const string DefaultSelector = "epi-pn-navigation .flex--1.truncate";

    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/AdminInit.js")]
    [ResponseCache(Duration = 60)]
    public IActionResult AdminInit() =>
        Content(AdminInitScript, "application/javascript; charset=utf-8");

    // CMS 13 Add-ons iframe entry point.  The shell's module router iframes this physical-style URL
    // (it's under our registered module prefix); the page immediately redirects the iframe to the
    // actual Razor settings controller so no server-rendered content lives in a static file.
    [HttpGet]
    [Route("~/Optimizely/DxpEnvironmentIndicator/settings.html")]
    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult SettingsEntry() => Content("""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8"><title>Environment Label</title></head>
        <body style="margin:0">
        <script>window.location.replace('/EPiServer/DxpEnvironmentIndicator/settings');</script>
        </body>
        </html>
        """, "text/html; charset=utf-8");

    // Stub AMD module served at the module's ClientResources path.  Satisfies any runtime
    // resource checks the shell performs after the module is registered.
    [HttpGet]
    [Route("~/Optimizely/DxpEnvironmentIndicator/ClientResources/init.js")]
    [ResponseCache(Duration = 3600)]
    public IActionResult ModuleInit() =>
        Content("// Environment Label module init", "application/javascript; charset=utf-8");

    // Resolves the environment server-side from the request host and bakes the name, background and
    // (accessibility-aware) text colour, and selector into the script — no client-side fetch and no
    // response cache, so a colour or selector change takes effect on the next load. When nothing
    // should show (production opt-out, or an unmatched host outside local dev) an inert script is returned.
    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/EnvIndicator.js")]
    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult EnvIndicator()
    {
        var env = resolver.Resolve();
        if (env == null)
            return Content("/* DXP environment indicator: disabled */", "application/javascript; charset=utf-8");

        var selector = settings.Get().Selector;
        if (string.IsNullOrWhiteSpace(selector)) selector = DefaultSelector;

        var prelude = $"var __DXP_LABEL={JsonSerializer.Serialize(env.Label)};"
                    + $"var __DXP_COLOR={JsonSerializer.Serialize(env.Color)};"
                    + $"var __DXP_TEXT={JsonSerializer.Serialize(ContrastColor.Text(env.Color))};"
                    + $"var __DXP_SELECTOR={JsonSerializer.Serialize(selector)};\n";
        return Content(prelude + EnvIndicatorScript, "application/javascript; charset=utf-8");
    }

    // CMS 12 only. Watches location.hash; when it matches our route it overlays a fixed-position
    // iframe of the settings page over the admin content pane (right of the sidebar nav, so the
    // shell chrome stays visible), then hides it again when the hash changes away. This keeps the
    // admin SPA in the browser history so the shell nav remains present throughout.
    private const string AdminInitScript = """
    (function () {
        var ROUTE = '#/EnvIndicator/Settings';
        var FRAME_ID = 'dxp-env-settings-frame';
        var SETTINGS_URL = '/EPiServer/DxpEnvironmentIndicator/settings';
        var NAV_SELECTOR = '.epi-side-bar-navigation';
        var CONTENT_SELECTOR = '.content-area-container';
        var trackTimer = null;

        function topOffset() {
            var nav = document.querySelector(
                '.epi-navigation, #epi-shellHeader, [class*="shellHeader"], header[role="banner"]');
            var b = nav ? Math.round(nav.getBoundingClientRect().bottom) : 0;
            return b > 0 ? b : 48;
        }

        function rectOf(sel, minW, minH) {
            var el = document.querySelector(sel);
            if (el) { var r = el.getBoundingClientRect(); if (r.width > minW && r.height > minH) return r; }
            return null;
        }

        function applyGeometry(frame) {
            var nav = rectOf(NAV_SELECTOR, 100, 100);
            if (nav) {
                frame.style.top    = nav.top + 'px';
                frame.style.left   = nav.right + 'px';
                frame.style.width  = Math.max(0, window.innerWidth - nav.right) + 'px';
                frame.style.height = nav.height + 'px';
                return;
            }
            var c = rectOf(CONTENT_SELECTOR, 100, 100);
            if (c) {
                frame.style.top = c.top + 'px'; frame.style.left = c.left + 'px';
                frame.style.width = c.width + 'px'; frame.style.height = c.height + 'px';
                return;
            }
            var t = topOffset();
            frame.style.top = t + 'px'; frame.style.left = '0';
            frame.style.width = '100vw'; frame.style.height = 'calc(100vh - ' + t + 'px)';
        }

        function showFrame() {
            var frame = document.getElementById(FRAME_ID);
            if (!frame) {
                frame = document.createElement('iframe');
                frame.id    = FRAME_ID;
                frame.src   = SETTINGS_URL;
                frame.title = 'Environment Label Settings';
                frame.style.cssText = 'position:fixed;border:0;z-index:2147483000;background:#fff;';
                document.body.appendChild(frame);
            }
            applyGeometry(frame);
            frame.style.display = 'block';
            if (!trackTimer) trackTimer = setInterval(function () {
                var f = document.getElementById(FRAME_ID);
                if (f && f.style.display !== 'none') applyGeometry(f);
            }, 300);
        }

        function hideFrame() {
            var frame = document.getElementById(FRAME_ID);
            if (frame) frame.style.display = 'none';
            if (trackTimer) { clearInterval(trackTimer); trackTimer = null; }
        }

        function sync() {
            if ((location.hash || '').indexOf(ROUTE) === 0) showFrame(); else hideFrame();
        }

        window.addEventListener('hashchange', sync);
        window.addEventListener('resize', function () {
            var f = document.getElementById(FRAME_ID);
            if (f && f.style.display !== 'none') applyGeometry(f);
        });

        if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', sync);
        else sync();

        [250, 750, 1500].forEach(function (ms) { setTimeout(sync, ms); });
    })();
    """;

    // Reads __DXP_ENV / __DXP_COLOR from the prelude. Finds the "CMS" product label in the top OPN
    // navigation bar and inserts a coloured environment pill next to it. Tries the configured selector
    // first, then a chain of known selectors for CMS 12 and CMS 13, then a text-content fallback that
    // finds any nav button whose visible text is exactly "CMS". The badge is idempotent via the
    // .dxp-env-badge marker. The OPN renders asynchronously, so placement runs on a light periodic
    // poll (cheap once badged — a single querySelector early-out) rather than a subtree MutationObserver
    // reacting to every shell mutation; polling for the session also lets the badge self-heal if the
    // CMS re-renders the top bar and strips our node.
    internal const string EnvIndicatorScript = """
    (function () {
        var POLL_MS = 500;
        var BADGE_CLASS = 'dxp-env-badge';

        var SELECTORS = [
            __DXP_SELECTOR,                                              // "epi-pn-navigation .flex--1.truncate"
            '.epi-pn-navigation .flex--1.truncate',                      // class-based OPN container (CMS 12/13)
            '.epi-pn-navigation__section--align-center .flex--1.truncate',
            '.epi-pn-navigation__section--align-start .flex--1.truncate', // CMS 13 may use --align-start
            'epi-pn-navigation .oui-button span',
            '.epi-pn-navigation .oui-button span',
            '.platform-navigation-wrapper .flex--1.truncate',
            '[class*="platform-navigation"] .truncate'
        ];
        var seen = {}, selectors = [];
        for (var i = 0; i < SELECTORS.length; i++) {
            if (SELECTORS[i] && !seen[SELECTORS[i]]) { seen[SELECTORS[i]] = 1; selectors.push(SELECTORS[i]); }
        }

        function findLabelBySelector() {
            for (var i = 0; i < selectors.length; i++) {
                try {
                    var els = document.querySelectorAll(selectors[i]);
                    for (var j = 0; j < els.length; j++) {
                        if ((els[j].textContent || '').trim() === 'CMS') return els[j];
                    }
                } catch(e) {}
            }
            return null;
        }

        // Text-content fallback: any button/span inside a nav-looking element whose text is "CMS"
        function findLabelByText() {
            var candidates = document.querySelectorAll(
                'epi-pn-navigation button, epi-pn-navigation span, ' +
                '[class*="navigation"] button, [class*="navigation"] span');
            for (var i = 0; i < candidates.length; i++) {
                var el = candidates[i];
                if ((el.textContent || '').trim() === 'CMS' && el.children.length === 0) return el;
            }
            return null;
        }

        function findLabel() {
            return findLabelBySelector() || findLabelByText();
        }

        function alreadyBadged(anchor) {
            var p = anchor.parentElement;
            while (p) {
                if (p.querySelector('.' + BADGE_CLASS)) return true;
                if (p === document.body) break;
                p = p.parentElement;
            }
            return false;
        }

        function apply() {
            if (document.querySelector('.' + BADGE_CLASS)) return true;
            var label = findLabel();
            if (!label) return false;
            var host = label.closest('.oui-dropdown-group') || label.closest('.oui-button') ||
                       label.closest('button') || label.parentElement;
            if (!host || !host.parentElement) return false;
            if (alreadyBadged(host)) return true;

            var badge = document.createElement('span');
            badge.className = BADGE_CLASS;
            badge.textContent = __DXP_LABEL;
            badge.style.cssText =
                'display:inline-flex;align-items:center;padding:1px 7px;background:' + __DXP_COLOR +
                ';color:' + __DXP_TEXT + ';font-size:11px;font-weight:700;border-radius:3px;' +
                'letter-spacing:0.5px;margin-left:8px;flex-shrink:0;white-space:nowrap;' +
                'vertical-align:middle;';
            host.insertAdjacentElement('afterend', badge);
            return true;
        }

        apply();
        setInterval(apply, POLL_MS);
    })();
    """;
}
