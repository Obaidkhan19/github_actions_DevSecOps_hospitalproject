# 02 - Building images: Dockerfile, layers, multi-stage, optimization

> **Like you are 10:** The Dockerfile is a **recipe**. `docker build` is **baking**. The image is the **cake**. A good baker (1) keeps the kitchen tools out of the box that gets delivered, and (2) never re-does steps that are already done.

File to open next to this guide: **`Dockerfile`** (repo root).

## 1. The Dockerfile, line by line

| Line | What it does | Kid version |
|------|--------------|-------------|
| `FROM ... AS build` | start from an existing image | "start from a ready-made kitchen" |
| `WORKDIR /src` | go to a folder (creates it) | "stand at this table" |
| `COPY a b` | copy from your computer into the image | "put the ingredients on the table" |
| `RUN cmd` | run a command **while building** | "mix the dough" |
| `ENV K=V` | a note available while building **and** running | "write on the box" |
| `ARG K=V` | a setting only while building | "a choice before baking" |
| `USER 1654` | from here on, do not be the boss (root) | "wear a visitor badge" |
| `EXPOSE 8080` | a sticky note: "I listen here" (opens nothing) | "the door number" |
| `HEALTHCHECK` | a question Docker asks: "are you OK?" | "the nurse" |
| `ENTRYPOINT [...]` | the program that starts when the container starts | "press play" |
| `LABEL` | name tag on the image | "a sticker" |

`RUN` happens at **build time** (once, makes a layer). `ENTRYPOINT` happens at **run time** (every start).

## 2. Layers and the cache (the secret of fast builds)

Every instruction makes a layer ([01 §4](01-basics-and-objects.md)). Docker caches a layer if **the instruction and everything before it are unchanged**. When a layer changes, **all later layers are rebuilt**.

So: put what changes **rarely** at the top, what changes **often** at the bottom.

Look at the build stage of our `Dockerfile`:

```dockerfile
COPY src/.../HospitalManagement.Web.csproj src/HospitalManagement.Web/   # changes rarely
RUN dotnet restore ...                                                    # SLOW (downloads packages)
COPY src/HospitalManagement.Web/ src/HospitalManagement.Web/              # changes every commit
RUN dotnet publish ...
```

You edit `PatientsController.cs` → only the last two steps run again, the slow restore says `CACHED`. If we had written `COPY . .` first, *every* edit would redownload all packages.

**Try it**
```bash
docker build -t hospital-web:dev .                 # first build: slow
docker build -t hospital-web:dev .                 # again: all CACHED, seconds
# edit any .cs file, then build again: only copy + publish run
docker build --no-cache -t hospital-web:dev .      # ignore the cache (when something is odd)
docker build --progress=plain -t hospital-web:dev . # show every line of output
```

## 3. Multi-stage build (three kitchens)

To compile C# you need the **.NET SDK**: huge (hundreds of MB), full of compilers. To *run* the finished app you need only the **runtime**: much smaller. If we put both in one image, we ship a whole kitchen with every pizza.

A **multi-stage build** uses several `FROM` lines. Each `FROM` starts a fresh stage. We build in a big stage and **copy only the result** into a small final one:

```
 ┌─ STAGE build ───────────────┐        ┌─ STAGE final ──────────────┐
 │ dotnet/sdk (big)            │        │ dotnet/aspnet-alpine (tiny)│
 │ source code, NuGet cache    │  COPY  │ ONLY the published app     │
 │ dotnet restore + publish ───┼───────▶│ non-root user, healthcheck │ ← this is shipped
 └─────────────────────────────┘ --from └────────────────────────────┘
        thrown away                              (~hundreds of MB smaller)
 ┌─ STAGE test ────────────────┐
 │ build + the xUnit tests     │ ← only when you ask: --target test
 └─────────────────────────────┘
```

The key line: `COPY --from=build /app/publish ./`

```bash
docker build -t hospital-web:dev .                  # builds the last stage: "final"
docker build --target test .                        # builds up to "test" = runs your unit tests
docker build --target build -t hospital-sdk:debug . # stop at "build" to look inside
docker image ls                                     # compare the SIZE of hospital-sdk vs hospital-web
```

Docker (BuildKit) only builds stages the target really needs, so a normal build skips `test` completely.

Bonus: the source code and compilers are **not** in the shipped image → smaller *and* safer ([03](03-security.md)).

## 4. Optimization checklist (and where we did each one)

| # | Trick | Why | Where |
|---|-------|-----|-------|
| 1 | **Multi-stage build** | ship only the runtime + app | `Dockerfile` stages |
| 2 | **Small base image** (`aspnet:10.0-alpine`, not the SDK) | less to download, fewer vulnerabilities | `final` stage `FROM` |
| 3 | **Order layers: rare → often changing** | the cache works | restore before copying code |
| 4 | **`.dockerignore`** | smaller build context, stable cache, no secrets | `.dockerignore` |
| 5 | **`--no-restore`** on publish | do not repeat work | `RUN dotnet publish` |
| 6 | **`UseAppHost=false`** | skip an extra launcher file | `RUN dotnet publish` |
| 7 | **Combine related commands with `&&`** | one layer instead of three | `mkdir ... && chown ...` |
| 8 | **Exec-form `ENTRYPOINT`** | clean, fast shutdown (`docker stop`) | last line |
| 9 | **Resource limits** | one app cannot eat the computer | `docker-compose.yml` → `deploy.resources` |
| 10 | **Log rotation** | logs cannot fill the disk | `docker-compose.yml` → `x-logging` |

**Measure, do not guess**
```bash
docker image ls hospital-web                # total size
docker history hospital-web:dev             # size of every layer - find the fat one
docker system df                            # disk used by images / cache / volumes
```

**More tricks for later (not needed now)**
- **BuildKit cache mounts:** `RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore` keeps the NuGet cache between builds, even when the csproj changes.
- **Chiseled images** (`aspnet:10.0-noble-chiseled`): even smaller, no shell, no package manager. Excellent for Kubernetes. Because they have no `wget`, you would drop the Dockerfile `HEALTHCHECK` and rely on Kubernetes probes.
- **Pin by digest** (`FROM image@sha256:...`): the exact same base forever.
- **Multi-architecture images** (amd64 + arm64) with `docker buildx build --platform linux/amd64,linux/arm64`.
- Do **not** turn on assembly trimming for ASP.NET MVC apps: it can break Razor views.

## 5. Why Alpine, and one honest warning

Alpine Linux is tiny, which is great. The price: it uses a different C library (`musl`) and, in the official .NET image, **globalization is "invariant"** (no ICU culture data). For this app that is fine. If you ever need real culture formatting (for example German date formats in views), add `RUN apk add --no-cache icu-libs tzdata` and `ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` to the `final` stage.

## 6. The build context

`docker build .` - the dot is the **build context**: the folder Docker packs and sends to the builder. `COPY` can only see files inside it. `.dockerignore` removes things from that package (`bin/`, `obj/`, `.git`, `.env`, docs). Check what you send: `docker build --progress=plain .` prints `transferring context: ...`.

## Kubernetes later

The image you build here is exactly what Kubernetes runs. Keep the tag **meaningful** (`1.0.0`, `sha-...`, see [07](07-registry-hub-deploy.md)) and never rebuild the same tag with different content.
