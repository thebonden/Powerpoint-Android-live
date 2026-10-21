# Native slide-contained live video: feasibility investigation

Investigated 2026-10-21. This is a documentation-based assessment, not a completed native-rendering prototype.

## What 0.0.002 does

A normal PowerPoint rectangle records placement. A separate scrcpy HWND is moved over the slide. The HWND is owned by the slideshow to maintain relative stacking, but is **not** parented into it and is not part of the slide rendering pipeline. It cannot become native media by changing window parenting. PowerPoint exports do not capture it.

## Options

| Route | Finding | Decision |
| --- | --- | --- |
| PowerPoint `Shapes.AddMediaObject2` | Creates media from a file and supports saving that file with the document. It has no documented decoded-frame or camera-stream parameter. A completed recording is a supported embedded asset; arbitrary live frames are not provided by this API. | Good for an optional recorded fallback, not the current live requirement. |
| Cameo plus a Windows virtual camera | Microsoft documents live camera objects directly on slides, including formatting and transitions. OBS can expose a scene/source as a virtual webcam. | Most promising native slide-contained live route. Needs a Windows proof of concept and compatible PowerPoint build. |
| ActiveX/OLE custom player | A separate control is a different deployment/compatibility model; it does not establish that PowerPoint will render arbitrary live frames in exports, transitions or Presenter View. | No implementation or compatibility claim. Do not use as a shortcut to “native video.” |
| Repeatedly replace a picture | Updates an image through COM, with no documented realtime-frame scheduling guarantee. | Not selected for realtime presentation rendering. |
| Custom decoder / Windows virtual-camera source | Could expose the Android frames as a camera for Cameo, with additional decoder, synchronization and camera-component work. | Larger future project after proving the Cameo path. |

The absence of a decoded-frame API in the examined PowerPoint media method is **not proof that all native live rendering is impossible**. Cameo is an existing native camera mechanism. The earlier blanket statement has been narrowed accordingly.

## Proposed proof of concept

1. On a Windows test PC with a Cameo-capable PowerPoint build, run ordinary scrcpy independently of this add-in. This add-in hides its window outside slideshow alignment, which is unsuitable as a stable capture source for this experiment.
2. Capture the scrcpy window in OBS; configure the virtual camera to output that source/scene; start the virtual camera.
3. Insert Cameo, select the OBS virtual camera, and compare visual placement with the overlay build.
4. Verify one and two displays, Presenter View, transitions, portrait/landscape rotation, 30-minute operation, disconnect/reconnect and camera-permission prompts.
5. Measure end-to-end latency and CPU/GPU load; verify PowerPoint's recording/export behavior separately. A live camera object in a PPTX does not store a permanent copy of future video. Do not promise exported live video without a recording test.
6. Establish whether the required insertion/device-selection actions have supported automation on the target PowerPoint version; do not claim an available VSTO frame-injection API from UI support alone.

Acceptance evidence should include PowerPoint version/channel, Android and scrcpy versions, camera source, recording/export examples and measurements. No driver or virtual camera is installed by 0.0.002. scrcpy's documented direct V4L2 webcam output is Linux-only, so it is not a ready-made Windows solution.

## Primary sources

- [Microsoft: Shapes.AddMediaObject2](https://learn.microsoft.com/en-us/office/vba/api/powerpoint.shapes.addmediaobject2) — file-based linked/embedded media API.
- [Microsoft: Presenting with Cameo](https://support.microsoft.com/en-us/powerpoint/presenting-with-cameo) — native camera feed object and camera selection.
- [OBS: Virtual Camera Guide](https://obsproject.com/kb/virtual-camera-guide) — scene/source output as a webcam.
- [scrcpy: Video4Linux](https://github.com/Genymobile/scrcpy/blob/master/doc/v4l2.md) — Linux loopback camera output.
- [scrcpy: Window documentation](https://github.com/Genymobile/scrcpy/blob/master/doc/window.md) — title, borderless mode and window geometry.
