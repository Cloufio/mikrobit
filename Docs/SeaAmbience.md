# Sea Ambience

`SeaAmbienceController` is created by `GameManager` when a scene contains the `WaterDetail` tilemap. It does nothing in scenes that do not contain that tilemap.

The controller uses the existing animated `WaterDetail` sprites as water-safe positions and creates only decorative sprite renderers. They have no collider, health, `Tool`, or trash scripts.

- **Surface details:** sparse, gently drifting animated water patches at sorting order `1`.
- **Shoreline foam:** brighter patches chosen from water-detail cells beside another tilemap, at sorting order `2`.
- **Water glints:** tiny cyan highlights grouped in open water, at sorting order `1`.

Passive water ambience uses sorting order `1`; trash and crashable objects use order `2`; the boat is set to order `4` by `BoatController`. That keeps passive water under the boat while damage and cleanup effects may still deliberately render on top.

To make the sea denser or calmer later, adjust `SurfacePatchCount`, `ShorelineFoamCount`, and `GlintClusterCount` near the top of `Assets/Scripts/SeaAmbienceController.cs`.
