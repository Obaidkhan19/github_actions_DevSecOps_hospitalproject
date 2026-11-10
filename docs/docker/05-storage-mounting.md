# 05 - Storage and mounting: volumes, bind mounts, tmpfs

> **Like you are 10:** A container is a **sandcastle**. When the tide comes (the container is deleted), **everything written inside is washed away**. If you want something to stay, you keep it in a **treasure chest on the beach** (a volume) and give the sandcastle a window to it. **Mounting** means "connect this chest to this place inside the container".

Open next to this guide: **`docker-compose.yml`** → `volumes:` and `tmpfs:`, and **`docker-compose.dev.yml`**.

## 1. The problem

A container's files live in its thin writable layer ([01 §4](01-basics-and-objects.md)). Delete the container → files gone. For the **hospital database** that would mean losing every patient. So the database files must live **outside** the container.

## 2. Three kinds of mounts

| Type | Kid version | Where the data really lives | Survives `docker rm`? | Used for | In this project |
|---|---|---|---|---|---|
| **Volume** (named) | a chest managed by Docker | Docker's own area | yes | databases, anything important | `sqldata`, `dpkeys` |
| **Bind mount** | a window to **your** folder | a folder you choose on your computer | yes (it is your folder) | development, sharing config or code live | `docker-compose.dev.yml` → `wwwroot` |
| **tmpfs** | a whiteboard in RAM | memory only | no, gone at stop | secrets or scratch data that must never touch the disk | `/tmp` of `web` |

```
 CONTAINER                       HOST (your computer)
 /var/opt/mssql  ◄──volume────►  Docker area "hospital_sqldata"
 /app/wwwroot    ◄──bind mount─► ./src/HospitalManagement.Web/wwwroot   (dev only)
 /tmp            ◄──tmpfs─────►  RAM
```

## 3. How we use them

**Volume for the database** (`docker-compose.yml`)

```yaml
db:
  volumes:
    - sqldata:/var/opt/mssql        # NAME : PATH-INSIDE
volumes:
  sqldata:                          # declared once at the bottom
```
The database files survive `docker compose down`, image updates and crashes.

**Volume for cookie keys** (`web`) - `dpkeys:/home/app/.aspnet/DataProtection-Keys`. ASP.NET keeps the secret keys that lock login cookies in that folder. Without the volume, a new container would create new keys and **everyone would be logged out**. The `Dockerfile` created this folder and gave it to the non-root user; Docker copies that ownership into a new empty volume - so the non-root app can write there.

**Bind mount for development** (`docker-compose.dev.yml`)

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d --build
```
Now `./src/HospitalManagement.Web/wwwroot` on your computer **is** `/app/wwwroot` in the container. Edit `css/site.css`, refresh the browser (use Ctrl+F5 to bypass the browser cache): the change appears with **no rebuild**. `:ro` = read-only for the container.

**tmpfs for `/tmp`** - because the web container's disk is read-only ([03](03-security.md)), programs still need one scratch place. `tmpfs: - /tmp` gives a RAM folder that disappears when the container stops.

## 4. `-v` or `--mount`?

Two spellings for the same thing:

```bash
docker run -v sqldata:/var/opt/mssql ...                        # short; "-v name:path"
docker run --mount type=volume,source=sqldata,target=/var/opt/mssql ...   # long, explicit, clearer
docker run --mount type=bind,source="$PWD"/data,target=/data,readonly ...
docker run --tmpfs /scratch ...
```
Rule of thumb: `-v NAME:/path` = volume; `-v /host/path:/path` = bind mount. `--mount` **errors** if a bind folder does not exist (safer); `-v` silently creates it.

## 5. Experiments

```bash
# 1) a volume outlives its container
docker volume create mydata
docker run --name c1 -v mydata:/data alpine sh -c "echo 'I survived' > /data/note.txt"
docker rm c1
docker run --rm -v mydata:/data alpine cat /data/note.txt      # prints: I survived

# 2) a bind mount shows your real folder
docker run --rm -v "$PWD":/work:ro alpine ls /work

# 3) tmpfs is memory
docker run --rm --tmpfs /scratch alpine df -h /scratch

# 4) look at our volumes (names have the project prefix)
docker volume ls
docker volume inspect hospital_sqldata

docker volume rm mydata
```

## 6. Backup and restore a volume

A volume is just files. Tar them with a throw-away container:

```bash
# backup  ->  creates sqldata.tgz in the current folder
docker run --rm -v hospital_sqldata:/data -v "$PWD":/backup alpine \
  tar czf /backup/sqldata.tgz -C /data .

# restore into an (empty) volume
docker run --rm -v hospital_sqldata:/data -v "$PWD":/backup alpine \
  tar xzf /backup/sqldata.tgz -C /data
```
Stop the database first (`docker compose stop db`) for a clean copy. For serious backups use SQL Server's own `BACKUP DATABASE`.

## 7. Dangers

| Command | Effect |
|---|---|
| `docker compose down` | removes containers and networks, **keeps volumes** |
| `docker compose down -v` | **deletes the volumes: all database data is lost** |
| `docker system prune --volumes` | deletes every volume no container uses |
| Bind mount + non-root | the container user must be allowed to read/write that host folder (permission errors are common on Linux) |
| Bind mounts in production | avoid: they tie the container to one computer's folders |

## Kubernetes later

Volume → **PersistentVolumeClaim** (a request for storage). `tmpfs` → `emptyDir` with `medium: Memory`. Bind mount → `hostPath` (rare; avoid). The database will need a **StatefulSet**, or - often better in the cloud - a managed database created with **Terraform**. The cookie-keys volume must become shared storage when you run several copies of the app (see the [README](README.md)).
