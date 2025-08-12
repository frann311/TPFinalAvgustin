public class JwtRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public JwtRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();

        // Si ya estás en /account/login o es una API, dejar pasar
        if (path != null && (
    path.Contains("/account/login") ||
    path.Contains("/account/signin") ||
    path.StartsWith("/api")))

        {
            await _next(context);
            return;
        }

        var token = context.Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(token))
        {
            context.Response.Redirect("/Account/Login");
            return;
        }

        await _next(context);
    }
}
