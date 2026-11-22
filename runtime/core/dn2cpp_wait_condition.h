#pragma once

#include <condition_variable>
#include <cstdint>
#include <mutex>

bool dn2cpp_on_entry_thread();
void dn2cpp_entry_wait(const void* condition, std::unique_lock<std::mutex>& lock);
void dn2cpp_entry_resume();
void dn2cpp_entry_notify(const void* condition, bool runnable);

// A poll of several objects cannot hold a single predicate lock. Sample before
// probing them so a signal racing the decision to park wins the handoff.
struct Dn2CppEntryPollingWait
{
    explicit Dn2CppEntryPollingWait(bool indefinite);
    ~Dn2CppEntryPollingWait();
    uint64_t sample() const;
    void park_if_unchanged(uint64_t sample) const;
    void resume() const;
    const bool observing;
};

bool dn2cpp_scheduler_has_work();
void dn2cpp_entry_poll_changed();

// An indefinite managed wait parks the entry thread. The signaler restores it
// as a possible task settler before it can block on work the entry will do.
class Dn2CppWaitCondition : public std::condition_variable
{
public:
    void wait(std::unique_lock<std::mutex>& lock)
    {
        if (!dn2cpp_on_entry_thread())
            return std::condition_variable::wait(lock);
        dn2cpp_entry_wait(this, lock);
        dn2cpp_entry_resume();
    }

    template<class Predicate>
    void wait(std::unique_lock<std::mutex>& lock, Predicate ready)
    {
        if (!dn2cpp_on_entry_thread())
            return std::condition_variable::wait(lock, ready);
        if (!ready())
        {
            do {
                dn2cpp_entry_wait(this, lock);
            } while (!ready());
            dn2cpp_entry_resume();
        }
    }

    void notify_one()
    {
        dn2cpp_entry_notify(this, true);
        std::condition_variable::notify_one();
    }

    void notify_all()
    {
        dn2cpp_entry_notify(this, true);
        std::condition_variable::notify_all();
    }

    void recheck()
    {
        dn2cpp_entry_notify(this, false);
        std::condition_variable::notify_all();
    }
};
