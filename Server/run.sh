#!/bin/sh
# Starts the AI art local server. Arguments are passed to artai.py.
cd "$(dirname "$0")" || exit 1
UV="$HOME/.local/bin/uv"
[ -x "$UV" ] || UV=uv
exec "$UV" run artai.py "$@"
