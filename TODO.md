# Bugs
- Shaders are applied above the unit select border. The select border should not be obstructed by anything besides UI
- Placeholder structure should not be able to place in invalid placements

# Tasks
- Use enums to reference any input mappings
- Having all of the unit selection logic in MouseManager.cs might not be a great idea, as it doesnt seem to scale well. Maybe change this?