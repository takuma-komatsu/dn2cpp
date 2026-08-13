// dn2cpp_system_collections_concurrent.cpp — System.Collections.Concurrent
// intrinsics. Currently: BlockingCollection<T>, a producer/consumer blocking queue
// backed by a real mutex + condition variables. One per-namespace intrinsic unit
// (see runtime/core/intrinsics/).
//
// Element storage is GC-safe by construction: each queued element lives in a
// GC-allocated node (dn2cpp_alloc) hung off the GC-allocated collection object, so
// the boxed/reference values are reachable from the GC heap and never collected out
// from under a blocked consumer — unlike a std::deque, whose nodes live in unscanned
// malloc memory. The blocking state (mutex/condvars/counters) lives in a separately
// `new`-ed control block that holds NO managed pointers, so the GC need not scan it;
// it is never freed (a small bounded per-collection leak, like the monitor table).
#include "dn2cpp_core.h"

#include <chrono>
#include <condition_variable>
#include <cstdint>
#include <mutex>

// The blocking/bounding state + sync primitives. `new`-ed (NOT GC-allocated): it
// holds non-trivial std::mutex/std::condition_variable members and no managed
// pointers. The collection object below points at it with a raw pointer; the GC sees
// that pointer but, as it does not address the GC heap, ignores it.
struct Dn2CppBlockingControl
{
    std::mutex mtx;                    // guards every field below + the node list
    std::condition_variable notEmpty; // signaled on Add / CompleteAdding
    std::condition_variable notFull;  // signaled on Take (bounded only)
    int32_t boundedCapacity;          // 0 => unbounded
    int32_t count;                    // current element count
    bool addingCompleted;             // CompleteAdding() has been called
};

// One queued element. GC-allocated and scanned, so `value` (a boxed value-type
// element or a reference element held directly) stays reachable while enqueued.
struct Dn2CppBlockingNode : Dn2CppObject
{
    Dn2CppObject* value;
    Dn2CppBlockingNode* next;
};

// The collection handle. GC-allocated: its head/tail keep the whole FIFO node chain
// (and thus every queued value) GC-reachable. The element type is irrelevant here —
// the call site boxes/unboxes by T's kind, so one storage shape serves every T.
struct Dn2CppBlockingCollection : Dn2CppObject
{
    Dn2CppBlockingControl* ctl;  // non-GC control block (sync + counters)
    Dn2CppBlockingNode* head;    // dequeue end (oldest); nullptr when empty
    Dn2CppBlockingNode* tail;    // enqueue end (newest); nullptr when empty
};

// Each static type-info bakes its interned Type companion in (lock-free typeof/GetType).
// Non-const and externally visible: the generated init prologue installs its IDisposable
// dispatch row, whose entry names program-specific emitted type-info.
extern const Dn2CppType dn2cpp_blockingcollection_type_obj;
Dn2CppTypeInfo dn2cpp_blockingcollection_type =
    dn2cpp_ti_with_typeobject({ "System.Collections.Concurrent.BlockingCollection`1", nullptr, (int32_t)sizeof(Dn2CppBlockingCollection), nullptr, nullptr, 0 }, &dn2cpp_blockingcollection_type_obj);
const Dn2CppType dn2cpp_blockingcollection_type_obj = { { &dn2cpp_type_type }, &dn2cpp_blockingcollection_type };
extern const Dn2CppType dn2cpp_blockingnode_type_obj;
static const Dn2CppTypeInfo dn2cpp_blockingnode_type =
    dn2cpp_ti_with_typeobject({ "System.Collections.Concurrent.BlockingCollection`1+Node", nullptr, (int32_t)sizeof(Dn2CppBlockingNode), nullptr, nullptr, 0 }, &dn2cpp_blockingnode_type_obj);
const Dn2CppType dn2cpp_blockingnode_type_obj = { { &dn2cpp_type_type }, &dn2cpp_blockingnode_type };

