# CustomGroupInjector

CustomGroupInjector is an add-on to Randomizer 4 which replicates the Split Group Randomizer feature with more customization options. This document will assume familiarity with the Split Group Randomizer, which is documented here: https://github.com/homothetyhk/RandomizerMod/blob/master/README.md#split-group-randomizer-settings

# Examples

This mod should have been distributed along with an Examples folder. To use the examples, simply move the contents of the Examples folder up one level (in other words, the folders inside Examples should be moved so they are next to the mod dll).

# Usage

- CustomGroupInjector partitions user input into packs, which each receive a submenu with various features. These include:
  - Randomize On Start: if toggled, the groups of the pack will be randomized uniformly into the Split Groups 0, 1, or 2.
  - Reset: if clicked, the groups of the pack will be reset to -1, and Randomize On Start will be deactivated.
  - Entry fields for each group, which allow entering a number between -1 and 99. This works identically to the settings for Split Group Randomizer. 
  - **CustomGroupInjector also takes into account the base Split Group Settings**. Be sure to deactivate (set to -1) base split groups which you do not intend to use.
- To add a Custom Group Pack, create a directory next to CustomGroupInjector.dll, and create the "pack.json" file within that directory. This file provides basic information about the pack and is used to construct its submenu. The "pack.json" file should be a json object with the following properties:
  - Name: this is the text displayed in the menu. This should be unique (compared to any other custom group packs in use) as it will be used to identify the toggle setting.
  - Files: this should be a json array with an element for each other file associated to the pack (more detail below). Each element should be a json object with the following properties:
    - FileName: the name of the file, including extension.
    - JsonType: the format and type of the file.
  - GroupNames: these are the names used for groups in the other files, and which will be used by CustomGroupInjector to create corresponding menu settings (similar to the group-specific settings for Split Group Randomizer). These should also be unique compared to any other group names used by other custom group packs.

## JsonType

- CustomGroupInjector currently supports the following values for JsonType:
  - "LocationCounts"
    - This should be a json file structured as a Dictionary<string, List<string>>. The keys of the dictionary should be group names. The contents of the inner collections should be location names, corresponding to weighted members of the group.
  - "LocationWeights"
    - This should be a json file structured as a Dictionary<string, Dictionary<string, double>>. The keys of the outer dictionary should be location names. The keys of the inner dictionaries should be group names. The values of the inner dictionary should be the relative weights of the location for each group.
      - An entry is only needed for each group where the weight is nonzero. In other words, a location in only one group only needs a single entry, where the weight can be any positive number.
  - There are also "ItemCounts" and "ItemWeights" formats which are identical, except that they are interpreted for items rather than for locations.
 - Multiple files with any combination of the formats are accepted. When there are files of the "Counts" and "Weights" formats, the combined effect is that the count of a location in one file is added directly to its combined weight.
- In summary, a typical pack has:
  - A pack.json file, with the information about the pack.
  - A items.json file, with the item groups in the user's preferred format.
  - A locations.json file, with the location groups in the user's preferred format.
 