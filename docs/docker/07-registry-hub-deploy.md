# 07 - Registry, Docker Hub, and deploying to a real server

> **Like you are 10:** You baked a cake (the image). Your friend lives far away. You cannot carry it in your hands, so you put it in a **bakery shelf on the internet** (a **registry**). Your friend's computer walks to the shelf and takes a copy. **Docker Hub** is the biggest public bakery shelf.

Open next to this guide: **`.github/workflows/docker_publish_devsecops.yml`**.

## 1. Registry, repository, tag

```
          docker.io / ghcr.io / myregistry:5000          <- REGISTRY   (the whole bakery)
                       │
                yourname/hospital-web                    <- REPOSITORY (one kind of cake)
                       │
                     :1.0.0                               <- TAG        (a version label)
```
Full name: `[registry/]namespace/repository[:tag]` - no registry means Docker Hub (`docker.io`); no tag means `:latest`.

Examples: `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (Microsoft's registry), `nginx:alpine` (Docker Hub official), `yourname/hospital-web:1.0.0` (your Docker Hub repo).

**Which registries exist?**

| Registry | Notes |
|---|---|
| **Docker Hub** | the default; public repos free; a few private repos free |
| **GitHub Container Registry** `ghcr.io` | next to your code; login with `GITHUB_TOKEN` (no extra secret) |
| **Azure ACR / AWS ECR / Google Artifact Registry** | the cloud ones; Terraform will create these later |
| **Self-hosted** (`registry:2`, Harbor) | your own shelf |

## 2. Tags - the label on the box

A tag is a **moving sticker**: `latest` points to "whatever was pushed last". Never deploy `latest` to production - nobody knows which version runs. Use:

| Tag | Use |
|---|---|
| `1.0.0` | an exact release (never changes) |
| `1.0` | "newest 1.0.x" |
| `sha-1a2b3c4` | the exact Git commit |
| `@sha256:...` (digest) | the content itself; cannot be faked |

The workflow creates `1.0.0`, `1.0` and `sha-...` for you.

## 3. Docker Hub by hand

```bash
docker login                                      # use an ACCESS TOKEN as password, never your real password
docker build -t yourname/hospital-web:1.0.0 .     # or set IMAGE_NAME / IMAGE_TAG in .env
docker tag hospital-web:dev yourname/hospital-web:1.0.0   # add a second name to an existing image
docker push yourname/hospital-web:1.0.0           # upload
docker pull yourname/hospital-web:1.0.0           # download (any computer)
docker logout
```
**Access token:** hub.docker.com → Account Settings → Personal access tokens. A token can be limited and revoked; your password cannot.

**Public or private?** A *public* repo can be pulled by everyone, which is fine for a demo but never for real patient software. Create a **private** repo, and on servers run `docker login` first.

## 4. Your own registry in 3 commands (to understand it)

```bash
docker run -d -p 5000:5000 --name registry registry:2
docker tag hospital-web:dev localhost:5000/hospital-web:dev
docker push localhost:5000/hospital-web:dev      # stored in YOUR registry
docker pull localhost:5000/hospital-web:dev
```

## 5. The automatic way: the workflow

`docker_publish_devsecops.yml` is written in the same style as your other workflows (big comment header, independent, secure defaults).

```
 git tag v1.0.0 ──► GitHub Actions ──► build ──► Trivy scan ──► login ──► push ──► Docker Hub
 git push origin v1.0.0
```

**One-time setup** (also written at the top of the workflow):
1. Docker Hub → create a token (Read & Write).
2. GitHub → Settings → Secrets and variables → Actions: variable `DOCKERHUB_USERNAME`, secret `DOCKERHUB_TOKEN`.

**Release:** `git tag v1.0.0 && git push origin v1.0.0` (or "Run workflow" for a test).

Security details: the token is a **secret** (hidden in logs), the workflow has `permissions: contents: read` only, the scan happens **before** the push, and the action versions are fixed.

## 6. Using GHCR instead

`ghcr.io` needs no extra secret. Section 7 of the workflow header lists the 4 changes (registry, username, password `GITHUB_TOKEN`, `packages: write`). Image names there must be lower case.

## 7. Deploying on a real server (Docker Hub → server)

"Deploying" = a server pulls your image and runs it. For a small single server:

```bash
# on the server (Linux with Docker installed), in a folder containing docker-compose.yml and .env
docker login                                   # once, if the repo is private
# in .env:  IMAGE_NAME=yourname/hospital-web   IMAGE_TAG=1.0.0
docker compose pull web                        # download exactly that version
docker compose up -d --no-build                # start / update without building
docker compose ps
```

**Update to 1.0.1:** change `IMAGE_TAG`, run the same two commands. Compose recreates only `web`; the database volume is untouched. **Roll back:** put `1.0.0` back and run them again - this is why we use exact tags.

**Server checklist**
- Do **not** copy the source code: only `docker-compose.yml` + `.env` (a bare server does not need the `build:` part, hence `--no-build`).
- Set `SECURE_COOKIES=true` once you serve HTTPS, and put a reverse proxy with a certificate in front (Caddy, Traefik, nginx).
- Do **not** use the `dev` file or the `monitoring` profile on a public server.
- Backups for the `sqldata` volume ([05 §6](05-storage-mounting.md)).
- Production SQL Server needs a proper licence (not the Developer edition).

## Kubernetes and Terraform later

- The registry is the handoff point: Kubernetes `image: yourname/hospital-web:1.0.0` pulls from here (private → `imagePullSecrets`).
- **Terraform** can create the registry (Azure ACR / AWS ECR), the cluster and the database. The tag stays the only thing that changes per release.
- A Terraform "docker" provider can even run these containers locally, a nice stepping stone.
