#!/usr/bin/env bash
# A plain C# destructor (~T()) becomes a
# reachable Finalize() override, wired into the type-info and run by the
# dedicated finalizer thread once GC.Collect() + GC.WaitForPendingFinalizers()
# observe the instance unreachable and collected. Covers a direct override and
# a subclass that inherits its base's override (Compilation.EffectiveFinalize's
# base-chain walk), GC.SuppressFinalize/ReRegisterForFinalize, resurrection
# (including its interaction with short/long WeakReference<T>), best-effort
# unrun finalizers at process exit, and an uncaught finalizer exception
# aborting the process (must stay the last section in Program.cs).
#
# Also covers SuppressFinalize reaching an instance the collector has ALREADY
# queued (FinalizerSuppressQueuedSubset), whose subject is the finalization queue
# rather than any destructor shape: deregistering with the collector cannot
# un-queue such an entry, so the queue's consumer has to drop it, and the only
# managed route into that window is a long WeakReference read from inside another
# finalizer body (which is what holds the queue still). The same section pins the
# undo direction — a repeated suppress followed by ReRegisterForFinalize must
# still finalize. Its teeth are the finalization COUNTS, not the window: the
# wasm axis runs the same program and never opens the window, because that build
# never collects an object first named from inside a finalizer body.
#
# The undone suppress is asserted with the instance STILL ROOTED, which is what
# makes the assertion an invariant rather than a host lottery: a run that drops
# the queued entry and waits for a second unreachability reads 0 there on every
# host, where a conservative collector may otherwise grant that second death and
# hide the loss. The rooted line folds the window in, so the wasm axis — which
# has nothing queued to run — still prints what real .NET prints.
#
# The mirror ordering — re-register FIRST, then suppress, both while queued —
# must come out the other way (measured against net10.0): the queued entry is
# dropped, yet the re-registration survives to the NEXT unreachability, so the
# count is asserted zero while still rooted and one after un-rooting. That
# un-rooted half also arms the consumer-side hygiene: the drop leaves an object
# that must become collectable a second time, which a sleeping finalizer
# thread's unscrubbed dead frames would otherwise pin forever.
#
# Also covers the THIRD allocation mouth (FinalizerClonedSubset), whose subject
# is Object.MemberwiseClone rather than any destructor shape: a clone is a fresh
# instance of a finalizable type, so real .NET queues it for finalization exactly as it
# queues the original (measured: two finalizations for one clone). dn2cpp registers at
# newobj and at the reflective-ctor path (FinalizerActivatorSubset, its sibling); the
# clone path is easy to leave unregistered, and nothing says so — the cloned object's
# finalizer simply never runs. It reaches MemberwiseClone through a reflective Invoke
# because the method is `protected`. Former gates: none (new area).
source "$(dirname "$0")/_common.sh"

corelib_diff_gate Finalizers
