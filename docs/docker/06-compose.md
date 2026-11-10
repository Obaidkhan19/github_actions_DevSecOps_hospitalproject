# 06 - Docker Compose

> **Like you are 10:** Starting containers by hand is like cooking a big dinner while shouting every instruction from memory. **Compose is the written menu**: "one web app, one database, put them on this street, give them these notes". You say `docker compose up` and the whole dinner is served.

Open next to this guide: **`docker-compose.yml`** (the menu), **`docker-compose.dev.yml`**, **`.env.example`**.

## 1. The shape of the file

```yaml
name: hospital          # project name = prefix for everything Compose creates
x-logging: &default-logging   # reusable block (anchor)
services:               # the containers
  db:  ...
  web: ...
  dozzle: ...
networks:               # the streets
volumes:                # the chests
```
A **service** is a *recipe for containers* ("web"); the actual running **container** is called `hospital-web-1`. (`docker compose up --scale web=3` would make 3 from one service.)

## 2. Services (`services:`)

| Key | Meaning | Example in our file |
|---|---|---|
| `image` | take this image | `mcr.microsoft.com/mssql/server:2022-latest` |
| `build` | build the image from a Dockerfile first | `context: .`, `target: final` |
| `restart` | what to do after a crash | `unless-stopped` |
| `depends_on` | start order | `web` waits for `db` to be **healthy** |
| `healthcheck` | how to ask "are you ready?" | `sqlcmd ... SELECT 1` |
| `profiles` | start only on request | `dozzle` → `--profile monitoring` |
| `logging` | log driver + rotation | `*default-logging` |
| `deploy.resources.limits` | CPU / memory cap | web: `1.0` CPU, `768M` |
| `read_only`, `cap_drop`, `security_opt` | hardening ([03](03-security.md)) | on `web` |

**Start order done right.** `depends_on` alone only waits until the db container has *started*; SQL Server needs another ~20 s to be *ready*. With `condition: service_healthy` Compose waits for the healthcheck to pass. This is why `web` starts clean.

## 3. Ports (`ports:`)

```yaml
ports:
  - "${WEB_PORT:-8080}:8080"      # "computer : container"
  - "127.0.0.1:9999:8080"         # only this computer (dozzle)
```
Always **quote** port mappings in YAML. `db` has none on purpose. See [01 §6](01-basics-and-objects.md) and [04 §6](04-networking.md).

## 4. Environment variables

Two layers - this is the part people confuse:

```
 .env  (your settings)  ──►  ${HOLES} in docker-compose.yml  ──►  environment: of the container  ──►  your .NET app
```

1. **`.env`** sits next to `docker-compose.yml`. Compose reads it automatically. (`.env.example` is the committed template.)
2. In `docker-compose.yml`, `${NAME}` is replaced by the value from `.env`:

| Syntax | Meaning |
|---|---|
| `${WEB_PORT}` | the value (empty if missing) |
| `${WEB_PORT:-8080}` | the value, or **8080** if missing |
| `${MSSQL_SA_PASSWORD:?message}` | **stop with `message`** if missing - no silent default passwords |

3. The `environment:` list becomes real environment variables **inside** the container, which ASP.NET reads: `Hospital__Name` → `Hospital:Name`.

Other ways: `env_file: [.env]` hands a whole file to the container; `docker compose run -e K=V ...` for one-offs. See the final result Compose will use:

```bash
docker compose config        # prints the file with all ${...} filled in (careful: shows passwords)
```

## 5. Volumes and networks

Declared at the bottom, then attached per service:

```yaml
services:
  db:
    volumes: [ "sqldata:/var/opt/mssql" ]      # named volume   (05)
    networks: [ backend ]                      # the closed street   (04)
volumes:
  sqldata:
networks:
  backend: { driver: bridge, internal: true }
```

## 6. Combining files (`docker-compose.dev.yml`)

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```
Files are merged: maps (like `environment`) are combined, lists (like `volumes`) are appended, the later file wins on conflicts. Check the merge: `docker compose -f docker-compose.yml -f docker-compose.dev.yml config`.

Naming trap: a file called exactly `docker-compose.override.yml` is loaded **automatically**. We named ours `.dev.yml` so it is never loaded by accident.

## 7. Commands you need

```bash
docker compose up -d --build            # build if needed, start everything in the background
docker compose ps                       # status and health
docker compose logs -f web              # follow one service's logs
docker compose exec web sh              # shell in a running container
docker compose exec db sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD"'   # SQL prompt inside the db container
docker compose stop / start / restart   # keep containers
docker compose down                     # remove containers + networks (volumes stay)
docker compose down -v                  # ... and volumes (DATA LOSS)
docker compose build --no-cache web     # rebuild the image from zero
docker compose pull                     # download newer images
docker compose --profile monitoring up -d   # also start the log viewer (08)
docker compose config                   # show the final merged file
docker compose run --rm web <command>   # one-off container
```

## 8. First run - what you should see

```bash
cp .env.example .env     # then edit the passwords
docker compose up -d --build
docker compose ps
```
`db` becomes **healthy** first (~30-60 s), then `web` starts, applies the migrations (`Database__MigrateOnStartup`), seeds the admin user and becomes healthy. Open http://localhost:8080.

**Troubleshooting**

| Symptom | Likely cause |
|---|---|
| `required variable MSSQL_SA_PASSWORD is missing a value` | you did not create `.env` |
| `db` unhealthy / restarts | password too weak (SQL Server needs upper+lower+digit, 8+ chars), or too little memory (needs about 2 GB) |
| `web` log: cannot open database / login failed | the password changed after the volume was first created: `docker compose down -v` (deletes data) and start again |
| `port is already allocated` | change `WEB_PORT` in `.env` |
| Apple Silicon (M1/M2/...) and `db` is slow or crashes | SQL Server's image is amd64: enable emulation in Docker Desktop, or add `platform: linux/amd64` to `db` |

## Kubernetes later

A Compose **service** becomes a Kubernetes **Deployment + Service**; `environment` → `ConfigMap`/`Secret`; `volumes` → `PersistentVolumeClaim`; `healthcheck` → probes; `depends_on` → readiness probes / init containers; `ports` → `Service`/`Ingress`. Tools like Kompose can convert a Compose file as a first draft.
