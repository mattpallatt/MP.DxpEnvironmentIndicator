using Microsoft.AspNetCore.Http;
using System.Text;

namespace MP.DxpEnvironmentIndicator.Middleware;

// Buffers the response, and if it is an HTML document, splices a <script> tag in just before the
// closing </body>. Idempotent — if the tag is already present the body is written through untouched.
// Non-HTML responses are streamed straight back. The caller decides which requests to run this on.
internal static class HtmlBodyInjector
{
    public static async Task InjectBeforeBodyCloseAsync(HttpContext context, RequestDelegate next, string scriptTag)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        // 304 Not Modified and 204 No Content must have no body. ASP.NET Core throws if
        // anything is written to the response stream for these status codes.
        var status = context.Response.StatusCode;
        if (status == 304 || status == 204 || status < 200)
            return;

        buffer.Position = 0;
        var contentType = context.Response.ContentType ?? string.Empty;

        if (!contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            await buffer.CopyToAsync(originalBody);
            return;
        }

        var html = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync();

        if (html.Contains(scriptTag, StringComparison.OrdinalIgnoreCase))
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            return;
        }

        var bodyClose = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        if (bodyClose < 0)
        {
            var raw = Encoding.UTF8.GetBytes(html);
            context.Response.ContentLength = raw.Length;
            await originalBody.WriteAsync(raw);
            return;
        }

        var modified = html[..bodyClose] + scriptTag + html[bodyClose..];
        var bytes = Encoding.UTF8.GetBytes(modified);
        context.Response.ContentLength = bytes.Length;
        await originalBody.WriteAsync(bytes);
    }
}
