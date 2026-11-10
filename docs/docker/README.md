# Docker for the Hospital project - from 0 to 100%

Every guide explains one topic **like you are 10 years old**, then shows the real commands, then points to the **exact file in this repo where the idea is used**. Read them in order.

## Where every file goes

Only the files marked **NEW** were added. Your application code (`src/`, `tests/`) is **not touched**.

```
github_actions_devsecops_hospitalproject/
├── Dockerfile                          NEW  the recipe (multi-stage, secure, optimized)
├── .dockerignore                       NEW  what Docker must NOT copy (secrets, bin/, obj/)
├── docker-compose.yml                  NEW  the whole system: web + db (+ optional log viewer)
├── docker-compose.dev.yml              NEW  extra file for developing on your laptop
├── .env.example                        NEW  copy to ".env" and put your settings there
├── .github/workflows/
│   └── docker_publish_devsecops.yml    NEW  build + scan + push to Docker Hub
├── docs/docker/                        NEW  these guides
│   ├── README.md                       you are here
│   ├── 01-basics-and-objects.md
│   ├── 02-build-multistage-optimize.md
│   ├── 03-security.md
│   ├── 04-networking.md
│   ├── 05-storage-mounting.md
│   ├── 06-compose.md
│   ├── 07-registry-hub-deploy.md
│   └── 08-monitoring-logging.md
├── src/HospitalManagement.Web/         (unchanged)
└── tests/HospitalManagement.Tests/     (unchanged)
```

Later, Kubernetes and Terraform get their own folders next to these, so nothing has to move:

```
deploy/k8s/          <- Kubernetes manifests (later)
deploy/terraform/    <- Terraform code (later)
```

## Start in 5 commands

```bash
cp .env.example .env                  # 1. make your settings file, then edit the passwords inside
docker compose up -d --build          # 2. build the image and start web + db
docker compose ps                     # 3. both should say "healthy" after about a minute
# 4. open http://localhost:8080 and log in with SEED_ADMIN_EMAIL / SEED_ADMIN_PASSWORD from .env
docker compose down                   # 5. stop (your data stays in the volume)
```

## Where is every concept?

| # | Concept | Explained in | Used in (file) |
|---|---------|--------------|----------------|
| 1 | Images, containers, Docker objects | [01](01-basics-and-objects.md) | all |
| 2 | **Interacting with Docker objects** (run, ps, logs, exec, inspect...) | [01](01-basics-and-objects.md) §5 | - |
| 3 | **Layers in a Docker image** | [01](01-basics-and-objects.md) §4, [02](02-build-multistage-optimize.md) §2 | `Dockerfile` |
| 4 | **Port mapping** | [01](01-basics-and-objects.md) §6, [04](04-networking.md) §6 | `docker-compose.yml` (`ports:`), `Dockerfile` (`EXPOSE`) |
| 5 | **Environment variables** | [01](01-basics-and-objects.md) §7, [06](06-compose.md) §4 | `docker-compose.yml` (`environment:`), `.env.example` |
| 6 | **Deploying containers** | [01](01-basics-and-objects.md) §8, [07](07-registry-hub-deploy.md) §7 | `docker-compose.yml` (`restart:`, `depends_on:`) |
| 7 | **Multi-stage build** | [02](02-build-multistage-optimize.md) §3 | `Dockerfile` (3 stages) |
| 8 | **Docker optimization** | [02](02-build-multistage-optimize.md) §4 | `Dockerfile`, `.dockerignore` |
| 9 | **Docker security** | [03](03-security.md) | `Dockerfile`, `docker-compose.yml`, `.dockerignore`, workflow |
| 10 | **Docker networking**: default and custom networks | [04](04-networking.md) §2-3 | `docker-compose.yml` (`networks:`) |
| 11 | **Custom networks for many containers** | [04](04-networking.md) §4 | `docker-compose.yml` (frontend + backend) |
| 12 | **Network drivers**: bridge, host, none | [04](04-networking.md) §5 | experiments in the guide |
| 13 | **Mounting**: volumes, bind mounts, tmpfs | [05](05-storage-mounting.md) | `docker-compose.yml` (`volumes:`, `tmpfs:`), `docker-compose.dev.yml` |
| 14 | **Docker Compose**: services, ports, volumes, env | [06](06-compose.md) | `docker-compose.yml`, `docker-compose.dev.yml` |
| 15 | **Docker registry** and **Docker Hub** | [07](07-registry-hub-deploy.md) | `.github/workflows/docker_publish_devsecops.yml` |
| 16 | **Monitoring and logging** | [08](08-monitoring-logging.md) | `docker-compose.yml` (`logging:`, `healthcheck:`, `dozzle`) |

## Built to move on to Kubernetes and Terraform

Every Docker choice below has a direct Kubernetes cousin, so the work you do now carries over.

| Docker choice made here | Why it helps later | Kubernetes / Terraform version |
|---|---|---|
| App listens on port **8080**, non-root | Non-root users cannot use ports under 1024 | `containerPort: 8080`, Service `targetPort: 8080` |
| User is a **number** (`USER 1654`) | Kubernetes can verify it | `securityContext.runAsNonRoot: true` |
| All settings come from **environment variables** | The image never changes between environments | `ConfigMap` (normal) and `Secret` (passwords) |
| `read_only: true` + `tmpfs: /tmp` | Same hardening rule | `readOnlyRootFilesystem: true` + `emptyDir` on `/tmp` |
| `cap_drop: ALL`, `no-new-privileges` | Same hardening rule | `capabilities.drop: [ALL]`, `allowPrivilegeEscalation: false` |
| Named volumes (`sqldata`, `dpkeys`) | Data must outlive containers | `PersistentVolumeClaim` |
| `frontend` / `backend` networks | Keep the database private | `Service` (ClusterIP) + `NetworkPolicy` |
| `healthcheck` + `/health` | Know when the app is ready | `readinessProbe` / `livenessProbe` |
| Logs to **stdout** as JSON | Log tools collect them | Same; Loki / Elastic read container stdout |
| `deploy.resources.limits` | Fair sharing of a computer | `resources.requests` and `resources.limits` |
| Versioned tags (`1.0.0`, `sha-...`) in a registry | Reproducible deployments | `image: user/hospital-web:1.0.0`, `imagePullSecrets` |
| Registry on Docker Hub / ghcr.io | Servers pull from it | Terraform can create a cloud registry (ACR / ECR) and the cluster |

### Four things to remember for Kubernetes (they are about the app, not Docker)

1. **Login cookies and several copies of the app.** ASP.NET locks cookies with keys saved in a folder (we give it a volume). With 2+ replicas, all copies need the **same** keys - shared storage or a key store. That needs a small code change later.
2. **One health endpoint.** `/health` also checks the database. Kubernetes works best with two: a *liveness* one that never touches the database, and a *readiness* one that does.
3. **Migrations on start.** `Database__MigrateOnStartup=true` is perfect for one container. With many replicas, run migrations once in a Kubernetes `Job` or init container instead.
4. **HTTPS.** Behind a Kubernetes Ingress with TLS, set `SECURE_COOKIES=true` and check forwarded-header handling.
