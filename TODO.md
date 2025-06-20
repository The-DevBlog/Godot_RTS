# Bugs
- Rebaking the navigation region at runtime is currently very slow. This is because I am parsing through the entire scene tree. Check out this link: https://www.reddit.com/r/godot/comments/17x3qvx/baking_navmesh_regions_at_runtime_best_practices/
- The upgrade icon on the ugprade button is a tad too large
- I cant get the 'energy' label txt to turn red when consumed energy > energy

# Tasks
- Make trello board
- Build models
- Animate garage fan
- Animate garage door
- Garage to build tanks
- Placeholder structure should not be able to place in invalid placements
- Use enums to reference any input mappings
- Having all of the unit selection logic in MouseManager.cs might not be a great idea, as it doesnt seem to scale well. Maybe change this?
- RootContainer.cs holds a lot of logic for multiple things. Maybe divy it out?
- Find a way to make all things that depend on the map size to be dyanmic. For example, the camera needs the mapsize for camera bounds. The mini map also needs to the mapsize. 

