#!/usr/bin/env bash
set -euo pipefail

# This script expects to be stored beside hospital-kubernetes-monitoring.json.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DASHBOARD_FILE="${SCRIPT_DIR}/hospital-kubernetes-monitoring.json"
NAMESPACE="monitoring"
CONFIGMAP="hospital-kubernetes-dashboard"

if [[ ! -f "${DASHBOARD_FILE}" ]]; then
  echo "Dashboard JSON not found: ${DASHBOARD_FILE}" >&2
  exit 1
fi

if ! kubectl get namespace "${NAMESPACE}" >/dev/null 2>&1; then
  echo "Namespace '${NAMESPACE}' does not exist."
  echo "Install your kube-prometheus-stack release first, then run this script again."
  exit 1
fi

echo "Creating/updating Grafana dashboard ConfigMap..."
kubectl create configmap "${CONFIGMAP}" \
  --namespace "${NAMESPACE}" \
  --from-file="hospital-kubernetes-monitoring.json=${DASHBOARD_FILE}" \
  --dry-run=client -o yaml \
| kubectl label --local -f - grafana_dashboard=1 -o yaml \
| kubectl apply -f -

echo
echo "Done. The Grafana dashboard sidecar should import the dashboard automatically."
echo "Refresh Grafana Dashboards after a few seconds."
echo "If it does not appear, check that the Grafana dashboard sidecar is enabled."
