# Instagram gallery setup

The website keeps its existing swipe gallery. Instagram photos, carousel children, and Reels populate the same cards. Reels have native video controls. Website pins appear first; lower display-order numbers come first within pinned/unpinned groups, followed by newest posts. These pins are independent of Instagram profile pinning.

## Where to paste your token

This integration uses an **Instagram User access token**, not a Meta app ID or app secret. Use a Business or Creator Instagram account and a Meta developer app configured for **Instagram API with Instagram Login**, with the `instagram_business_basic` permission. Authorize your account and obtain a **long-lived** token using Meta's documented token exchange. App roles/tester acceptance and app review requirements depend on whether you connect your own account or other users' accounts. This implementation is for one server-configured account; it does not add an OAuth login screen.

Meta documentation: https://developers.facebook.com/docs/instagram-platform/instagram-api-with-instagram-login/

In your **backend host's environment settings** (for example Render → your API service → Environment), add:

| Name | Value |
| --- | --- |
| `Instagram__AccessToken` | Paste your long-lived Instagram User access token here |
| `Instagram__AdminKey` | A separate random password of at least 32 characters for website gallery management |
| `Instagram__ApiVersion` | `v22.0` (change to a supported Meta version when upgrading) |
| `Instagram__SyncMinutes` | `30` (optional; minimum 5) |

Generate the management key with a password manager. **Do not paste your Instagram token into Angular, the media manager, Git, or chat.** Empty placeholders in `dotnet-server/appsettings.json` document configuration only; put real credentials in your host's secret/environment settings. Use HTTPS for deployed frontend and API traffic. The management key protects only the new gallery endpoints; it does not secure the repository's existing unrelated admin endpoints.

For a local Bash session, enter the values without placing them in shell history:

```bash
read -rsp 'Instagram access token: ' Instagram__AccessToken; echo
export Instagram__AccessToken
read -rsp 'Media admin key (32+ characters): ' Instagram__AdminKey; echo
export Instagram__AdminKey
```

Run the backend from that same session, or restart/redeploy it after updating your host's environment settings. Environment variable configuration overrides the empty JSON placeholders. On this cloud machine, first source `/workspace/.setup-tools/env.sh`, then enter the two variables above. Restart only the API process you own so it inherits the new values.

## Apply the database migration

From the repository root, with the backend database connection configured:

```bash
dotnet ef database update --project dotnet-server
```

On this cloud machine, use the prepared tools and external build outputs to avoid modifying tracked bin/obj files:

```bash
source /workspace/.setup-tools/env.sh
dotnet build dotnet-server/dotnet-server.csproj --artifacts-path /workspace/.setup-tools/artifacts
dotnet ef database update --project dotnet-server --no-build --msbuildprojectextensionspath /workspace/.setup-tools/artifacts/obj/dotnet-server
```

## Choose and pin posts

1. Open your website's `/admin/media` page.
2. Under **Instagram Gallery**, enter `Instagram__AdminKey` in **Media admin key**. This key stays in component memory; it is not saved to browser storage.
3. Click **Sync Instagram now**, then use **Visible**, **Pinned**, and **Order**, and click **Save post** for each edited post.
4. Refresh the landing page. Changes are stored in PostgreSQL and shared by all visitors.

New posts default to visible. To unpublish a synced post, clear Visible. Sync preserves visibility, pins, and ordering. A successful sync refreshes captions and temporary media URLs. The background worker runs at startup and every 30 minutes by default. Manual sync is available through the protected dashboard.

## Reliability and limitations

- Long-lived tokens expire (typically around 60 days). Refresh them through Meta's documented `refresh_access_token` flow while still valid, update `Instagram__AccessToken`, and restart/redeploy the backend. Automatic credential renewal is not implemented. Do not use an expiring short-lived test token for production.
- Sync fetches up to 500 recent parent posts. Keep your curated gallery within that recent window; older cached URLs may expire. Carousel children returned by Meta are displayed as individual cards.
- Saved posts are retained if upstream requests fail. They are not automatically removed when deleted from Instagram or when they fall outside the fetched window: hide them in the dashboard. Cached remote media can still fail if its URL expires before the next successful sync.
- The original hardcoded/browser-local gallery is a fallback before Instagram is configured or when the portfolio API cannot be reached. Hiding all synced posts intentionally produces an empty Instagram gallery.
- Allow backend HTTPS access to `graph.instagram.com`; viewers' browsers also need access to Instagram's media CDN. Google Fonts networking is separate.
- `GET /api/portfolio` is public and returns visible media only. Management reads, updates, and sync require `X-Media-Admin-Key`. Tokens and the management key are never returned by these endpoints.
- This setup does not replace the hero video or avatar automatically.
