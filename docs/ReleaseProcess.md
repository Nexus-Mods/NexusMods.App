# Release Process and Workflow

## Overview
The Nexus Mods App follows a strict release process which assumes that the
`main` branch is the development branch and will always be kept in a releasable
state. This means that any major overhauls of feature development should either
be done in a single PR or in a be developed behind a optional setting or feature flag. 

At a high level, releases are done by branching from main, and then building a app release
from this branch. A tag is assigned at the moment of the release. Hotfixes to a release
will only be done for specific issues that cause major regressions, and these hotfixes will
be applied to the main branch and backported into the release branch (or vice versa). Hotfix
releases are branched from the base release. Once it is determined that no hotfixes
will be applied to the release branch, the release branch is deleted. Often this will be done
sometime after the next release is made.

## Release Schedule
The Nexus Mods App developer team follows 3 week iteration cycle, so releases are expected
to be made at the same interval. Most often releases will be cut by the team lead (halgari)
who lives in MST timezone. The cutoff point for a release is sometime early morning on Wednesday
of the release week (MST). Due to the requirement that main kept in a "always releasable" state, 
the exact time of the creation of the release branch will not be further defined. 

## Technical Process for a release

1. Make sure the release changelog is committed. Often the changelog includes images that 
reference a tag or commit, so merge the commit PR first, then continue the process. Erri120 is
the current changelog maintainer. 
2. Create a release branch from main named `release_vX.Y`
3. Tag the most recent commit of the release branch
4. Execute the `Release` github action, and be sure to pass in the correct tag and branch names. 
5. Once the release has started building, it will create a new release entry on GitHub. Edit the release
and copy in the correct changelog information from `CHANGELOG.md`
6. Once the release has built, verify that it has uploaded the artifacts correctly and set the release as the new
current version on GitHub

## Technical Process for a hotfix release
Exactly the same as the process for a main release, but the base for all branching, builds and tags
is the previous most recent release. 
