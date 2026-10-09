#!/bin/sh
# Disposable, loopback-only synthetic fixture. No existing DB/container is used.
set -eu
repo=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
sdk=${DMV_DOTNET:-dotnet}
fixture=$(mktemp -d "${TMPDIR:-/tmp}/dmv-pg.XXXXXXXX")
chmod 700 "$fixture"
container="dmv-native-test-$(basename "$fixture" | tr '[:upper:]' '[:lower:]')"
image="$container:fixture"
cleanup() {
  docker rm -f "$container" >/dev/null 2>&1 || true
  docker image rm "$image" >/dev/null 2>&1 || true
  rm -rf "$fixture"
}
trap cleanup EXIT INT TERM
openssl req -x509 -newkey rsa:2048 -nodes -keyout "$fixture/server.key" -out "$fixture/server.crt" -days 2 -subj '/CN=localhost' -addext 'subjectAltName=DNS:localhost' >/dev/null 2>&1
openssl req -x509 -newkey rsa:2048 -nodes -keyout "$fixture/untrusted.key" -out "$fixture/untrusted.crt" -days 2 -subj '/CN=untrusted-fixture' >/dev/null 2>&1
printf '%s\n' 'FROM postgres:17@sha256:d74eeac9a635390a49bc21bd49fccd973de707e2a53a76ac49b552b8712ec46f' 'COPY server.key server.crt /tls/' 'RUN chown postgres:postgres /tls/* && chmod 600 /tls/server.key' > "$fixture/Dockerfile"
docker build -q -t "$image" "$fixture" >/dev/null
password=$(openssl rand -hex 24)
printf 'POSTGRES_PASSWORD=%s\n' "$password" > "$fixture/container.env"
chmod 600 "$fixture/container.env"
docker run -d --name "$container" --env-file "$fixture/container.env" -p 127.0.0.1::5432 "$image" -c ssl=on -c ssl_cert_file=/tls/server.crt -c ssl_key_file=/tls/server.key >/dev/null
attempt=0
until docker exec "$container" pg_isready -U postgres >/dev/null 2>&1; do
  attempt=$((attempt+1)); [ "$attempt" -lt 45 ] || exit 1
  sleep 1
done
docker exec -i "$container" psql -U postgres -v ON_ERROR_STOP=1 < "$repo/tests/fixtures/postgres.sql" >/dev/null
docker exec -i "$container" psql -U postgres -v ON_ERROR_STOP=1 < "$repo/tests/fixtures/postgres-structure.sql" >/dev/null
port=$(docker port "$container" 5432/tcp | sed 's/.*://')
export DMV_TEST_PG_PORT="$port" DMV_TEST_PG_PASSWORD="$password" DMV_TEST_PG_CA="$fixture/server.crt" DMV_TEST_PG_BAD_CA="$fixture/untrusted.crt"
cd "$repo"
"$sdk" test --no-restore --logger 'trx;LogFileName=native-01-pg.trx'
if [ "${DMV_KEEP_TEST_FIXTURE:-0}" = 1 ]; then
  export DMV_TEST_CONTAINER="$container" DMV_TEST_IMAGE="$image"
  python3 - <<'PY'
import os,json,pathlib
root=pathlib.Path(os.environ['DMV_TEST_PG_CA']).parent
p=root/'ui.json'
p.write_text(json.dumps({'port':os.environ['DMV_TEST_PG_PORT'],'password':os.environ['DMV_TEST_PG_PASSWORD'],'ca':os.environ['DMV_TEST_PG_CA'],'container':os.environ['DMV_TEST_CONTAINER'],'image':os.environ['DMV_TEST_IMAGE']}))
p.chmod(0o600)
print('Disposable UI fixture:',root)
PY
  trap - EXIT INT TERM
fi
