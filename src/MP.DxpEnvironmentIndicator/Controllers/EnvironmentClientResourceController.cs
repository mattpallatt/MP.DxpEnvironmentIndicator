using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Controllers;

// Serves the two client-side scripts the shell needs — the admin settings overlay bootstrap and the
// top-bar environment badge. Served from a controller (rather than packaged static assets) so the
// class library is self-contained — there is no host-side ClientResources folder to deploy.
[AllowAnonymous]
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

    // Resolves the environment server-side from the request host and bakes the name, background and
    // (accessibility-aware) text colour, and selector into the script — no client-side fetch and no
    // response cache, so a colour or selector change takes effect on the next load. When nothing
    // should show (production opt-out, or an unmatched host outside local dev) an inert script is returned.
    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/EnvIndicator.js")]
    public IActionResult EnvIndicator()
    {
        var env = resolver.Resolve(Request.Host.Host ?? string.Empty);
        if (env == null)
            return Content("/* DXP environment indicator: no badge for this environment */", "application/javascript; charset=utf-8");

        var selector = settings.Get().Selector;
        if (string.IsNullOrWhiteSpace(selector)) selector = DefaultSelector;

        var prelude = $"var __DXP_ENV={JsonSerializer.Serialize(env.Name)};"
                    + $"var __DXP_LABEL={JsonSerializer.Serialize(env.Label)};"
                    + $"var __DXP_COLOR={JsonSerializer.Serialize(env.Color)};"
                    + $"var __DXP_TEXT={JsonSerializer.Serialize(ContrastColor.Text(env.Color))};"
                    + $"var __DXP_SELECTOR={JsonSerializer.Serialize(selector)};\n";
        return Content(prelude + EnvIndicatorScript, "application/javascript; charset=utf-8");
    }

    // Injects a "DXP Environment Indicator" link directly into the CMS 13 admin SPA sidebar.
    // The SPA (React) has hardcoded navigation so [MenuProvider] cannot add to it; we clone
    // an existing link's style and inject ours pointing straight at the standalone settings page.
    // A MutationObserver retries for 30 s in case the SPA re-renders and removes our link.
    private const string AdminInitScript = """
    (function () {
        var SETTINGS_URL = '/EPiServer/DxpEnvironmentIndicator/Admin/Settings';
        var LINK_ID = 'dxp-env-settings-nav-link';
        var linkObserver = null;

        function injectNavLink() {
            if (document.getElementById(LINK_ID)) return true;

            var existingLink = null;
            var links = document.querySelectorAll('a[href]');
            for (var i = 0; i < links.length; i++) {
                var h = links[i].getAttribute('href') || '';
                if (h.indexOf('#/') === 0 || h.indexOf('default#/') >= 0) {
                    existingLink = links[i];
                    break;
                }
            }
            if (!existingLink) return false;

            var container = existingLink.parentElement;
            while (container && container !== document.body) {
                var tag = container.tagName.toLowerCase();
                if (tag === 'ul' || tag === 'ol' || tag === 'nav' ||
                    container.getAttribute('role') === 'navigation') break;
                container = container.parentElement;
            }
            if (!container || container === document.body) return false;

            var cloneItem = existingLink.closest('li') || existingLink;
            var newItem = cloneItem.cloneNode(false);
            if (cloneItem.tagName.toLowerCase() === 'li') {
                var a = document.createElement('a');
                a.id = LINK_ID;
                a.href = SETTINGS_URL;
                a.className = existingLink.className;
                a.setAttribute('aria-current', 'false');
                a.textContent = 'DXP Environment Indicator';
                newItem.appendChild(a);
            } else {
                newItem.id = LINK_ID;
                newItem.href = SETTINGS_URL;
                newItem.textContent = 'DXP Environment Indicator';
            }
            container.appendChild(newItem);
            return true;
        }

        function startObserver() {
            if (linkObserver) return;
            linkObserver = new MutationObserver(function () {
                if (!document.getElementById(LINK_ID)) injectNavLink();
            });
            linkObserver.observe(document.body, { childList: true, subtree: true });
            setTimeout(function () {
                if (linkObserver) { linkObserver.disconnect(); linkObserver = null; }
            }, 30000);
        }

        function init() {
            if (!injectNavLink()) startObserver();
        }

        if (document.readyState === 'loading')
            document.addEventListener('DOMContentLoaded', init);
        else
            init();

        [250, 750, 1500].forEach(function (ms) {
            setTimeout(function () { if (!document.getElementById(LINK_ID)) injectNavLink(); }, ms);
        });
    })();
    """;

    // Reads __DXP_ENV / __DXP_COLOR from the prelude. Finds the "CMS" product label in the top OPN
    // navigation bar and inserts a coloured environment pill next to it. Tries the configured selector
    // first, then a chain of known selectors for CMS 12 and CMS 13, then a text-content fallback that
    // finds any nav button whose visible text is exactly "CMS". The badge is idempotent via the
    // .dxp-env-badge marker. The OPN renders asynchronously so a MutationObserver retries for 15 s.
    private const string EnvIndicatorScript = """
    (function () {
        var deadline = Date.now() + 15000;
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
            var label = findLabel();
            if (!label) return false;
            var host = label.closest('.oui-dropdown-group') || label.closest('.oui-button') ||
                       label.closest('button') || label.parentElement;
            if (!host || !host.parentElement) return false;
            if (alreadyBadged(host)) return true;

            var badge = document.createElement('span');
            badge.className = BADGE_CLASS;
            badge.setAttribute('data-dxp-env', __DXP_ENV);
            badge.textContent = __DXP_LABEL;
            badge.style.cssText =
                'display:inline-flex;align-items:center;padding:1px 7px;background:' + __DXP_COLOR +
                ';color:' + __DXP_TEXT + ';font-size:11px;font-weight:700;border-radius:3px;' +
                'letter-spacing:0.5px;margin-left:8px;flex-shrink:0;white-space:nowrap;cursor:pointer;' +
                'vertical-align:middle;';
            badge.title = 'DXP Environment Indicator — click to open settings';
            badge.onclick = function (e) {
                e.stopPropagation();
                window.location = '/EPiServer/DxpEnvironmentIndicator/Admin/Settings';
            };
            host.insertAdjacentElement('afterend', badge);
            return true;
        }

        function start() {
            if (apply()) return;
            var obs = new MutationObserver(function () {
                if (apply() || Date.now() > deadline) obs.disconnect();
            });
            obs.observe(document.body, { childList: true, subtree: true, characterData: true });
            setTimeout(function () { obs.disconnect(); }, 15000);
        }

        if (document.readyState === 'loading')
            document.addEventListener('DOMContentLoaded', start);
        else
            start();
    })();
    """;
}
