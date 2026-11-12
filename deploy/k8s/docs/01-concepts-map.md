# Concepts map: roadmap topic -> where it lives -> why

Read this next to the YAML files. Every file also explains itself in its first lines.

## The architecture in one picture (roadmap item 2)

```
 YOU:  kubectl apply -f ...
          │
          ▼
 ┌─ CONTROL PLANE (k3d "server" node: the brain) ────────────────────────┐
 │  API server          the front desk: every kubectl command goes here  │
 │  datastore           the notebook: remembers what should exist        │
 │  scheduler           chooses a node for each new pod                  │
 │  controller manager  the watchers: "3 wanted, 2 running -> make one"  │
 └───────────────────────────────────────────────────────────────────────┘
          │ orders
          ▼
 ┌─ WORKER NODES (k3d "agent" nodes: the muscles) ───────────────────────┐
 │  kubelet             the node's manager: starts and checks pods       │
 │  kube-proxy          makes Service addresses work                     │
 │  container runtime   (containerd) actually runs the containers        │
 └───────────────────────────────────────────────────────────────────────┘
 ADD-ONS (namespace kube-system, built into k3d):
   CoreDNS = the phone book of names | Traefik = the front door (Ingress)
   Metrics Server = the meters (kubectl top, HPA) | local-path = creates disks
```

- Look at it: `kubectl get nodes -o wide` and `kubectl get pods -n kube-system`.
- k3s runs the control-plane parts inside one program, so you will not see API server or scheduler pods here. In Phase 3, kubeadm shows them as real pods.
- With one server, k3s keeps its notebook in SQLite. Real etcd (and its backup) comes in Phase 3.

## Where each roadmap topic is

| # | Topic | Where | Choice and why |
|---|---|---|---|
| 1 | k3d cluster | `00-cluster/k3d-config.yaml` | k3d over minikube/kind: Traefik, DNS, metrics and storage are built in; extra nodes are one line |
| 2 | Architecture | this file | learn |
| 3 | Namespaces, labels, selectors | `02-hospital-a/00-namespace.yaml`, labels in every file | one namespace per hospital; the standard `app.kubernetes.io/...` labels |
| 4 | Pods, static pods | pods inside `05-web-deployment.yaml`; `01-learn-only/01-static-pod.yaml` | you never create bare pods; static pods are for cluster internals |
| 5 | Deployment, ReplicaSet, ReplicationController | `05-web-deployment.yaml`; `01-learn-only/02`, `03` | **Deployment** wins: it manages a ReplicaSet for you and adds updates and rollbacks. RC is deprecated |
| 6 | StatefulSet | `04-db-statefulset.yaml` | the database needs a stable name and a volume that follows it |
| 7 | DaemonSet | `01-learn-only/04-daemonset.yaml` | one pod per node; not needed for the hospitals |
| 8 | PV, PVC, storage | `volumeClaimTemplates` in `04-db-statefulset.yaml` | automatic volumes from the built-in `local-path` class |
| 9 | ConfigMap, Secrets | `01-configmap.yaml`, `02-secret.yaml.example` | normal settings vs passwords, kept apart; real secrets stay out of Git |
| 10 | Services | `03-db-service.yaml`, `06-web-service.yaml`; `01-learn-only/05`, `06`, `07` | **ClusterIP + Ingress** wins over NodePort and LoadBalancer |
| 11 | CoreDNS | the name `db` in the connection string | exercise in the cheat sheet (`nslookup db`) |
| 12 | Ingress | `07-ingress.yaml` | **Traefik** (built in) wins over ingress-nginx (retired) |
| 13 | Requests and limits | `04-db-statefulset.yaml`, `05-web-deployment.yaml` | requests = what is reserved, limits = the ceiling |
| 14 | Probes | `04-db-statefulset.yaml`, `05-web-deployment.yaml` | startup, readiness (`/health`) and a liveness that does not touch the database |
| 15 | Rolling updates, rollbacks | `strategy` in `05-web-deployment.yaml` | `maxUnavailable: 0`: a new pod must be ready before an old one stops |
| 16 | kubectl logs, exec, describe, port-forward | `02-kubectl-cheatsheet.md` | exercises on your own hospitals |
| 17 | Metrics Server | built in; `kubectl top pods -A` | needed by the HPA |
| 18 | HPA, VPA | `08-hpa.yaml`; `01-learn-only/08-vpa.yaml.example` | **HPA** wins for a web app (more pods); VPA needs an install and suits single pods |
| 19 | Deploy Hospital A | `02-hospital-a/` | one folder, one `kubectl apply -f` |
| 20 | Deploy Hospital B | `03-hospital-b/` | the same files with other names |
| + | securityContext | `04-...`, `05-...` | web: non-root, read-only file system, no extra powers. db: non-root (SQL Server must write to disk) |
| + | Sticky sessions | `06-web-service.yaml` | workaround for per-pod login keys, see the file |

## When two things do the same job: what we picked

| Job | Picked | Not picked, and why |
|---|---|---|
| Keep N pods running | Deployment | ReplicaSet (no rollouts), ReplicationController (deprecated) |
| Run the database | StatefulSet | Deployment (random names, no stable volume) |
| Expose the web app | ClusterIP + Ingress | NodePort (odd ports), LoadBalancer (one paid IP per app) |
| Ingress controller | Traefik | ingress-nginx (retired) |
| More capacity | HPA | VPA (restarts pods, extra install) |
| Settings | ConfigMap + Secret | values pasted into the Deployment (hard to reuse) |
| Local cluster | k3d | minikube, kind (more to install, fewer built-ins) |
| Temporary writable folders | emptyDir | a PVC (these files are throw-away) |

## Words that are easy to mix up

- **Request vs limit:** the request is what the scheduler reserves for the pod. The limit is the ceiling. Above the memory limit the container is killed; above the CPU limit it is slowed down.
- **Readiness vs liveness:** readiness decides "send visitors here?" (failing = no traffic). Liveness decides "restart me?" (failing = restart).
- **Pod vs container:** a pod is the smallest thing Kubernetes runs; it holds one or more containers. Ours hold exactly one.
- **PV vs PVC:** the PVC is a request for disk space; the PV is the disk that satisfies it.