// Link a value at the tail. Caller holds ctl->mtx. The node alloc may run the GC,
// which is safe: the GC does not acquire ctl->mtx, and `boxedOrRef` is live on the
// caller's stack (so it is scanned) until it is stored into the node.
static void dn2cpp_blockingcoll_enqueue_locked(Dn2CppBlockingCollection* c, Dn2CppObject* boxedOrRef)
{
    auto* node = static_cast<Dn2CppBlockingNode*>(dn2cpp_alloc(sizeof(Dn2CppBlockingNode)));
    node->type = &dn2cpp_blockingnode_type;
    dn2cpp_gc_store_ref(&node->value, boxedOrRef);
    node->next = nullptr;
    // The collection and the old tail are old and possibly black, and they live
    // in different blocks — each linking store must dirty its own slot.
    if (c->tail != nullptr)
        dn2cpp_gc_store_ref(&c->tail->next, node);
    else
        dn2cpp_gc_store_ref(&c->head, node);
    dn2cpp_gc_store_ref(&c->tail, node);
    c->ctl->count++;
}

// Unlink and return the head value. Caller holds ctl->mtx and has checked count > 0.
// The returned pointer keeps the value alive on the caller's stack; the drained node
// is cleared so a stale chain cannot retain the value past consumption.
static Dn2CppObject* dn2cpp_blockingcoll_dequeue_locked(Dn2CppBlockingCollection* c)
{
    Dn2CppBlockingNode* node = c->head;
    Dn2CppObject* v = node->value;
    c->head = node->next;
    if (c->head == nullptr)
        c->tail = nullptr;
    c->ctl->count--;
    node->next = nullptr;
    node->value = nullptr;
    return v;
}

Dn2CppObject* dn2cpp_blockingcoll_new(int32_t boundedCapacity)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(dn2cpp_alloc(sizeof(Dn2CppBlockingCollection)));
    c->type = &dn2cpp_blockingcollection_type;
    c->ctl = new Dn2CppBlockingControl();
    c->ctl->boundedCapacity = boundedCapacity;
    c->ctl->count = 0;
    c->ctl->addingCompleted = false;
    c->head = nullptr;
    c->tail = nullptr;
    return c;
}

// Add(item): block while a bounded collection is full; throw InvalidOperationException
// (catchable) if adding is — or becomes, while blocked — completed. The unique_lock
// releases ctl->mtx as the C++ exception unwinds the stack.
void dn2cpp_blockingcoll_add(Dn2CppObject* coll, Dn2CppObject* boxedOrRef)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    auto* ctl = c->ctl;
    std::unique_lock<std::mutex> lk(ctl->mtx);
    if (ctl->addingCompleted)
        dn2cpp_throw_invalid_operation();
    if (ctl->boundedCapacity > 0)
        ctl->notFull.wait(lk, [&] { return ctl->count < ctl->boundedCapacity || ctl->addingCompleted; });
    if (ctl->addingCompleted)
        dn2cpp_throw_invalid_operation();
    dn2cpp_blockingcoll_enqueue_locked(c, boxedOrRef);
    ctl->notEmpty.notify_one();
}

// TryAdd(item [, timeout]): like Add but bounded, returns 0 instead of blocking past
// the timeout (0 = single immediate attempt, negative = infinite). Still throws
// InvalidOperationException when adding is completed.
int32_t dn2cpp_blockingcoll_tryadd(Dn2CppObject* coll, Dn2CppObject* boxedOrRef, int32_t timeoutMs)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    auto* ctl = c->ctl;
    std::unique_lock<std::mutex> lk(ctl->mtx);
    if (ctl->addingCompleted)
        dn2cpp_throw_invalid_operation();
    if (ctl->boundedCapacity > 0 && ctl->count >= ctl->boundedCapacity)
    {
        if (timeoutMs == 0)
            return 0;
        if (timeoutMs < 0)
            ctl->notFull.wait(lk, [&] { return ctl->count < ctl->boundedCapacity || ctl->addingCompleted; });
        else
            ctl->notFull.wait_for(lk, std::chrono::milliseconds(timeoutMs),
                [&] { return ctl->count < ctl->boundedCapacity || ctl->addingCompleted; });
        if (ctl->addingCompleted)
            dn2cpp_throw_invalid_operation();
        if (ctl->count >= ctl->boundedCapacity)
            return 0; // timed out, still full
    }
    dn2cpp_blockingcoll_enqueue_locked(c, boxedOrRef);
    ctl->notEmpty.notify_one();
    return 1;
}

