namespace Dn2Cpp;

/// <summary>One dimension of the runtime generic context: the slot registries,
/// per-user tables and fill state keyed on a canonical owner of type
/// <typeparamref name="TOwner"/> — a <see cref="ClassInfo"/> for the class
/// dimension, a <see cref="MethodInfo"/> for generic-method instantiations. The
/// two dimensions differ only in what an owner and a user <i>are</i> — exactly the
/// questions the hooks below ask — so the fill loop exists once and runs twice.
///
/// <para><b>Order is output.</b> A slot's index is its position in the owner's
/// registry, and that index is emitted into the table and read back by the shared
/// body, so every traversal here is fixed: owners in first-slot-allocation order by
/// index (resolving a slot can append an owner), users in <c>SharedUsers</c> order
/// off a snapshot (linking can append a user mid-resolve), slots from the first
/// unfilled index up. Only <see cref="Tables"/> re-sorts, and it must, because
/// <c>SharedUsers</c> order is a function of how discovery walked the model.</para></summary>
internal sealed class RgctxDimension<TOwner> where TOwner : class
{
    /// <summary>Resolves one slot of <paramref name="owner"/>'s registry under
    /// <paramref name="user"/>'s real generic context, with the note/reach side
    /// effects the equivalent per-instantiation body site would have.</summary>
    internal delegate string SlotResolver(TOwner owner, TOwner user, RgctxSlot slot);

    private readonly Func<TOwner, IReadOnlyList<TOwner>> _sharedUsers;
    private readonly Comparison<TOwner> _compare;
    private readonly Func<TOwner, bool> _isSatellite;
    private readonly Func<TOwner, bool> _isLive;
    private readonly SlotResolver _resolve;

    private readonly Dictionary<TOwner, RgctxRegistry> _registries = new();
    private readonly List<TOwner> _ownerOrder = new();
    // Per grouped real user, the resolved table entries (aligned to its owner's
    // registry slots; extended incrementally as slots appear).
    private readonly Dictionary<TOwner, List<string>> _tables = new();
    private readonly List<TOwner> _userOrder = new();
    // Slots whose planning-pass fill failed to resolve for some real user —
    // FinalizeSharedGenerics taints every body using one, and later allocation
    // attempts taint immediately.
    private readonly HashSet<(TOwner, RgctxSlot)> _badSlots = new();
    // Users whose table another (live) table's forwarding entry references —
    // they must fill even before any own method is reached.
    private readonly HashSet<TOwner> _tableRequired = new();

    /// <param name="sharedUsers">The owner's group members, in linkage order.</param>
    /// <param name="compare">The content-derived emission order (see <see cref="Tables"/>).</param>
    /// <param name="isSatellite">Whether the user is a canonical-world satellite: it
    /// never exists at runtime — no receiver carries its type-info and no forwarder
    /// appends its table — so it must not fill, or its entries would name
    /// never-emitted canonical-world symbols.</param>
    /// <param name="isLive">Whether the user can observe a context at all, <i>apart
    /// from</i> the cross-table requirement this dimension owns (tested first). Group
    /// membership alone is not enough: filling a dead member's table would force-emit
    /// its statics/cctors.</param>
    internal RgctxDimension(
        Func<TOwner, IReadOnlyList<TOwner>> sharedUsers,
        Comparison<TOwner> compare,
        Func<TOwner, bool> isSatellite,
        Func<TOwner, bool> isLive,
        SlotResolver resolve)
    {
        _sharedUsers = sharedUsers;
        _compare = compare;
        _isSatellite = isSatellite;
        _isLive = isLive;
        _resolve = resolve;
    }

    /// <summary>Allocates (or reuses) <paramref name="owner"/>'s slot for the
    /// given kind + raw IL token, returning its table index.</summary>
    internal int SlotIndex(TOwner owner, RgctxSlotKind kind, int token)
    {
        if (!_registries.TryGetValue(owner, out var reg))
        {
            reg = new RgctxRegistry();
            _registries[owner] = reg;
            _ownerOrder.Add(owner);
        }
        return reg.GetOrAdd(kind, token);
    }

    /// <summary>Whether the slot is known unresolvable (its planning-pass fill
    /// failed for some group member) — the allocating body must taint.</summary>
    internal bool SlotKnownBad(TOwner owner, RgctxSlotKind kind, int token) =>
        _badSlots.Contains((owner, new RgctxSlot(kind, token)));

    /// <summary>Whether the recorded (owner, slot) key failed its planning fill.</summary>
    internal bool SlotKnownBad((TOwner Owner, RgctxSlot Slot) key) => _badSlots.Contains(key);

    /// <summary>Records that a live table's forwarding entry references
    /// <paramref name="user"/>'s table, so it must fill and emit even when the
    /// user is otherwise unreachable (the shared callee runs under its identity
    /// through that entry).</summary>
    internal void RequireTable(TOwner user) => _tableRequired.Add(user);

    /// <summary>Total distinct slots across the registries (metrics).</summary>
    internal int SlotTotal()
    {
        int n = 0;
        foreach (var reg in _registries.Values)
            n += reg.Slots.Count;
        return n;
    }

    /// <summary>Resets the registries and fill state between the planning and
    /// emission passes; the bad-slot record survives (its taints were already
    /// applied), and the cross-table requirements are rediscovered by the
    /// emission fill (planning may have recorded them for later-dropped
    /// bodies).</summary>
    internal void ResetForEmission()
    {
        _registries.Clear();
        _ownerOrder.Clear();
        _tables.Clear();
        _userOrder.Clear();
        _tableRequired.Clear();
    }

