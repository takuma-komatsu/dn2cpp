#!/usr/bin/env bash
# Internal suite selector; pre-merge exposes --with-culture. Direct gates run.
DN2CPP_INCLUDE_CULTURE=${DN2CPP_INCLUDE_CULTURE:-0}
case "$DN2CPP_INCLUDE_CULTURE" in
    0|1) ;;
    *) echo 'error: culture gate selection must be 0 or 1' >&2; return 2 ;;
esac
CULTURE_GATE=gates/build-and-run-culture-info-api.sh
culture_gate_selected() {
    [ "$DN2CPP_INCLUDE_CULTURE" = 1 ] || [ "$1" != "$CULTURE_GATE" ]
}
culture_project_selected() {
    [ "$DN2CPP_INCLUDE_CULTURE" = 1 ] || [ "$1" != CultureInfoApi ]
}
