# Meeting Summary on Default Storage Locations

**Date of Meeting**: 26th of November, 2023

**Attendees**: App Team Only (No Vortex/Design)

**Agenda**:

1. Final decision on default storage location for app files.  
2. Discussion on Portable Mode implementation.  
3. Decision on handling separate per user and per machine data.  
4. Additional context and future considerations.  

## Decisions Made

### Default Storage Location

**Agreed upon LocalAppData**. The team unanimously agreed to use the `LocalAppData` on Windows 
and `XDG_DATA_HOME` on Linux. This is Option 2 from the ADR, however with only the use of LocalAppData. 

We chose to pretend (for now) that Multi-User systems aren't within our scope as they are very 
infrequently used.

This keeps our DataModel simpler, without the need to track file source locations (where to 
download them from) or splitting the DataModel itself into two.

### Portable Mode

**Support for Portable Mode Confirmed**. Team chose to vote in favour of supporting a portable 
mode based on past experience with end users and and their mod managers.

@erri120 made an note that one of the fact that part of the main appeal of a 'portable mode' is 
being able to transfer the data between one machine to another. So technically, that can already be 
fulfilled using built-in export/import functionality. 

In any case, as not much work is currently required to deliver this (it already is portable, in fact!), 
the team decided to support it.

There are some concerns, however, such as the user potentially managing game with multiple 
managers, leading to an inconsistent state. For now, we expect the user to be responsible for
not making this mistake.

### Future Plans

**Auto Installs to Multiple Locations**: This feature will be addressed in the future. The goal 
is to allow users to specify storage locations on a per-game basis.

## Next Steps and Action Items

- **Implementing LocalAppData as the Default Storage**: Transition the app's default storage location 
  to `LocalAppData` / `XDG_DATA_HOME` as per the decision.

- **Developing Portable Mode**: Basically make the current config file we have 'optional', if it 
  exists, use it, otherwise default to a 'default' storage location.