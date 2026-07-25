namespace TSTHRMS.Api;

/// <summary>
/// A friendly landing page for GET "/" on a split deployment (Coolify) where nothing else would
/// otherwise answer at the API's own root - the frontend lives on a different origin (Vercel),
/// so no one browses here except to sanity-check the API is up. Only registered when wwwroot has
/// no index.html (see Program.cs) - a single-origin deployment (docs/deployment-windows-server-iis.md)
/// serves the real SPA at "/" instead, and this page must never shadow that.
/// </summary>
public static class StatusPage
{
    public const string Html = """
        <!doctype html>
        <html lang="en">
        <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>TSTHRMS API</title>
        <style>
          :root { color-scheme: light dark; }
          * { box-sizing: border-box; }
          body {
            margin: 0;
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            background: radial-gradient(circle at 50% 0%, #e8f0fe, #f4f6fb);
            color: #0f172a;
          }
          .card {
            background: #ffffff;
            border-radius: 20px;
            box-shadow: 0 20px 45px rgba(15, 23, 42, 0.12);
            padding: 40px 56px;
            text-align: center;
            max-width: 420px;
          }
          .pill {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            font-size: 14px;
            font-weight: 600;
            color: #16a34a;
            margin-bottom: 16px;
          }
          .dot {
            width: 10px;
            height: 10px;
            border-radius: 999px;
            background: #22c55e;
            box-shadow: 0 0 0 4px rgba(34, 197, 94, 0.18);
          }
          h1 { margin: 0 0 8px; font-size: 28px; letter-spacing: -0.02em; color: #0f172a; }
          .subtitle { margin: 0; color: #64748b; font-size: 15px; }

          /* Declared last on purpose - a rule earlier in the file with equal specificity (e.g.
             the plain .card/.subtitle rules above) would otherwise win the cascade and silently
             override these dark-mode colors back to their light-mode values. */
          @media (prefers-color-scheme: dark) {
            body { background: radial-gradient(circle at 50% 0%, #16213a, #0b1120); color: #e2e8f0; }
            .card { background: #111827; box-shadow: 0 20px 45px rgba(0,0,0,0.45); }
            h1 { color: #f8fafc; }
            .subtitle { color: #94a3b8; }
          }
        </style>
        </head>
        <body>
          <div class="card">
            <div class="pill"><span class="dot"></span>API is online</div>
            <h1>TSTHRMS API is Running</h1>
            <p class="subtitle">The API is online and accepting requests.</p>
          </div>
        </body>
        </html>
        """;
}
