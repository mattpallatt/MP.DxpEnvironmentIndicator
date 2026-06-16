using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MP.DxpEnvironmentIndicator.Services;

namespace MP.DxpEnvironmentIndicator.Controllers;

// Serves the two client-side scripts the shell needs — the admin settings overlay bootstrap and the
// top-bar environment badge. Served from a controller (rather than packaged static assets) so the
// class library is self-contained — there is no host-side ClientResources folder to deploy.
[AllowAnonymous]
public class EnvironmentClientResourceController(IEnvironmentResolver resolver) : Controller
{
    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/AdminInit.js")]
    [ResponseCache(Duration = 300)]
    public IActionResult AdminInit() =>
        Content(AdminInitScript, "application/javascript; charset=utf-8");

    // Resolves the environment server-side from the request host and bakes the name/colour into the
    // script (no client-side fetch). When nothing should show (production opt-out, or an unmatched
    // host outside local dev) an inert script is returned.
    [HttpGet]
    [Route("~/EPiServer/DxpEnvironmentIndicator/ClientResources/Scripts/EnvIndicator.js")]
    [ResponseCache(Duration = 60)]
    public IActionResult EnvIndicator()
    {
        var env = resolver.Resolve(Request.Host.Host ?? string.Empty);
        if (env == null)
            return Content("/* DXP environment indicator: no badge for this environment */", "application/javascript; charset=utf-8");

        var prelude = $"var __DXP_ENV={JsonSerializer.Serialize(env.Name.ToUpperInvariant())};"
                    + $"var __DXP_COLOR={JsonSerializer.Serialize(env.Color)};\n";
        return Content(prelude + EnvIndicatorScript, "application/javascript; charset=utf-8");
    }

    // Watches location.hash; when it matches our route, overlays an iframe of the standalone settings
    // page over the admin content pane (anchored to the right of the side-bar nav, which is present on
    // every admin route including ours) and hides it again on navigation elsewhere.
    private const string AdminInitScript = """
    (function () {
        var ROUTE = '#/EnvIndicator/Settings';
        var FRAME_ID = 'dxp-env-settings-frame';
        var SETTINGS_URL = '/EPiServer/DxpEnvironmentIndicator/Admin/Settings';
        var NAV_SELECTOR = '.epi-side-bar-navigation';
        var CONTENT_SELECTOR = '.content-area-container';
        var trackTimer = null;

        function topOffset() {
            var nav = document.querySelector(
                '.epi-navigation, #epi-shellHeader, [class*="shellHeader"], [class*="GlobalNavigation"], header[role="banner"]');
            var bottom = nav ? Math.round(nav.getBoundingClientRect().bottom) : 0;
            return bottom > 0 ? bottom : 48;
        }

        function rectOf(selector, minW, minH) {
            var el = document.querySelector(selector);
            if (el) {
                var r = el.getBoundingClientRect();
                if (r.width > minW && r.height > minH) return r;
            }
            return null;
        }

        function applyGeometry(frame) {
            var nav = rectOf(NAV_SELECTOR, 100, 100);
            if (nav) {
                frame.style.top = nav.top + 'px';
                frame.style.left = nav.right + 'px';
                frame.style.width = Math.max(0, window.innerWidth - nav.right) + 'px';
                frame.style.height = nav.height + 'px';
                return;
            }
            var content = rectOf(CONTENT_SELECTOR, 100, 100);
            if (content) {
                frame.style.top = content.top + 'px';
                frame.style.left = content.left + 'px';
                frame.style.width = content.width + 'px';
                frame.style.height = content.height + 'px';
                return;
            }
            var t = topOffset();
            frame.style.top = t + 'px';
            frame.style.left = '0';
            frame.style.width = '100vw';
            frame.style.height = 'calc(100vh - ' + t + 'px)';
        }

        function showFrame() {
            var frame = document.getElementById(FRAME_ID);
            if (!frame) {
                frame = document.createElement('iframe');
                frame.id = FRAME_ID;
                frame.src = SETTINGS_URL;
                frame.title = 'DXP Environment Indicator Settings';
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
            if ((location.hash || '').indexOf(ROUTE) === 0) showFrame();
            else hideFrame();
        }

        window.addEventListener('hashchange', sync);
        window.addEventListener('resize', function () {
            var f = document.getElementById(FRAME_ID);
            if (f && f.style.display !== 'none') applyGeometry(f);
        });

        if (document.readyState === 'loading')
            document.addEventListener('DOMContentLoaded', sync);
        else
            sync();

        [250, 750, 1500].forEach(function (ms) { setTimeout(sync, ms); });
    })();
    """;

    // Reads __DXP_ENV / __DXP_COLOR from the prelude. Finds the product label cell ("CMS") in the top
    // navigation's centre section and inserts a coloured environment pill immediately after it, so the
    // bar reads "CMS [ENV]". The badge is appended (not an innerHTML rewrite) so it survives the SPA
    // re-rendering the label, and is idempotent via the .dxp-env-badge marker. The shell renders
    // asynchronously, so it retries via a MutationObserver until the cell exists, then disconnects —
    // with a hard 15s deadline so pages that never show the bar (e.g. login) don't observe forever.
    private const string EnvIndicatorScript = """
    (function () {
        var LABEL = '.epi-pn-navigation__section--align-center .flex--1.truncate';
        var deadline = Date.now() + 15000;

        function apply() {
            var label = document.querySelector(LABEL);
            if (!label) return false;
            var host = label.closest('.oui-dropdown-group') || label.closest('.oui-button') || label.parentElement;
            if (!host || !host.parentElement) return false;
            if (host.parentElement.querySelector('.dxp-env-badge')) return true;
            var badge = document.createElement('span');
            badge.className = 'dxp-env-badge';
            badge.textContent = __DXP_ENV;
            badge.style.cssText = 'display:inline-flex;align-items:center;padding:1px 7px;background:' + __DXP_COLOR +
                ';color:#fff;font-size:11px;font-weight:700;border-radius:3px;letter-spacing:0.5px;' +
                'margin-left:8px;flex-shrink:0;white-space:nowrap;';
            host.insertAdjacentElement('afterend', badge);
            return true;
        }

        function start() {
            if (apply()) return;
            var obs = new MutationObserver(function () {
                if (apply() || Date.now() > deadline) obs.disconnect();
            });
            obs.observe(document.body, { childList: true, subtree: true });
            setTimeout(function () { obs.disconnect(); }, 15000);
        }

        if (document.readyState === 'loading')
            document.addEventListener('DOMContentLoaded', start);
        else
            start();
    })();
    """;
}
