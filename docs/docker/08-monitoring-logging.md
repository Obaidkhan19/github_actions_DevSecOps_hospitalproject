# 08 - Monitoring and logging

> **Like you are 10:** **Logging** is the app's **diary**: "10:01 I started. 10:02 A user logged in. 10:03 Oops, a problem!". **Monitoring** is the **doctor's check-up**: "heart rate OK, temperature OK, eating too much memory!". A container is like a patient you cannot see inside, so you need both.

Open next to this guide: **`docker-compose.yml`** → `x-logging`, `healthcheck`, `dozzle`. And `Program.cs` (line: `builder.Logging.AddJsonConsole()`).

## 1. Logging

**The golden rule of containers: write logs to the console (stdout/stderr), not to files.** Docker catches the console output for you. Your app already does this - `Program.cs` removes all log providers and adds the **JSON console** one, so every log line is one JSON object that tools can read.

```bash
docker compose logs -f web                  # follow live (docker logs -f hospital-web-1)
docker compose logs --tail 100 web          # last 100 lines
docker compose logs --since 10m web         # last 10 minutes
docker compose logs -t web                  # with timestamps
docker compose logs --no-log-prefix web | jq .    # pretty-print JSON lines (needs "jq")
docker compose logs web | grep -i error
```

**How much to log** - without changing code, with an environment variable. In `docker-compose.yml` under `web: environment:` add:

```yaml
Logging__LogLevel__Default: Debug          # Trace, Debug, Information, Warning, Error
```
`Information` is normal; `Debug` only while hunting a bug (and **never log passwords or patient data** - this is a hospital system).

### Logs fill disks - rotation

By default Docker keeps every log line **forever** in a file on the host. A busy container can fill the disk. In our file:

```yaml
x-logging: &default-logging
  driver: json-file
  options:
    max-size: "10m"     # one file max 10 MB
    max-file: "3"       # keep 3 files, delete the oldest   => max 30 MB per container
```
Each service uses it with `logging: *default-logging`. Check: `docker inspect hospital-web-1 --format '{{.HostConfig.LogConfig}}'`.

### Logging drivers

A **driver** decides *where* logs go:

| Driver | Where |
|---|---|
| `json-file` (default) | JSON files on the host (our choice; `docker logs` works) |
| `local` | compact files, rotated by default |
| `journald`, `syslog` | the Linux system log |
| `fluentd`, `gelf`, `splunk`, `awslogs`, `gcplogs` | log servers / cloud services |
| `none` | throw logs away |

Note: with some remote drivers `docker logs` stops working, so keep that in mind.

## 2. Monitoring

**a) Health: "is it alive and ready?"** We built two layers:

- `Dockerfile` → `HEALTHCHECK` for `web`; `docker-compose.yml` → `healthcheck` for `db`.
- The app's `/health` endpoint also checks the database connection.

```bash
docker compose ps                                              # STATUS shows (healthy) / (unhealthy)
docker inspect hospital-web-1 --format '{{.State.Health.Status}}'
docker inspect hospital-web-1 --format '{{json .State.Health}}'   # last check results
curl -i http://localhost:8080/health                            # 200 Healthy / 503 Unhealthy
```
Docker **marks** a container unhealthy but does not restart it by itself (Kubernetes does). Compose uses health for `depends_on`.

**b) Resources: "how much does it eat?"**

```bash
docker stats                                # live CPU %, memory, network, disk for every container
docker stats --no-stream                    # one snapshot
docker top hospital-web-1                   # processes inside
docker system df                            # disk used by Docker
```
Compare **memory use** with the 768M limit we set. If it is near the limit, the container gets killed (`OOMKilled`): `docker inspect NAME --format '{{.State.OOMKilled}}'`.

**c) Events: "what just happened?"**

```bash
docker events                               # live stream: start, die, health_status, oom ...
docker events --since 1h --filter container=hospital-web-1
```

**d) A web page for logs (optional): Dozzle**

```bash
docker compose --profile monitoring up -d
# open http://localhost:9999   (only from your own computer)
```
You get live logs of every container in the browser. It reads the Docker socket, so use it **locally only** ([03](03-security.md)). Stop it with `docker compose stop dozzle`.

## 3. What a real monitoring setup looks like (next steps)

```
 containers ──metrics──► Prometheus ──► Grafana (dashboards + alerts)
 containers ──logs────► Loki / Elastic ──► Grafana / Kibana (search)
```
- **cAdvisor** exports container CPU/memory to Prometheus.
- **OpenTelemetry** in the app can export request counts, durations, traces (a later code change).
- **Alerts**: "notify me when `/health` is unhealthy for 2 minutes".

The decisions we took now make that easy: logs are JSON on stdout, there is a health endpoint, and limits are explicit.

## Kubernetes later

`docker logs` → `kubectl logs`; `docker stats` → `kubectl top` (metrics-server); `docker events` → `kubectl get events`; `HEALTHCHECK` → **liveness / readiness / startup probes**. Cluster-wide, a log agent (Promtail, Fluent Bit) ships container stdout to Loki or Elastic, and the Prometheus Operator scrapes metrics.
