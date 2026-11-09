#!/usr/bin/env bash
# Hosted Linux prerequisites for the strict non-Godot pre-merge selection.
set -euo pipefail
cd "$(dirname "$0")/.."
source gates/_setup-premerge.sh
premerge_setup_init linux

sudo apt-get update
sudo apt-get install -y --no-install-recommends \
    clang-19 cmake ninja-build binutils openssl \
    curl ca-certificates git unzip xz-utils
export CC=clang-19 CXX=clang++-19
premerge_export CC CXX
premerge_prepare_compilers
premerge_prepare_godot
{
    "$CC" --version
    dpkg-query -W clang-19 cmake ninja-build binutils openssl
    apt-cache policy clang-19 cmake ninja-build binutils openssl
} > "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
premerge_record_inputs
