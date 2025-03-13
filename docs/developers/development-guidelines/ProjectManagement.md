## Project Management

This document serves as a way of recording the current process of managing the Nexus Mods app during development. Issue tracking and planning is done in Github but there are 
a few nuances to this process that deserve some explination

### Versioning
While [Semantic Versioning](https://semver.org/) works well enough, the exact definition of the process is harder to implement than one would originally assume. Questions over "breaking changes" 
and "bugfix releases" are somewhat pointless when coupled with a steady release cycle and a commitment to data migration. So this project adopts the following versioning structure:

Version numbers will follow the format of `{MajorRelease}.{Sprint}.{Build}`. 

#### Major Release
For the most part the `MajorRelease` portion will never change. Currently the `MajorRelease` is `0`, and will remain 
so until the project is out of beta at which point it will become `1`. A major release of `2` will only happen after a full rewrite or some other world-altering change. 

#### Sprint
The `Sprint` value goes up by 1 every 3 weeks in sync with the core team's sprint scedule. As of the time of this writing that number is `0.9`. The next sprint will be `0.10` and then `0.11`, etc. 
Whenever the app exits beta, a decision will be made if the sprint counter resets or stays the same. 

#### Build
At the end of each sprint a build is made and handed off to the QA team for testing. Bugs that are found in QA are triaged by impact and either scheduled into a future sprint or backported to the
branch being QA'd. When these fixes are ready a new version will be created with thes fixes and that build increments the build number. The build number resets each sprint.

#### Examples
So a full release list may look something like this:

* 0.8.0 - Build 1 of sprint 8
* 0.8.1 - Build 2 of sprint 8, with fixes to bugs found in QA
* 0.8.2 - Build 3 of sprint 8, with more fixes found
* 0.9.0 - Build 1 of sprint 9
* 0.9.1 - Build 2 of sprint 9, with fixes to bugs found in QA
* 0.10.0 - Build 1 of sprint 10

### Issue/Milestone/Sprint tracking
Issues are found in [Github](https://github.com/Nexus-Mods/NexusMods.App/issues). These issues are maintained by the project's team lead and project manager and added to sprints. Sprints last 3
weeks. Whenever the end of a sprint arrives and there are remaining issues, the issues are either bumped to the next sprint or resceduled for a future sprint.

Milestones are not used in a traditional way by the project, as we prefer to track items in sprints, what people commonly think of as "Epics" are grouped into Github's Milestones but not given a 
due date. The idea with these Milestones is that they group issues together to give a quick overview of how far a major collection of work is from completion. For
the most part these groups will correspond to game support or major features. So there may be a project for "Cyberpunk Support" or "Collection Creation" or "MacOS support". These milestones may 
span several sprints and may be shelved and worked on over several months. Again, they do not have a due date.

The main takeaway from this process is that nothing in the management system has a movable due date. Sprints happen every 3 weeks like clockwork. Milestones don't have due dates, instead
they are done when the items in them are done. A high level view of the work a team is doing is best seen via a report on the sprint. This gives an overview of issues completed, planned, etc.

### Game Support "Milestones"
One of the most common types of milestones is game support. Games may have multiple milestones associated with them, but the first is the "basic support" milestone. Once a game reaches this
"basic" level of integration it can be set to `Green` in the app's discord and is considered ready for general use. The basic acceptance critera for milestones of this type is:

* All the most common mods for the game are installable
* The game can be launched with these mods installed
* General diagnostics for the most common errors are implemented
* The most common collections for the game are installable
* Any sort of common sorting/conflict resolution should be handled

An astute reader will notice that these acceptance critera are very vauge and undefined. This is by design, the team lead and project manager are free to decide what mods are considered "common"
or not. If we were to define a popularity level or install rate for collections/mods, there will always be outliers. So instead these definitions are left up to management. 

Once basic game support is added future milestones may be created for the game to add in additional support for more diagnostics, tools, visualizers, etc. 