    /// <summary>The per-user tables for the emitter, in the comparison's order:
    /// each user with a non-empty owner registry gets one table. Sorted rather than
    /// yielded in fill order, because linkage appends group members in discovery
    /// order, which would otherwise leak into the emitted output. (Slot <i>indices</i>
    /// within a table are already order-independent: they come from the owner's
    /// registry, filled during the sorted body compile.)</summary>
    internal IEnumerable<(TOwner User, IReadOnlyList<string> Entries)> Tables()
    {
        var users = new List<TOwner>(_userOrder);
        users.Sort(_compare);
        foreach (var user in users)
            if (_tables[user].Count > 0)
                yield return (user, _tables[user]);
    }

    /// <summary>Resolves every not-yet-filled (slot, group member) pair of this
    /// dimension, incrementally and idempotently; returns whether anything new
    /// was filled. In <paramref name="planning"/> mode an unresolvable slot
    /// degrades to <c>nullptr</c> and is recorded bad — that record IS the
    /// planning pass's product; in emission mode the emitter replays a subset of
    /// the planning slots, so a failure there is a real bug and escapes.</summary>
    internal bool Fill(bool planning)
    {
        bool changed = false;
        // Index loop: resolving a slot can append an owner.
        for (int oi = 0; oi < _ownerOrder.Count; oi++)
        {
            var owner = _ownerOrder[oi];
            var reg = _registries[owner];
            // Snapshot: linking can append users while we resolve.
            foreach (var user in _sharedUsers(owner).ToList())
            {
                if (_isSatellite(user))
                    continue;
                // Dead group members get no table (and none of the fill's
                // note/reach side effects); the per-round fill catches a member
                // up in full the round it first turns live.
                if (!_tables.ContainsKey(user) && !IsLive(user))
                    continue;
                if (!_tables.TryGetValue(user, out var entries))
                {
                    entries = new List<string>();
                    _tables[user] = entries;
                    _userOrder.Add(user);
                }
                for (int i = entries.Count; i < reg.Slots.Count; i++)
                {
                    changed = true;
                    if (planning)
                    {
                        try
                        {
                            entries.Add(_resolve(owner, user, reg.Slots[i]));
                        }
                        // IsMustEscape is load-bearing: a strict violation or a tripped
                        // instantiation bound must abort the run, not degrade the slot to
                        // a fallback — and InstantiationBoundException IS a
                        // NotSupportedException, so the type list alone would swallow it.
                        catch (Exception e) when (!Compilation.IsMustEscape(e)
                                                  && e is NotSupportedException or InvalidOperationException
                                                      or KeyNotFoundException)
                        {
                            _badSlots.Add((owner, reg.Slots[i]));
                            entries.Add("nullptr");
                        }
                    }
                    else
                    {
                        entries.Add(_resolve(owner, user, reg.Slots[i]));
                    }
                }
            }
        }
        return changed;
    }

    private bool IsLive(TOwner user) => _tableRequired.Contains(user) || _isLive(user);
}

/// <summary>The runtime generic context subsystem: the class dimension and the
/// generic-method dimension, and the joint operations the emit pipeline drives
/// them through. <see cref="Compilation"/> owns one and fronts it with the
/// facade the emitter and MethodCompiler call.
///
/// <para>The class dimension's context is receiver-derivable (a real
/// instantiation's type-info anchors the walk); the method dimension's never is,
/// so every real generic-method instantiation carries a per-method table that a
/// one-line forwarder appends. That difference lives entirely in the emitter and
/// in <see cref="Compilation.WouldNeedRgctxParam"/>; the bookkeeping below is the
/// same on both sides.</para></summary>
internal sealed class RgctxSystem
{
    /// <summary>The class dimension: slots owned by a canonical owner class,
    /// tables carried by its grouped real instantiations.</summary>
    internal RgctxDimension<ClassInfo> Classes { get; }

    /// <summary>The generic-method dimension: slots owned by a canonical
    /// generic-method instantiation, tables carried by its linked real
    /// instantiations.</summary>
    internal RgctxDimension<MethodInfo> Methods { get; }

    internal RgctxSystem(RgctxDimension<ClassInfo> classes, RgctxDimension<MethodInfo> methods)
    {
        Classes = classes;
        Methods = methods;
    }

    /// <summary>Total distinct slots across both dimensions' final registries
    /// (metrics).</summary>
    internal int SlotTotal() => Classes.SlotTotal() + Methods.SlotTotal();

    /// <summary>Resets both dimensions between the planning and emission passes,
    /// so the final registries hold exactly the retained shared bodies' slots in
    /// deterministic compile order.</summary>
    internal void ResetForEmission()
    {
        Classes.ResetForEmission();
        Methods.ResetForEmission();
    }

    /// <summary>Fills both dimensions, class first: a class-dimension entry can
    /// require a method table (and vice versa), and running the class pass to
    /// completion before the method pass is the order the slot indices — and
    /// therefore the output bytes — were recorded under. The caller re-runs to a
    /// fixpoint, so a requirement discovered in the wrong direction is picked up
    /// on the next round rather than lost.</summary>
    internal bool Fill(bool planning)
    {
        bool changed = Classes.Fill(planning);
        changed |= Methods.Fill(planning);
        return changed;
    }
}
