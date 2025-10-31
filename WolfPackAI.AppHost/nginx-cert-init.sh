#!/bin/sh
# Generate a temporary self-signed certificate if Let's Encrypt certs are missing

set -e

DOMAIN="${DOMAIN:-localhost}"
CERT_DIR="/etc/letsencrypt/live/${DOMAIN}"
CERT_PATH="${CERT_DIR}/fullchain.pem"
KEY_PATH="${CERT_DIR}/privkey.pem"

echo "[nginx-init] Domain: ${DOMAIN}"

# Ensure openssl is available (nginx:alpine may not include it)
if ! command -v openssl >/dev/null 2>&1; then
  echo "[nginx-init] openssl not found. Installing via apk..."
  if command -v apk >/dev/null 2>&1; then
    apk add --no-cache openssl >/dev/null
  else
    echo "[nginx-init] ERROR: apk not available; cannot install openssl."
    exit 1
  fi
fi

if [ ! -f "${CERT_PATH}" ] || [ ! -f "${KEY_PATH}" ]; then
  echo "[nginx-init] Certificates not found. Creating temporary self-signed cert at ${CERT_DIR}..."
  mkdir -p "${CERT_DIR}"
  openssl req -x509 -nodes -days 1 -newkey rsa:2048 \
    -keyout "${KEY_PATH}" \
    -out "${CERT_PATH}" \
    -subj "/CN=${DOMAIN}"
  chmod 600 "${KEY_PATH}"
  chmod 644 "${CERT_PATH}"
  echo "[nginx-init] Temporary certificate generated."
else
  echo "[nginx-init] Certificates already exist for ${DOMAIN}. Skipping temp cert generation."
fi

exit 0


