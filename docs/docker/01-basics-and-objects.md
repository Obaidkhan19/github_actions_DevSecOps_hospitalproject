# 01 - Basics: images, containers, objects, layers, ports, variables, running

> **Like you are 10:** Docker is a **lunch box for programs**. We pack the program *and everything it needs* (food, fork, napkin) into one box. Open the box on your laptop, on a friend's PC or on a giant server - the lunch is exactly the same.

## 1. The problem Docker solves

"It works on my computer!" Your Hospital app needs .NET 10, SQL Server, certain settings... A friend has .NET 8 and no SQL Server, so it breaks. With Docker everything is **inside the box**, so it runs the same everywhere.

**Container vs. virtual machine (VM)**
- A **VM** is a whole *house* with its own pipes, wires and roof. Heavy, slow to start.
- A **container** is an *apartment* in one big building: it shares the building's pipes (the Linux kernel) but has its own locked door and rooms. Light, starts in a second.

## 2. The words

| Word | Kid version | Real meaning | In this project |
|------|-------------|--------------|-----------------|
| **Dockerfile** | the recipe | text file with build steps | `Dockerfile` |
| **Image** | the finished cake: frozen, you cannot change it | read-only package: app + runtime + libraries | `hospital-web:dev` |
| **Container** | one slice you are eating: alive | a *running* image | `hospital-web-1`, `hospital-db-1` |
| **Registry** | the bakery shelf where cakes are stored | server that stores images | Docker Hub |
| **Docker Engine** | the kitchen robot that does the work | background program (`dockerd`) | - |
| **docker CLI** | the remote control | the `docker ...` commands you type | - |
| **Volume** | a safe box that survives | storage outside the container | `sqldata` |
| **Network** | a street that joins houses | virtual network between containers | `frontend`, `backend` |

One cake (image) can make many slices (containers). You can start 5 hospitals from the same image.

## 3. The four Docker objects

Docker manages four kinds of "things". Everything you type works on one of them:

| Object | List them | Look inside | Delete |
|---|---|---|---|
| **Image** | `docker image ls` | `docker image inspect IMG` | `docker rmi IMG` |
| **Container** | `docker ps -a` | `docker inspect NAME` | `docker rm NAME` |
| **Volume** | `docker volume ls` | `docker volume inspect NAME` | `docker volume rm NAME` |
| **Network** | `docker network ls` | `docker network inspect NAME` | `docker network rm NAME` |

## 4. Layers - why images are like transparent sheets

Imagine **sheets of see-through plastic** stacked on an overhead projector. Each Dockerfile line (`COPY`, `RUN`...) adds one more sheet. Looking from above you see the combined picture - that is the image.

```
 sheet 4:  ENTRYPOINT ...           (tiny)
 sheet 3:  COPY the published app   (changes often)
 sheet 2:  mkdir keys folder
 sheet 1:  the Alpine + .NET base   (shared by many images)
```

Facts that matter:
- Sheets are **read-only**. Many images and containers can **share** the same sheet, so Docker stores it only once (saves disk and download time).
- Docker **remembers** each sheet. If a line and everything above it is unchanged, Docker reuses the sheet: you see `CACHED` in the build output. Change one line and **every sheet after it** is rebuilt. That is why *the order of lines matters* (see [02](02-build-multistage-optimize.md)).
- A **running container** gets one extra thin **writable sheet** on top. Everything the app writes goes there. When the container is deleted, that sheet is thrown away - which is why we need **volumes** ([05](05-storage-mounting.md)).

See your sheets: `docker history hospital-web:dev`

## 5. Interacting with Docker objects (the commands you will use every day)

**Containers**

```bash
docker run hello-world                 # create + start a container from an image
docker ps                              # running containers (add -a for stopped too)
docker logs -f --tail 50 NAME          # watch the output live (see 08)
docker exec -it NAME sh                # open a shell INSIDE a running container
docker stop NAME   /  docker start NAME  /  docker restart NAME
docker rm NAME                         # delete a stopped container (rm -f = force)
docker inspect NAME                    # everything about it, as JSON
docker top NAME                        # processes inside
docker stats                           # live CPU / memory (see 08)
docker cp NAME:/app/appsettings.json . # copy a file out (or in)
```

**Images**

```bash
docker pull mcr.microsoft.com/dotnet/aspnet:10.0-alpine   # download
docker build -t hospital-web:dev .                        # build from the Dockerfile
docker image ls                                           # list (look at SIZE)
docker history hospital-web:dev                           # the sheets (layers)
docker rmi hospital-web:dev                               # delete
```

**Cleaning up**

```bash
docker system df                       # how much disk Docker uses
docker system prune                    # remove stopped containers, unused networks, dangling images
docker system prune --volumes          # DANGER: also removes unused volumes = your data
```

**Try it:** `docker run --rm -it alpine sh`, then type `ls`, `whoami`, `exit`. `--rm` deletes the container when you leave.

