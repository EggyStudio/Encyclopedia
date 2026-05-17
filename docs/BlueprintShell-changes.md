# BlueprintShell — change requests for the Encyclopedia wiki

Target package: `BlueprintShell 0.1.2` (https://github.com/EggyStudio/BlueprintShell)

This wiki uses BlueprintShell as the application shell-builder and BlazorBlueprint
as the component library. The package was designed around a *dockable editor*
experience (`[EditorPanel]`, dock zones, header chrome). To host a public,
Wikipedia-style reader **and** an authenticated editor inside the same shell, we
need a handful of additions / loosenings. Each item below is scoped and labeled
with priority.

Legend: **P0** = blocker for v1, **P1** = strongly desired, **P2** = nice to have.

---

## 1. P0 — `[ReaderPage]` attribute (or equivalent) for non-dockable routed pages

**Problem.** `[EditorPanel]` registers a panel inside a dock zone. Wikipedia-style
reader pages are full-bleed, centered content with no dock chrome. We do not
want every reader page wrapped in panel headers, dock splitters, or close buttons.

**Request.** Add an attribute (working name `[ReaderPage]`) that registers a
Blazor component as a routed page rendered with a minimal layout — no dock
splitters, no panel header, no panel close button — but still inside the shell
host so theming, services, and `ShellRegistry` features (icons, dark-mode,
auth state) are available.

```csharp
[ReaderPage("article", "/wiki/{Identifier}", Layout = typeof(ReaderLayout))]
public partial class Article : ComponentBase { ... }
```

If you'd rather not add an attribute, exposing `ShellRegistry.RegisterRoute(...)`
that takes a `RouteAttribute` + `Layout` would let consumers build their own.

---

## 2. P0 — Conditional shell chrome (`ShellChromeMode`)

**Problem.** Anonymous visitors should see clean Wikipedia-style pages. Logged-in
editors should see the full editor shell (dock zones, toolbar, panel tabs) when
they hit `/edit/*`.

**Request.** Add `BlueprintShellOptions.ChromeMode` (or similar) with values:

- `Full` — current behavior, all chrome visible.
- `Minimal` — no dock splitters, no panel headers, only header bar.
- `Hidden` — no chrome at all; just `<CascadingValue>` of shell services around `@Body`.

And/or accept a `Func<HttpContext, ShellChromeMode>` so the consumer can pick
per-request (e.g. by route prefix).

```csharp
builder.Services.AddBlueprintShell(o =>
{
    o.AppTitle = "Encyclopedia";
    o.ChromeFor = ctx => ctx.Request.Path.StartsWithSegments("/edit")
        ? ShellChromeMode.Full
        : ShellChromeMode.Hidden;
});
```

---

## 3. P0 — Public re-export of BlazorBlueprint primitives

**Problem.** Consumers want the shadcn-style BlazorBlueprint components
(`Button`, `Card`, `Input`, `Tabs`, `Dialog`, `Tooltip`, …) inside their own
pages without separately referencing `BlazorBlueprint.Components` versions that
might drift from what the shell ships.

**Request.** Either:

- Re-export the relevant `BlazorBlueprint.Components` types from
  `BlueprintShell.Components` (a `using static` namespace alias would also work), **or**
- Add a transitive `<PackageReference ... PrivateAssets="none" />` on
  `BlazorBlueprint.Components` so consumers automatically get the same major
  version the shell was built against.

Also publish a `_Imports.razor` snippet in `contentFiles/any/net10.0/` that
imports the standard namespaces, so users get one-line drop-in.

---

## 4. P0 — Theme presets (`reader` vs `editor`) toggleable at runtime

**Problem.** Reader pages want softer, serif-friendly content typography
(Wikipedia-like). Editor pages want a tighter, monospace-friendly IDE feel.
Currently the OKLCH variable set is global.

**Request.** Add a `ThemePreset` concept on `ShellRegistry`:

```csharp
registry.RegisterTheme("reader", new ThemePreset
{
    Variables = new Dictionary<string, string>
    {
        ["--background"] = "oklch(1 0 0)",
        ["--foreground"] = "oklch(0.15 0 0)",
        ["--font-sans"]  = "\"Source Serif Pro\", Georgia, serif",
    },
});

// Switch at runtime:
shellTheme.SetActive("reader");
```

Implementation suggestion: render a `<style id="bp-active-theme">` block whose
contents come from the active preset, swap on theme switch via JS interop.

---

## 5. P1 — Mobile/responsive dock fallback

**Problem.** On phones, dock zones don't work — there's no room. Right now the
dock splitters still appear and squish content.

**Request.** Below a configurable viewport width (default 768px), automatically
collapse all dock zones into a single scrollable stack with the panel header
becoming a section header. Expose an option:

```csharp
o.MobileBreakpointPx = 768;
o.MobileBehavior     = MobileBehavior.Stacked; // or .Drawer, .Hidden
```

---

## 6. P1 — PWA hooks

**Problem.** We want the wiki to be installable as a PWA, with the same theme
colors and offline-cached shell assets the BlueprintShell already serves.

**Request.** Add a helper:

```csharp
app.MapBlueprintShellPwa(new BlueprintShellPwaOptions
{
    ManifestPath      = "/manifest.webmanifest",
    ServiceWorkerPath = "/sw.js",
    ThemeColor        = "#0a0a0a",
    BackgroundColor   = "#ffffff",
    ShortName         = "Encyclopedia",
    DisplayMode       = "standalone",
    CacheStaticAssets = true,   // BlueprintShell's own CSS / fonts / icons
});
```

The shell knows the paths of its own static assets (`staticwebassets/css/...`,
`staticwebassets/styles/...`), so it can generate a sensible default cache list
for the service worker.

---

## 7. P1 — Per-request auth state surfaced to `ShellRegistry`

**Problem.** Reader vs editor chrome (item 2) needs to know whether the request
is from a logged-in editor. We resolve auth from a client-side account file
(see app docs) but the shell makes routing/chrome decisions before our code
gets a chance.

**Request.** A scoped service `IShellAuthContext` we can implement, that the
shell calls when deciding chrome / panel visibility:

```csharp
public interface IShellAuthContext
{
    bool   IsAuthenticated { get; }
    string? UserId           { get; }
    IReadOnlySet<string> Roles { get; }
}

builder.Services.AddScoped<IShellAuthContext, MyAuthContext>();
```

`[EditorPanel(..., RequiresRole = "editor")]` would also be a clean hook.

---

## 8. P1 — `IEditorShellBuilder` discovery from external assemblies

**Problem.** README says "discovered at startup by `StaticShellLoader` via
reflection" — but it isn't clear whether it scans the entry assembly only or
also referenced assemblies. We want to put builders in feature-specific class
libraries (e.g. `Encyclopedia.Editor`, `Encyclopedia.Reader`) and have them
auto-load.

**Request.** Either confirm in the README that all loaded assemblies are
scanned, or add an explicit `AddBlueprintShell(o => o.ScanAssemblies = new[] { ... })`.

---

## 9. P2 — Routable `[EditorPanel(Route = ...)]` deep-link state

**Problem.** A user editing `/edit/article/cathode-ray-tube` should land
directly in the edit panel with the article loaded. Right now `Route` puts a
nav link in the header, but the panel doesn't see route parameters.

**Request.** Make the panel component receive `[Parameter] public string?
Identifier { get; set; }` when reached via its `Route`, the same way standard
`@page` directives do.

---

## 10. P2 — Slot for app-supplied top-nav between header brand and dark-mode toggle

**Problem.** We need a global search box, a "Discover wikis" link, and a
"Contribute" CTA visible from every page. Today the only customization point is
panels.

**Request.** Expose a `ShellHeaderSlot` (a `RenderFragment` registered on
`ShellRegistry`) that the shell renders in its top bar between the app title
and the dark-mode toggle.

```csharp
registry.RegisterHeaderSlot(builder =>
{
    builder.OpenComponent<GlobalSearchBox>(0);
    builder.CloseComponent();
});
```

---

## 11. P2 — Public `BlueprintShellOptions.StaticAssetsBasePath`

**Problem.** When embedding inside an existing ASP.NET Core app we have no
clean way to relocate `/_content/BlueprintShell/...` if it collides with
something else, and the path is hard-coded into the shell's CSS imports.

**Request.** Accept `StaticAssetsBasePath` on the options and rewrite CSS
imports through it.

---

## 12. P2 — Diagnostics endpoint

**Problem.** When something doesn't render we have to guess whether a panel
was discovered, which source registered it, and what theme is active.

**Request.** A dev-only endpoint (e.g. `GET /_shell/diagnostics`) that returns
JSON describing discovered panels, sources, precedence resolution, active
theme, and loaded assemblies. Behind `IsDevelopment` or an explicit opt-in.

---

## Open questions for the BlueprintShell maintainer

1. Is there a published roadmap or version that already addresses any of the
   P0 items? If so, we'll align rather than fork.
2. Does `MapBlueprintShell()` add middleware that intercepts requests
   matching shell routes only, or does it install a global handler? (Affects
   how we route reader pages.)
3. Is the SignalR hub path `/shell-hub` configurable? We may want to namespace
   it to avoid collisions with other SignalR endpoints we add later.
4. Does the shell hold any singletons that pin per-tenant state? We want a
   single Fly.io app to potentially serve multiple wikis later.

---

## Workarounds in use until the above ships

- **Item 1/2 (chrome)** — we host reader pages on plain Blazor routes outside
  the shell's panel system, and only call `MapBlueprintShell()` under
  `/edit/*` via `app.MapWhen(...)`. This is brittle if the shell installs
  global static-asset middleware; revisit when item 2 lands.
- **Item 3 (primitives)** — we add direct `<PackageReference>`s to
  `BlazorBlueprint.Components` etc. and pin them to the same versions the
  shell's nuspec declares. Risk: version drift on shell update.
- **Item 4 (themes)** — we ship two `<link rel="stylesheet">` blocks and
  toggle them with a `data-theme` attribute on `<html>`. Works but means the
  shell's own variables are not part of our preset.
- **Item 6 (PWA)** — we hand-write the manifest and service worker.
- **Item 7 (auth)** — we use a `CascadingValue<AccountState>` ourselves; the
  shell doesn't see it for routing decisions.
