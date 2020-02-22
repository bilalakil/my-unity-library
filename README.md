# my-unity-library

A collection of generic helpers that I'll be taking around with me to all my Unity projects.

They are currently undocumented, however the code itself isn't too bad.

## Unity Cheatsheet

### Quick Reference

- `logcat`: `$ANDROID_HOME/platform-tools/adb logcat -s Unity`

### Mobile Performance

- Bake and fake (blob shadows, light probes). [[2][2]]
- Use texture atlases. [[1][1]]
- Use shared materials, esp. with texture atlases. [[1][1]]
- Use mobile shaders. [[1][1]]
- Set game objects to static when possible. [[1][1]]
- Custom solutions, i.e. custom collision for projectiles. [[2][2]]

#### Minimise:

- Bumpmaps. [[2][2]]
    - _Are bumpmaps normalmaps?_
- Unity getter usage (`FindObjectsOfType`, etc.). [[1][1]]
- Pixel lights (pretty much just 1 for directional). [[1][1]]
- Transparency (incl. glow, fog, etc.), especially cutout. If necessary, go for alpha blended. [[1][1], [2][2]]
    - _What about UI and particles?_
- Rigidbodies. [[1][1]]
- Mesh colliders. [[1][1]]

#### Don't:

- Use `OnGUI()`. [[3][3]]
    - _Do Unity's UI elements have this issue?_
- Use post-processing, unless exceptionally necessary. [[1][1]]
- Move a static collider (a collider without a rigidbody). [[1][1]]

##### Other

- [Enabling the internal profiler for iOS/Android](https://docs.unity3d.com/Manual/iphone-InternalProfiler.html)

#### References

1. [Unity - Manual: Optimizations][1]
2. [Unity - Manual: Graphics Methods][2]
3. [Unity - Manual: Profiling][3]

[1]: https://docs.unity3d.com/Manual/MobileOptimisation.html
[2]: https://docs.unity3d.com/Manual/MobileOptimizationGraphicsMethods.html
[3]: https://docs.unity3d.com/Manual/MobileProfiling.html
