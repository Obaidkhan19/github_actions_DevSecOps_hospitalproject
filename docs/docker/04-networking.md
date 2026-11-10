# 04 - Docker networking

> **Like you are 10:** Containers are **houses**. A **network** is a **street** that connects houses. Houses on the same street can knock on each other's doors by **name** ("Hi `db`!"). Houses on different streets cannot see each other at all. Some streets are **closed** (no road out to the world).

Open next to this guide: **`docker-compose.yml`** → sections `networks:`, `web:`, `db:`.

## 1. The big picture of our project

```
  your browser / internet
          │  port 8080
  ┌───────▼──────────────────────────────────────────┐
  │ network "frontend"  (bridge)                     │
  │        ┌─────────┐                ┌───────────┐  │
  │        │   web   │                │  dozzle   │  │  (optional)
  │        └────┬────┘                └───────────┘  │
  └─────────────┼────────────────────────────────────┘
                │  web is ALSO on the second network
  ┌─────────────┼────────────────────────────────────┐
  │ network "backend"  (bridge, internal: true)      │
  │        ┌────┴────┐                               │
  │        │   db    │   no published ports,         │
  │        │  :1433  │   no way out to the internet  │
  │        └─────────┘                               │
  └──────────────────────────────────────────────────┘
```

- `web` stands on **two** streets: `frontend` (so you and the internet can reach it) and `backend` (so it can reach the database).
- `db` stands only on `backend`, a **closed street**. Nobody outside can reach it, it cannot reach out.

## 2. The default network and why we do not use it

When you run a container without `--network`, it joins the built-in **default `bridge`** network. It works, but:
- containers can reach each other **only by IP address** (which changes), **not by name**;
- all containers on it can see each other (poor isolation).

**Custom (user-defined) networks** fix both problems: they have **automatic name lookup (DNS)** and you choose who is on which street.

## 3. Custom network = names instead of numbers

On our network the web app connects to the database with this line from `docker-compose.yml`:

```
ConnectionStrings__DefaultConnection: "Server=db,1433;Database=...;"
                                              ▲
                                              └── the SERVICE NAME. Docker's built-in DNS turns "db" into the right IP.
```

**A classic mistake:** inside a container `localhost` means *the container itself*. Writing `Server=localhost` in the web container looks for SQL Server inside the web container and fails. Use the service name: `db`.

## 4. Custom networks for multiple containers

Compose creates the networks for you (named with the project prefix: `hospital_frontend`, `hospital_backend`). The same by hand:

```bash
docker network create hospital-net
docker run -d --name db  --network hospital-net ...
docker run -d --name web --network hospital-net -p 8080:8080 ...
docker network connect    other-net web      # put a running container on a 2nd street
docker network disconnect other-net web
docker network inspect hospital_backend      # who is on this street? is it "Internal": true ?
```

Why two networks in our project? **Least privilege** - each container may talk only to the neighbours it needs. If somebody breaks into `dozzle` (frontend only), they still cannot reach the database.

## 5. Network drivers

A **driver** is the type of street. Most important three:

| Driver | Kid version | What happens | When to use |
|---|---|---|---|
| **bridge** (default) | a private street with its own signs | private virtual switch on your computer; needs `-p` to be reached from outside | almost always (our project) |
| **host** | the house has **no walls** to the street | container uses the computer's network directly; no `-p` needed, no isolation | maximum speed, special tools (Linux only in practice) |
| **none** | a house with **no door at all** | no network except loopback | offline jobs, extra isolation |

Others (just names for now): **overlay** (a street across many computers, Swarm), **macvlan** (the container gets its own address on your real LAN), **ipvlan**.

**Experiments - try each one, then clean up**

```bash
# none: no network
docker run --rm --network none alpine ip addr              # only "lo" (loopback)
docker run --rm --network none alpine ping -c 1 8.8.8.8    # fails

# default bridge: no names
docker run -d --name a1 alpine sleep 3600
docker run -d --name a2 alpine sleep 3600
docker exec a1 ping -c 1 a2                                # "bad address" - names do not work
docker exec a1 ping -c 1 $(docker inspect -f '{{.NetworkSettings.IPAddress}}' a2)   # IP works

# custom bridge: names work
docker network create school
docker run -d --name b1 --network school alpine sleep 3600
docker run -d --name b2 --network school alpine sleep 3600
docker exec b1 ping -c 1 b2                                # works!

# host: no -p needed (Linux). Open http://localhost
docker run -d --name h1 --network host nginx:alpine

# clean up
docker rm -f a1 a2 b1 b2 h1 ; docker network rm school
```

> On **Docker Desktop** (Windows/macOS) `host` mode belongs to the hidden Linux VM, not your PC, so `localhost` may not behave as on Linux.

**Internal networks.** `internal: true` (our `backend`) is a `bridge` with the road to the outside removed. Containers on it can talk to each other but not to the internet.

## 6. Port mapping versus networks

| Situation | What you need |
|---|---|
| Your **browser** → web container | **port mapping** `-p 8080:8080` (`ports:`) |
| **web container** → db container | a **shared network** + the service name. **No ports** needed |
| Anyone → db | nothing: deliberately closed |
| Only my computer → admin tool | `127.0.0.1:9999:8080` |

Port mapping is only for traffic coming **from outside Docker**. Containers on the same network reach each other's **container ports directly** (`db:1433`), whether published or not.

## 7. Debug checklist

```bash
docker compose ps                                  # are both healthy?
docker network inspect hospital_backend            # are web and db listed?
docker compose exec web sh -c "wget -qO- http://127.0.0.1:8080/health"   # does the app answer?
docker compose logs db | tail -20                  # SQL Server errors?
```
`port is already allocated` → another program uses that computer port: change `WEB_PORT` in `.env`.

## Kubernetes later

Every pod gets its own IP and **Services** give stable names (like our `db`). Compose networks become **namespaces** + **NetworkPolicies**. The idea "reach things by name, publish only what must be public" is identical (Ingress = the only public door).
