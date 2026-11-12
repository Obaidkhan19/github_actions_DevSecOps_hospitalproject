# Kubernetes, Phase 1: two hospitals on a k3d cluster

## 1. Install the tools (once)

You already have Docker. You also need `kubectl` (the remote control) and `k3d` (the cluster maker):

```bash
# kubectl  (on an ARM computer replace amd64 with arm64)
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl

# k3d

k3d = Kubernetes in Docker.

It is a tool that creates and manages local Kubernetes clusters using Docker containers. 

curl -s https://raw.githubusercontent.com/k3d-io/k3d/main/install.sh | bash

kubectl version --client && k3d version      # both should print a version
```

## 2. Run it

```bash
docker compose down                                                   # frees RAM (data stays in its volume)

k3d cluster start hospital
k3d cluster stop hospital
k3d cluster create --config /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/00-cluster/k3d-config.yaml# 1. the cluster
kubectl get nodes                                                     #    expect 3 nodes, all Ready

copies the example Secret YAML file and creates a new file where you will store your actual passwords.
cp /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/02-hospital-a/02-secret.yaml.example /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/02-hospital-a/02-secret.yaml # 2. your passwords
cp /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/03-hospital-b/02-secret.yaml.example /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/03-hospital-b/02-secret.yaml
nano /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/02-hospital-a/02-secret.yaml     # change both passwords (rules are inside the file)
nano /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/03-hospital-b/02-secret.yaml

kubectl apply -f /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/02-hospital-a/
kubectl apply -f /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/03-hospital-b/

kubectl get pods -A -w                                                # 4. watch (Ctrl+C to leave)

EMAIL   nano /home/obaid-khan/Downloads/kubernetes-phase1/deploy/k8s/02-hospital-a/01-configmap.yaml
PASSWORD about secret.yml

http://hospital-b.localhost:8088/
```

## 6. Clean up

```bash
kubectl delete namespace hospital-b          # remove one hospital and ALL its data
k3d cluster delete hospital                  # remove the whole cluster
```
