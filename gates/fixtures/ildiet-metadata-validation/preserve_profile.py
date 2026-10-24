"""Representative preservation witnesses and an explicit exhaustive profile."""
import os

_value = os.environ.get("DN2CPP_PRESERVE_CONTROL_FULL", "0")
if _value not in ("0", "1"):
    raise ValueError("DN2CPP_PRESERVE_CONTROL_FULL must be 0 or 1")
FULL = _value == "1"


def select_cases(subject, available, representatives):
    """Select named cases without changing their independent fixture inputs."""
    available = tuple(available)
    names = tuple(item if isinstance(item, str) else item[0] for item in available)
    representatives = tuple(representatives)
    if not representatives or len(set(representatives)) != len(representatives):
        raise ValueError("Empty or duplicate representatives: " + subject)
    if len(set(names)) != len(names) or not set(representatives) <= set(names):
        raise ValueError("Unknown or duplicate cases: " + subject)
    selected = available if FULL else tuple(item for name, item in zip(names, available) if name in representatives)
    print("graph-selection=" + subject + ":" + ("full" if FULL else "representative") + ":" +
          ",".join(item if isinstance(item, str) else item[0] for item in selected), flush=True)
    return selected


def sharing_modes(sentinel=False):
    return (True, False) if FULL or sentinel else (True,)


def full_witness(*args, **kwargs):
    # The exhaustive inventory must not describe a representative execution.
    if FULL:
        print(*args, **kwargs)


def budget_cuts(work):
    if work <= 1:
        raise ValueError("Budget witness needs a successful workload above one")
    return tuple(range(1, work)) if FULL else tuple(sorted({1, work // 2, work - 1}))
