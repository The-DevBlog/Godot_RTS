# Bugs
- The upgrade icon on the ugprade button is a tad too large
- I cant get the 'energy' label txt to turn red when consumed energy > energy
- Scaling issues at 2560x1440 and up

# Tasks
- Rebaking the navigation region at runtime is currently very slow. This is because I am parsing through the entire scene tree. The larger my map is, the longer the bake takes. Check out this link: https://www.reddit.com/r/godot/comments/17x3qvx/baking_navmesh_regions_at_runtime_best_practices/
- Make trello board
- Build models
- Animate garage fan
- Animate garage door
- Garage to build tanks
- Placeholder structure should not be able to place where units are
- Use enums to reference any input mappings
- Having all of the unit selection logic in MouseManager.cs might not be a great idea, as it doesnt seem to scale well. Maybe change this?
- RootContainer.cs holds a lot of logic for multiple things. Maybe divy it out?