// Take(): block until an element is available; throw InvalidOperationException when
// the collection is completed and empty (drained). Returns the boxed/reference value.
Dn2CppObject* dn2cpp_blockingcoll_take(Dn2CppObject* coll)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    auto* ctl = c->ctl;
    std::unique_lock<std::mutex> lk(ctl->mtx);
    ctl->notEmpty.wait(lk, [&] { return ctl->count > 0 || ctl->addingCompleted; });
    if (ctl->count == 0)
        dn2cpp_throw_invalid_operation(); // completed && empty
    Dn2CppObject* v = dn2cpp_blockingcoll_dequeue_locked(c);
    ctl->notFull.notify_one();
    return v;
}

// TryTake(out item [, timeout]): 1 + *out set on success; 0 + *out = nullptr on
// timeout or completed-empty (0 = single immediate attempt, negative = infinite).
int32_t dn2cpp_blockingcoll_trytake(Dn2CppObject* coll, int32_t timeoutMs, Dn2CppObject** out)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    auto* ctl = c->ctl;
    std::unique_lock<std::mutex> lk(ctl->mtx);
    if (ctl->count == 0)
    {
        if (ctl->addingCompleted || timeoutMs == 0)
        {
            *out = nullptr;
            return 0;
        }
        if (timeoutMs < 0)
            ctl->notEmpty.wait(lk, [&] { return ctl->count > 0 || ctl->addingCompleted; });
        else
            ctl->notEmpty.wait_for(lk, std::chrono::milliseconds(timeoutMs),
                [&] { return ctl->count > 0 || ctl->addingCompleted; });
        if (ctl->count == 0)
        {
            *out = nullptr;
            return 0; // timed out, or completed while blocked and still empty
        }
    }
    *out = dn2cpp_blockingcoll_dequeue_locked(c);
    ctl->notFull.notify_one();
    return 1;
}

// CompleteAdding(): mark adding complete and wake every blocked producer/consumer so
// each re-evaluates its predicate (consumers drain then throw; producers throw).
void dn2cpp_blockingcoll_complete_adding(Dn2CppObject* coll)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    auto* ctl = c->ctl;
    {
        std::lock_guard<std::mutex> lk(ctl->mtx);
        ctl->addingCompleted = true;
    }
    ctl->notEmpty.notify_all();
    ctl->notFull.notify_all();
}

int32_t dn2cpp_blockingcoll_count(Dn2CppObject* coll)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    std::lock_guard<std::mutex> lk(c->ctl->mtx);
    return c->ctl->count;
}

int32_t dn2cpp_blockingcoll_is_adding_completed(Dn2CppObject* coll)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    std::lock_guard<std::mutex> lk(c->ctl->mtx);
    return c->ctl->addingCompleted ? 1 : 0;
}

int32_t dn2cpp_blockingcoll_is_completed(Dn2CppObject* coll)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    std::lock_guard<std::mutex> lk(c->ctl->mtx);
    return (c->ctl->addingCompleted && c->ctl->count == 0) ? 1 : 0;
}

// BoundedCapacity: the unbounded sentinel (0) maps to .NET's -1.
int32_t dn2cpp_blockingcoll_bounded_capacity(Dn2CppObject* coll)
{
    auto* c = static_cast<Dn2CppBlockingCollection*>(coll);
    int32_t cap = c->ctl->boundedCapacity;
    return cap > 0 ? cap : -1;
}
