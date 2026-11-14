

docker --version
kubectl version --client
k3d version
helm version


## Run it (from the repository root)


```bash
docker --version
kubectl version --client
k3d version
helm version




k3d cluster create --config deploy/k8s/00-cluster/k3d-config.yaml


cd kubernetes-phase2
kubectl get nodes

kubectl apply -f deploy/helm/00-prereqs/hospital-a/
kubectl apply -f deploy/helm/00-prereqs/hospital-b/


helm install hospital-a deploy/helm/hospital \
  -n hospital-a \
  -f deploy/helm/values-hospital-a.yaml


helm install hospital-b deploy/helm/hospital \
  -n hospital-b \
  -f deploy/helm/values-hospital-b.yaml

kubectl get pods -A -w = -a is all

k3d cluster delete hospital
k3d cluster list




cp kubernetes-phase2/deploy/helm/00-prereqs/hospital-a/01-secret.yaml.example \
   kubernetes-phase2/deploy/helm/00-prereqs/hospital-a/01-secret.yaml
cp kubernetes-phase2/deploy/helm/00-prereqs/hospital-b/01-secret.yaml.example \
   kubernetes-phase2/deploy/helm/00-prereqs/hospital-b/01-secret.yaml
nano deploy/helm/00-prereqs/hospital-a/01-secret.yaml   
nano deploy/helm/00-prereqs/hospital-b/01-secret.yaml
kubectl apply -f deploy/helm/00-prereqs/hospital-a/
kubectl apply -f deploy/helm/00-prereqs/hospital-b/

helm install hospital-a deploy/helm/hospital -n hospital-a -f deploy/helm/values-hospital-a.yaml
helm install hospital-b deploy/helm/hospital -n hospital-b -f deploy/helm/values-hospital-b.yaml

kubectl get pods -A -w                  
delete db post for vc test kubectl get pods -n hospital-a -w
```
