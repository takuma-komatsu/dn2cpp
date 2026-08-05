#!/usr/bin/env bash
# Blocking synchronization primitives on real threads — SemaphoreSlim (N consumers
# wait, producer releases N), ManualResetEventSlim (gate N workers, one Set releases all,
# IsSet), AutoResetEvent (strict ping-pong, 3 turns each), Monitor condition signaling,
# CountdownEvent (N workers Signal, main Wait), and Barrier (N threads run P phases,
# with a post-phase action). MethodImplSubset.cs adds MethodImplAttribute:
# [MethodImpl(Synchronized)] instance/static/recursive/throwing bodies hammered
# by racing threads, lock(typeof(X)) identity (interned Type objects) and its
# mutual exclusion with static Synchronized methods, plus the NoInlining/
# AggressiveInlining hints. Every result is read after Join, so the output is
# deterministic and diffed exact vs real .NET. CountdownEvent/Barrier live in
# the System.Threading assembly (not CoreLib), so it is referenced alongside
# CoreLib. RwLockRecursionSubset.cs asserts ReaderWriterLockSlim's per-thread
# ownership: the LockRecursionException matrix under NoRecursion AND
# SupportsRecursion (type + message verbatim — each of these used to be a silent
# same-thread HANG), the upgradeable holder's granted read/upgrade paths, the
# per-thread Is*LockHeld queries, RecursionPolicy, and the
# SynchronizationLockException release-without-hold checks.
source "$(dirname "$0")/_common.sh"
corelib_diff_gate SyncPrimitives System.Threading
