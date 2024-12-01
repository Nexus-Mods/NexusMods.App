# Updating Mods

!!! info "This is a design document detailing the steps taken to update mods."

A corresponding research document (original design) can be found on a [separate page][research-doc].

## General Approach

!!! tip "First read the [Problem Statement] in the [Research Document]"

The requested approach (from business) has been to maximize the use of the V2 API,
as opposed to programming against the legacy V1 API.

To achieve this, we will [NOT use the `file_updates` array from V1 API's Querying Mod Files][querying-mod-files];
instead choosing to opt to wait until backend decides their future plans with
respect to 'Mods 2.0' project, and how mod updates will be handled in V2 API in the future.

For now, we will:

- [1. Determine Updated Mod Pages], to update our local cache.
- [2. Multi Query Pages], for update mod pages with a 'cache miss'.

## Displaying Mod Updates

!!! info "We display all files on a given mod page that are more recent (file upload time) than the user's file."

Although uncommon this may include:

- Files for other mods on same mod page.
- Older files (if uploaded out of order).

We will for now rely on *users' common sense* to identify whether a file is an 
update to a previous file or not. Until site decides on future plans.

[Problem Statement]: ../../misc/research/00-update-implementation-research.md#problem-statement
[1. Determine Updated Mod Pages]: ../../misc/research/00-update-implementation-research.md#1-determine-updated-mod-pages
[2. Multi Query Pages]: ../../misc/research/00-update-implementation-research.md#multi-query-pages
[querying-mod-files]: ../../misc/research/00-update-implementation-research.md#2-querying-mod-files
[Research Document]: ../../misc/research/00-update-implementation-research.md
[research-doc]: ../../misc/research/00-update-implementation-research.md
