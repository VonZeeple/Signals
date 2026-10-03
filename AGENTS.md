
# AGENTS

## Useful references

- JSON patching for Vintage Story: https://wiki.vintagestory.at/Modding:JSON_Patching
- Remapper documentation: https://wiki.vintagestory.at/Modding%3AThe_Remapper/en

## Debugging workflow

When looking for errors, check both build-time and runtime issues:

- Inspect compiler and editor diagnostics first to catch broken code or stale warnings.
- Also review runtime errors from the game/server logs, especially JSON patch failures or mod-load exceptions.
- Use the exact exception, file path, and operation details from the log to trace the mismatch before changing code.