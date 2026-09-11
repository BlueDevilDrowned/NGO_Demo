# PlayableGraph Visualizer

The PlayableGraph Visualizer is a tool that displays the PlayableGraphs in the scene.
It can be used in both Play and Edit mode and will always reflect the current state of the graph.
Playable nodes are represented by colored nodes, varying according to their type. Connections color intensity indicates its weight.

This embedded fork is compatible with Unity 6 (tested with Unity 6000.4). It keeps the original
IMGUI window and runtime `GraphVisualizerClient` API while fixing Unity 6 compilation and layout
edge cases.

See the [Documentation](Documentation~/playablegraph-visualizer.md) for the installation and the usage.
