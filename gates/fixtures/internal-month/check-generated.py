#!/usr/bin/env python3
"""Shared typed-cache proof retains original bounds, boxing and Wasm liveness checks."""
import argparse
from pathlib import Path
import subprocess
import sys
parser = argparse.ArgumentParser()
parser.add_argument('output', type=Path)
parser.add_argument('--minimal', action='store_true')
args = parser.parse_args()
command = [sys.executable, str(Path(__file__).resolve().parents[1] / 'dtfi-name-arrays/check-generated.py'), str(args.output), '--scalar', 'internal']
if args.minimal: command.append('--minimal')
subprocess.run(command, check=True)
