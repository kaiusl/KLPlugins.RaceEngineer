# BUGS

- [ ] BUG: Restarting session doesn't create new event but should as tyresets are reset.
    - Note: In multiplayer after race finished and session restarts to qualy it should remain the same event as the tyresets are not reset (Does this depend on session setup? Probably...)

## *DONE!*

- [x] BUG: When quering input pressure data we have race in db. A new insert could be started while query in under way. 
    - FIXED: Added mutex to db.
- [x] POSSIBLE BUG: Data is not reset after the event, but is it properly set at the start of event? CHECK!!!
    - FIXED: Added reset at end of game. 