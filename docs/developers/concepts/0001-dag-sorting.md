## Directed Acyclic Graph Sorting


Loadouts and plugins in a mod manager can be seen as a Directed Acyclic Graph (DAG), where the nodes are the items being sorted, and the dependencies are the edges of the graph. Based on current mod setups we have a fairly good idea of the constraints required for such a graph, meaning all we need do is identify these requirements and build an efficient sorting system


## Requirements

Most loadouts will consist of a few dozen mods on the low end to 1500 or 2000 on the high end. We can likely assume that the absolute upper max for any modlist is 10,000 items.

Loadouts also tend to apply zero or more rules to a mod/plugin:

* First - a given mod should be at the start of the list
* Before - a given mod should come before another mod
* After - a given mod should come after another mod

There are other rules that may be required but these are most common. In addition we wish to have 
mods be sorted with a stable sort, thus at times we may add additional constraints between every 
mod in a list. This is to ensure that the order of mods is consistent between runs of the sort.

## Simplification
A key observation in this sorting algorithm is that the fewer rules that exist inside the inner loop
of the sort, the more opportunities there will be for optimization. So we can simplify the problem
by reducing all the rules down to a single rule type: `After`. All other rules can be inverted and
transformed to fit this single rule:

* `First` on `A` - Applies an `After` to every mod except `A`
* `Before B` on `A` - Applies an `After A` to mod `B`

With this applied our data model becomes quite simple: we now have mappings of `NodeID -> set{AfterID1, AfterID2}`

## Algorithm
The resulting sort algorithm then is quite simple:

```
remaining = Simplify(nodes)
used = {}
sorted = []

while remaining:
    nodesToAdd = remaining.Where(n => n.AfterIds ⊆ used)
    for node in nodesToAdd:
        sorted.append(node)
        used.append(node.Id)
        remaining.remove(node.Id)
```

## Performance
For testing this setup I used a list of 10026 items. 26 items were labeled as "First" and had a "After" pointing to the previous mod. 
The other 10000 items had a "After" on the item before it. This should result in roughly 27 rules on most of the 10000 items which should 
emulate a fairly complex loadorder. 

Running on a single core this sort took 5 seconds to complete. Adding `AsParallel` to the `Simplify` algorith, resulted in a 2.1 sec sort time. 
Making the inner loop parallel (searching for the next nodes to add) had a negative performance impact. With proper indexing of the nodes this sort 
time may be improved further, but it's unlikely to be required. 

A more likely setup of 1026 mods had a sort time of 20ms, which is certainly more than fast enough.

## Test System
7950x @ 5.1ghz, 32GB 6000Mhz DDR5
