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

# Start background watcher to reload nginx when real certs appear or change
STAMP_FILE="/etc/letsencrypt/.last-reload"

# Initialize stamp file to current time
date > "${STAMP_FILE}" 2>/dev/null || touch "${STAMP_FILE}"

(
  while true; do
    # Prefer a certbot-managed lineage like /etc/letsencrypt/live/${DOMAIN}-0001 if present
    REAL_LINEAGE_DIR=""
    for d in /etc/letsencrypt/live/${DOMAIN}-*; do
      if [ -d "$d" ] && [ -f "$d/fullchain.pem" ] && [ -f "$d/privkey.pem" ]; then
        REAL_LINEAGE_DIR="$d"
        break
      fi
    done

    if [ -n "$REAL_LINEAGE_DIR" ]; then
      # Ensure target directory exists
      mkdir -p "${CERT_DIR}"
      # Link real certs into the expected paths so nginx uses them
      CURRENT_CERT_TARGET=""
      CURRENT_KEY_TARGET=""
      [ -L "${CERT_PATH}" ] && CURRENT_CERT_TARGET="$(readlink -f "${CERT_PATH}" 2>/dev/null || echo '')"
      [ -L "${KEY_PATH}" ] && CURRENT_KEY_TARGET="$(readlink -f "${KEY_PATH}" 2>/dev/null || echo '')"

      if [ "$CURRENT_CERT_TARGET" != "$REAL_LINEAGE_DIR/fullchain.pem" ] || [ "$CURRENT_KEY_TARGET" != "$REAL_LINEAGE_DIR/privkey.pem" ]; then
        echo "[nginx-init] Found certbot certificate at $REAL_LINEAGE_DIR. Switching nginx to use it..."
        ln -sf "$REAL_LINEAGE_DIR/fullchain.pem" "${CERT_PATH}"
        ln -sf "$REAL_LINEAGE_DIR/privkey.pem" "${KEY_PATH}"
        echo "[nginx-init] Reloading nginx to apply real certificate..."
        nginx -s reload 2>/dev/null || true
        date > "${STAMP_FILE}" 2>/dev/null || touch "${STAMP_FILE}"
      else
        # If symlinks already point to the real lineage, reload when lineage files update (renewals)
        if [ "$REAL_LINEAGE_DIR/fullchain.pem" -nt "${STAMP_FILE}" ] || [ "$REAL_LINEAGE_DIR/privkey.pem" -nt "${STAMP_FILE}" ]; then
          echo "[nginx-init] Renewal detected in $REAL_LINEAGE_DIR. Reloading nginx..."
          nginx -s reload 2>/dev/null || true
          date > "${STAMP_FILE}" 2>/dev/null || touch "${STAMP_FILE}"
        fi
      fi
    else
      # Fallback: reload if temp cert files themselves changed
      if [ -f "${CERT_PATH}" ] && [ -f "${KEY_PATH}" ]; then
        if [ "${CERT_PATH}" -nt "${STAMP_FILE}" ] || [ "${KEY_PATH}" -nt "${STAMP_FILE}" ]; then
          echo "[nginx-init] Certificate update detected. Reloading nginx..."
          nginx -s reload 2>/dev/null || true
          date > "${STAMP_FILE}" 2>/dev/null || touch "${STAMP_FILE}"
        fi
      fi
    fi
    sleep 30
  done
) &

exit 0


