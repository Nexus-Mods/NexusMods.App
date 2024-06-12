# Release Process and Workflow

## Overview
The Nexus Mods App follows a strict release process which assumes that the
`main` branch is the development branch and will always be kept in a releasable
state. This means that any major overhauls of feature development should either
be done in a single PR or be developed behind an optional setting or feature flag.

At a high level, releases are done by branching from `main`, and then building an app release
from this branch. A tag is assigned at the moment of the release. Hotfixes to a release
will only be done for specific issues that cause major regressions, and these hotfixes will
be applied to the `main` branch and backported into the release branch (or vice versa). Hotfix
releases are branched from the base release. Once it is determined that no hotfixes
will be applied to the release branch, the release branch is deleted. Often, this will be done
sometime after the next release is made.

## Release Schedule
The Nexus Mods App developer team follows a 3-week iteration cycle, so releases are expected
to be made at the same interval. Most often, releases will be cut by the team lead, [halgari](https://github.com/halgari),
who lives in the MST timezone. The cutoff point for a release is sometime in the early morning on Wednesday
of the release week (MST). Due to the requirement that main kept in an "always releasable" state, 
the exact time of the creation of the release branch will not be further defined. 

## Hotfix Criteria
The ultimate goal of the team is to never release a hotfix for a release. However there are times 
where a regression is discovered at the last moment, or some bug slips in as part of a late PR. These
situations should be considered the exception not the norm, and common hotfixes should be viewed as 
a failure of process and addressed accordingly. 

Hotfixes should be restricted to the following criteria:

* Data loss - something in the release will cause the user to lose their data or work in an area of the 
app that was previously marked as "supported"
* System instability - something in the app causes crashes in a part of the app outside of the app's influence
* Inability to upgrade - something in the app causes the app to get into a state where future updates will
not be installable and the user would be required to wipe their data and start over. 

The common theme for all of these is situations where the user experience is getting worse as time goes on, 
and waiting till the next release cycle creates an ever compounding problem for users and the dev team. These criteria
are just guidelines, however, and the app team managers reserve the right to make case-by-case decisions. 

## Hotfix development process
* Bring up an issue with the dev team
* If a decision is made that the issue needs to be hotfixed, the github issue will be marked as `needs-backporting`
* Create a PR against either `main` or the release branch with the fix. 
* Create a second PR against the other branch with the same fix (feel free to wait until the first PR is merged).
* Once *both* PRs are merged the initial issue can be closed. 

## Technical Process for a Release

1. Make sure the release changelog is committed. Often the changelog includes images that 
reference a tag or commit, so merge the commit PR first, then continue the process. [erri120](https://github.com/erri120) is
the current changelog maintainer. 
2. Create a release branch from main named `release_vX.Y`
3. Tag the most recent commit of the release branch
4. Execute the `Release` github action, and be sure to pass in the correct tag and branch names. 
5. Once the release has started building, it will create a new release entry on GitHub. Edit the release
and copy in the correct changelog information from `CHANGELOG.md`
6. Once the release has been built, verify that it has uploaded the artifacts correctly, and set the release as the new
current version on GitHub

## Technical Process for a hotfix release
It is exactly the same as the process for a main release, but the base for all branching, builds, and tags
is the previous most recent release. 
