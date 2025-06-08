# Bugs
- Shaders are applied above the unit select border. The select border should not be obstructed by anything besides UI
- Placeholder structure should not be able to place in invalid placements
- Rebaking the navigation region at runtime is currently very slow. This is because I am parsing through the entire scene tree. Check out this link: https://www.reddit.com/r/godot/comments/17x3qvx/baking_navmesh_regions_at_runtime_best_practices/
- The upgrade icon on the ugprade button is a tad too large

# Tasks
- Use enums to reference any input mappings
- Having all of the unit selection logic in MouseManager.cs might not be a great idea, as it doesnt seem to scale well. Maybe change this?