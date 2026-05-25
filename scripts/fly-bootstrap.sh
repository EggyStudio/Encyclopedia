#!/usr/bin/env bash
# Idempotent first-time fly.io setup for this Encyclopedia instance.
#
# Run this once per fly.io app (yours, or someone forking and deploying their
# own). It is safe to re-run: each step checks whether its target already
# exists and skips it if so.
#
# Prerequisites:
#   - flyctl installed and authenticated  (`curl -L https://fly.io/install.sh | sh && flyctl auth login`)
#   - fly.toml present in the current directory (it is, in this repo)
#   - the app already exists on fly.io  (`flyctl launch --no-deploy` creates it; or `flyctl apps create encyclopedia`)
#
# Override the defaults via env vars:
#   APP=my-encyclopedia DB=my-encyclopedia-db REGION=ams ./scripts/fly-bootstrap.sh

set -euo pipefail

APP=${APP:-encyclopedia}
DB=${DB:-encyclopedia-db}
REGION=${REGION:-cdg}
VM_SIZE=${VM_SIZE:-shared-cpu-1x}
VOLUME_SIZE=${VOLUME_SIZE:-1}

step() { printf '\n\033[1;34m==> %s\033[0m\n' "$*"; }
note() { printf '   %s\n' "$*"; }

command -v flyctl >/dev/null 2>&1 || {
    echo "flyctl is not installed. Install it with:"
    echo "    curl -L https://fly.io/install.sh | sh"
    echo "Then add ~/.fly/bin to your PATH and re-run this script."
    exit 1
}

flyctl auth whoami >/dev/null 2>&1 || {
    echo "Not logged in to fly.io. Run:    flyctl auth login"
    exit 1
}

# ---- 1. Postgres cluster ----------------------------------------------------
step "1/3  Postgres cluster ($DB)"
if flyctl postgres list --json 2>/dev/null | grep -q "\"Name\":\"$DB\""; then
    note "Cluster $DB already exists - skipping create."
else
    flyctl postgres create \
        --name "$DB" \
        --region "$REGION" \
        --vm-size "$VM_SIZE" \
        --initial-cluster-size 1 \
        --volume-size "$VOLUME_SIZE"
    note "Cluster $DB created. The postgres superuser password was printed above - save it."
fi

# ---- 2. Attach to the app ---------------------------------------------------
step "2/3  Attach $DB to app $APP"
# `postgres attach` exits non-zero if already attached; treat that as success.
if flyctl postgres attach "$DB" --app "$APP" 2>&1 | tee /tmp/fly-attach.log; then
    note "Attached. DATABASE_URL was set on $APP and a redeploy was triggered."
else
    if grep -q "already attached\|already exists" /tmp/fly-attach.log; then
        note "Already attached - no change."
    else
        echo "Attach failed for a reason that wasn't 'already attached'. See output above."
        exit 1
    fi
fi
rm -f /tmp/fly-attach.log

# ---- 3. Required Postgres extensions ----------------------------------------
step "3/3  Install pg_trgm + unaccent extensions"
note "These are needed by db/migrations/001_init.sql (FTS + trigram search)."
note "The app's DB user can't CREATE EXTENSION, so this runs as the postgres superuser."
flyctl postgres connect -a "$DB" -d "$APP" <<'SQL'
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;
SQL

step "Done."
note "The app should now start cleanly. Watch the logs with:"
note "    flyctl logs -a $APP"
note "Then refresh https://$APP.fly.dev - Discover should load without an alert."