## 6. Port mapping

A container is an apartment with its **own** phone numbers (ports). Our web app answers on **8080 inside**. But your browser is *outside* the building and cannot see inside. **Port mapping** connects a button on the front-door intercom to an apartment:

```
        OUTSIDE (your computer)            INSIDE (container)
 browser ---> localhost:8080  ------------>  port 8080  (the web app)

        docker run -p 8080:8080 ...
                     |     |
                     |     +-- container port (right side = inside)
                     +-------- computer port   (left side  = outside)
```

```bash
docker run -d -p 8080:8080 hospital-web:dev          # computer 8080 -> container 8080
docker run -d -p 9000:8080 hospital-web:dev          # computer 9000 -> container 8080
docker run -d -p 127.0.0.1:8080:8080 hospital-web:dev  # only THIS computer may connect
docker port NAME                                      # show the mappings
```

- Two containers may **both use 8080 inside**, but each needs a **different outside number** (`-p 8080:8080` and `-p 8081:8080`). Starting a second one on the same outside port gives `port is already allocated`.
- `EXPOSE 8080` in the Dockerfile is only a **sticky note** for humans. It opens nothing. `-p` (or `ports:`) does the real work.
- **The database has no `-p` at all** in our project. The web container reaches it through a private network ([04](04-networking.md)). Fewer open doors = safer.

**Used in:** `docker-compose.yml` → `web: ports: "${WEB_PORT:-8080}:8080"`; `Dockerfile` → `EXPOSE 8080` and `ASPNETCORE_HTTP_PORTS=8080`.

## 7. Environment variables

Env variables are **sticky notes handed to the app when it wakes up**: "your database is at db", "the hospital is called Hospital A". The same image can be a different hospital depending on its notes. The image itself never contains passwords.

```bash
docker run -e Hospital__Name="Hospital A" -e Seed__SampleData=true hospital-web:dev
docker run --env-file .env hospital-web:dev      # read many notes from a file
docker exec NAME env                             # see the notes inside a container
```

- **.NET trick:** two underscores `__` mean a colon. `Hospital__Name` sets `Hospital:Name` from `appsettings.json`. `ConnectionStrings__DefaultConnection` sets the database address. Your `Program.cs` already reads all settings this way - that is why the app is "container ready".
- **`ENV` vs `ARG` in a Dockerfile:** `ENV` = a note that is also there while the container runs. `ARG` = only while building (like `DOTNET_VERSION`).
- **Never** put secrets in a `Dockerfile` or in an image: anyone with the image can read them (`docker history`). Hand them over at run time ([03](03-security.md)).

**Used in:** `docker-compose.yml` → `environment:`; all values come from `.env` (template: `.env.example`).

## 8. Deploying containers (the manual way, so you appreciate Compose)

"Deploying" = putting your app somewhere it can run for real. Here are the same two containers as `docker-compose.yml`, typed by hand:

```bash
docker network create hospital-net                    # a street
docker volume create sqldata                          # a safe box

docker run -d --name db --network hospital-net \
  -v sqldata:/var/opt/mssql \
  -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='Change_Me_Db_Pass_123!' \
  mcr.microsoft.com/mssql/server:2022-latest

docker build -t hospital-web:dev .

docker run -d --name web --network hospital-net -p 8080:8080 \
  -e 'ConnectionStrings__DefaultConnection=Server=db,1433;Database=HospitalDB;User Id=sa;Password=Change_Me_Db_Pass_123!;TrustServerCertificate=True;' \
  -e Database__MigrateOnStartup=true \
  -e Seed__AdminEmail=admin@example.com -e Seed__AdminPassword='Change_Me_Admin_123!' \
  hospital-web:dev
```

That is long, easy to mistype, and hard to repeat. Compose ([06](06-compose.md)) writes it all once in a file.

**Useful `docker run` flags**

| Flag | Meaning |
|---|---|
| `-d` | detached: run in the background |
| `--name X` | give it a friendly name (otherwise Docker invents one) |
| `--rm` | delete the container when it stops |
| `-p A:B` | port mapping |
| `-e K=V`, `--env-file F` | environment variables |
| `-v` / `--mount` | storage ([05](05-storage-mounting.md)) |
| `--network N` | which street ([04](04-networking.md)) |
| `--restart unless-stopped` | start again after a crash or a reboot |
| `--memory 768m --cpus 1` | resource limits |

**Restart policies:** `no` (default), `on-failure` (only after errors), `always`, `unless-stopped` (always, except if *you* stopped it) ← our choice.

Real-server deployment (pull from Docker Hub, update to a new version) is in [07](07-registry-hub-deploy.md).

## Kubernetes later

`docker run` = a Kubernetes **Pod**. `-p` = a **Service**. `-e` = **ConfigMap/Secret**. `-v` = **PersistentVolumeClaim**. `--restart` = Kubernetes always restarts for you.
