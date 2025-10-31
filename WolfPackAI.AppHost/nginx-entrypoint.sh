#!/bin/sh
# Nginx entrypoint script with certbot integration
# This script handles certificate initialization and nginx startup

set -e

DOMAIN="${DOMAIN:-localhost}"
CERT_PATH="/etc/letsencrypt/live/${DOMAIN}/fullchain.pem"
KEY_PATH="/etc/letsencrypt/live/${DOMAIN}/privkey.pem"

# Function to check if certificates exist
check_certs() {
    [ -f "$CERT_PATH" ] && [ -f "$KEY_PATH" ]
}

# Function to generate temporary self-signed certificate
generate_temp_cert() {
    echo "Generating temporary self-signed certificate for ${DOMAIN}..."
    mkdir -p "/etc/letsencrypt/live/${DOMAIN}"
    openssl req -x509 -nodes -days 1 -newkey rsa:2048 \
        -keyout "$KEY_PATH" \
        -out "$CERT_PATH" \
        -subj "/CN=${DOMAIN}" 2>/dev/null || true
}

# Check if certificates exist, if not create temporary ones
if ! check_certs; then
    echo "Certificates not found. Generating temporary self-signed certificate..."
    generate_temp_cert
    echo "Waiting for certbot to obtain real certificates..."
    echo "Note: HTTPS will work with self-signed cert until real certs are obtained"
fi

# Start nginx in background
echo "Starting nginx..."
nginx -g "daemon off;" &
NGINX_PID=$!

# Wait a bit for nginx to start
sleep 2

# Function to reload nginx
reload_nginx() {
    if kill -0 $NGINX_PID 2>/dev/null; then
        echo "Reloading nginx configuration..."
        nginx -s reload || true
    fi
}

# Monitor for certificate updates and reload nginx
while kill -0 $NGINX_PID 2>/dev/null; do
    if check_certs; then
        # Certificates exist, ensure nginx is using them
        reload_nginx
    fi
    sleep 30
done

wait $NGINX_PID

