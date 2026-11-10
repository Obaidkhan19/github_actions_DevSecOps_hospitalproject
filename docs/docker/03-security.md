# 03 - Docker security

> **Like you are 10:** Imagine your app lives in a **house**. Security means: (1) the house has **few doors and windows**, (2) the people inside get a **visitor badge** instead of master keys, (3) the **valuable stuff is in a locked room** nobody on the street can reach, and (4) a **guard checks the house** for broken locks every week.

Your project already has a DevSecOps pipeline (CodeQL, Gitleaks, Trivy, ZAP...). Docker adds a new layer to protect. Here is every protection in this repo and where to find it.

## 1. The protections

| # | Rule | Kid version | Where it is |
|---|------|-------------|-------------|
| 1 | **Do not run as root** | visitor badge, not master key | `Dockerfile` → `USER ${APP_UID}` |
| 2 | **Small base image** | fewer windows to break | `Dockerfile` → `aspnet:...-alpine` |
| 3 | **Multi-stage build** | no tools or source code left lying around | `Dockerfile` (3 stages) |
| 4 | **No secrets in the image** | never write the safe's code on the box | no passwords in `Dockerfile`; `.dockerignore` blocks `.env` |
| 5 | **Program files owned by root** | the app can run itself but not rewrite itself | `COPY --from=build` without `--chown` |
| 6 | **Read-only filesystem** | the walls cannot be painted over | `docker-compose.yml` → `read_only: true` + `tmpfs: /tmp` |
| 7 | **Drop Linux powers** | take away all special tools | `cap_drop: [ALL]` |
| 8 | **No new privileges** | a badge can never be upgraded | `security_opt: no-new-privileges:true` |
| 9 | **No published port for the database** | the safe has no door to the street | `db:` has no `ports:` |
| 10 | **Internal network** | the back room has no door to the internet | `backend: internal: true` |
| 11 | **Resource limits** | one tenant cannot use all the electricity | `deploy.resources.limits` |
| 12 | **Required secrets** | refuse to start without a password | `${MSSQL_SA_PASSWORD:?...}` |
| 13 | **Admin tools only on 127.0.0.1** | the dashboard is only visible from inside your own room | `dozzle` → `127.0.0.1:9999:8080` |
| 14 | **Scan the image** | the weekly guard | `.github/workflows/docker_publish_devsecops.yml` (Trivy) |

## 2. The important ones, explained

**1 · Non-root.** By default a container's program is `root` *inside* the container. If a hacker breaks the app, they are root in there, which makes escaping easier. We switch to user `1654` (the `app` user of the .NET image). We use the **number**, not the name, because Kubernetes' `runAsNonRoot` can only verify numbers. A side effect: non-root cannot open ports below 1024, which is why we use **8080**, not 80.

Check it: `docker compose exec web id` → `uid=1654(app)`.

**4 · Secrets.** Anything you write in a Dockerfile (`ENV PASSWORD=...`) or `COPY` into an image can be read by **anyone who gets the image**: `docker history --no-trunc IMAGE` and `docker run IMAGE env`. So:
- passwords come in **when the container starts** (`.env` → `environment:`);
- `.env` is in `.gitignore` (Git) and `.dockerignore` (Docker);
- `.env.example` contains only fake placeholders (Gitleaks watches for real ones).
Next level: Docker/Kubernetes **Secrets**, or a vault (Azure Key Vault, HashiCorp Vault).

**6-8 · Hardening the running container.** Think "lock everything that the app doesn't need":
- `read_only: true` - if an attacker gets in, they cannot drop a program onto the disk. The app still writes to the two places we allow: the `dpkeys` volume and `/tmp` (RAM).
- `cap_drop: ALL` - Linux splits root's power into ~40 small "capabilities" (change file owner, open raw network sockets...). Our app needs none.
- `no-new-privileges` - blocks tricks like `sudo`-style upgrades.

> If the web container ever refuses to start after you change something, temporarily comment out `read_only: true` to find out whether the filesystem is the cause, then give the new writable folder a volume or `tmpfs`.

**9-10 · Network hiding.** The database should only talk to the app. In `docker-compose.yml` it has no `ports:` and lives on `backend`, a network marked `internal: true`: containers there cannot reach the internet, and the outside world cannot reach them. Only `web` is on both networks ([04](04-networking.md)).

## 3. Dangerous things to avoid

| Don't | Why |
|---|---|
| `docker run --privileged` | switches off almost all protection |
| Mounting `/var/run/docker.sock` into a container | whoever controls that socket controls the **whole host** (they can start a root container that mounts the host disk). Our optional `dozzle` does it read-only, **for local use only** |
| `FROM image:latest` in production | the base changes without warning; pin a version or a digest |
| Random images from unknown publishers | use **Docker Official Images**, Verified Publishers, or Microsoft (`mcr.microsoft.com`) |
| Running as root "because it was easier" | see rule 1 |
| `-p 1433:1433` for the database "just to test" | it opens the safe to the street; use `docker compose exec db ...` instead |

## 4. Scanning (the weekly guard)

Images contain hundreds of packages. New vulnerabilities (CVEs) are published every day, so an image that was clean yesterday can be risky today.

```bash
# scan your local image (Trivy runs as a container itself)
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock aquasec/trivy:latest image --severity HIGH,CRITICAL hospital-web:dev
```

In CI: `docker_publish_devsecops.yml` scans the image **before** pushing. It starts in "report only" mode (`exit-code: '0'`); switch to `'1'` to block releases with HIGH/CRITICAL findings.

How the pieces of your pipeline fit now:

| Workflow | Looks at |
|---|---|
| `sast_devsecops.yml` (CodeQL) | your C# code |
| `dependency_scanning_devsecops.yml` | NuGet packages |
| `secrets_scanning_devsecops.yml` (Gitleaks) | leaked secrets, also in `.env.example` |
| `trivy_devsecops.yml` | project files |
| **`docker_publish_devsecops.yml` (new)** | **the finished Docker image** |
| `dast_devsecops.yml` (ZAP) | the running website |

**Keep it healthy:** rebuild regularly (`docker build --pull --no-cache`) to get patched base images, and pin versions so you choose *when* to change.

## Kubernetes later

Almost every rule has an exact twin: `runAsNonRoot`, `readOnlyRootFilesystem`, `capabilities.drop: [ALL]`, `allowPrivilegeEscalation: false`, `Secret`s, `NetworkPolicy`, `resources.limits`, and admission rules. See the table in the [README](README.md).
