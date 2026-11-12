# kubectl cheat sheet with exercises on your hospitals (roadmap items 11, 15, 16, 17)

`-n hospital-a` means "in the room hospital-a". Use `-A` for all namespaces.

## The four commands to learn first

| Command | What it does |
|---|---|
| `kubectl get pods -n hospital-a` | list things (`-o wide` adds the node and IP, `-w` keeps watching) |
| `kubectl describe pod <name> -n hospital-a` | full story of one object; read **Events** at the bottom when something is wrong |
| `kubectl logs <pod> -n hospital-a` | the app's output (`-f` follows, `--tail 50`, `--previous` = the crashed run) |
| `kubectl exec -it <pod> -n hospital-a -- sh` | a shell inside a pod |

Other useful ones: `kubectl get all -n hospital-a`, `kubectl get events -n hospital-a --sort-by=.lastTimestamp`,
`kubectl top pods -A`, `kubectl get pvc -n hospital-a`, `kubectl explain deployment.spec.strategy`.
Shortcuts: `po` pods, `svc` services, `deploy` deployments, `sts` statefulsets, `pvc`, `hpa`, `ns`.

## Exercises (do them in this order)

**1. Logs.** `kubectl logs deployment/web -n hospital-a` (picks one pod). Follow all web pods: `kubectl logs -f -l app.kubernetes.io/name=hospital-web -n hospital-a`. Your app writes JSON lines to the console, exactly like in Docker.

**2. Describe.** `kubectl describe pod db-0 -n hospital-a`. Find: the node it runs on, the volume, the probes, the Events.

**3. Exec: ask the database a question.**
```bash
kubectl exec -it db-0 -n hospital-a -- sh -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT name FROM sys.databases"'
```

**4. Port-forward: open a door just for you.** Reach a Service without the Ingress:
```bash
kubectl port-forward svc/web 9000:80 -n hospital-a     # then open http://localhost:9000
```
Stop with Ctrl+C. Great for debugging, never for real traffic.

**5. CoreDNS: names to addresses.**
```bash
kubectl run -it --rm dns --image=busybox -n hospital-a -- nslookup db
kubectl run -it --rm dns --image=busybox -n hospital-a -- nslookup web.hospital-b.svc.cluster.local
```
The second one is the full name of a Service in the OTHER namespace: `service.namespace.svc.cluster.local`.

**6. Self-healing.** Delete a web pod: `kubectl delete pod <web-pod> -n hospital-a` and watch `kubectl get pods -n hospital-a -w`. A new one appears at once.

**7. Data survives (PVC).** Log in to hospital A and look at the patients. Then:
```bash
kubectl delete pod db-0 -n hospital-a
kubectl get pods -n hospital-a -w          # db-0 comes back, with the same name
```
Reload the page after db-0 is ready: the data is still there, because the new db-0 got the same volume (`kubectl get pvc -n hospital-a`).

**8. Metrics.** `kubectl top nodes` and `kubectl top pods -A` (the Metrics Server needs about a minute after the cluster starts).

**9. HPA.** Run the load test from `02-hospital-a/08-hpa.yaml` and watch `kubectl get hpa -n hospital-a -w`. The number of web pods grows from 2 towards 4.

**10. Rolling update and rollback.**
```bash
# 1. in 02-hospital-a/05-web-deployment.yaml change  image: mukhann/hospital-web:v2  to  :v1
kubectl apply -f deploy/k8s/02-hospital-a/05-web-deployment.yaml
kubectl rollout status deployment/web -n hospital-a     # one new pod first, then one old pod leaves
kubectl rollout history deployment/web -n hospital-a
kubectl rollout undo deployment/web -n hospital-a       # back to the previous version
kubectl get rs -n hospital-a                            # the old ReplicaSet is still there: that is how rollback works
```
(Your Docker Hub has both `v1` and `v2`, so no new build is needed. Set the file back to `v2` afterwards.)

**11. Scale by hand and see the HPA win.** `kubectl scale deployment/web --replicas=1 -n hospital-a`. Within about 15 seconds the HPA puts it back to 2, its minimum.

## Reading a pod status

| Status | Meaning |
|---|---|
| `Pending` | no node could take it yet (RAM, or the volume is not ready) |
| `ContainerCreating` | image is being pulled |
| `ImagePullBackOff` | the image name or access is wrong |
| `CreateContainerConfigError` | a Secret or ConfigMap it needs does not exist |
| `Running` but `0/1` | the readiness probe is failing: check `describe` and `logs` |
| `CrashLoopBackOff` | it starts, crashes, waits, retries: read `kubectl logs <pod> --previous` |
