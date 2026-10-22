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

## Standalone direction (updated 2026-10-22)

The product requirement is now explicit: **PowerPoint must work without OBS Studio**. The earlier OBS Virtual Camera idea is background feasibility evidence only; it is not a deployment dependency or the selected product approach.

0.0.003 reuses only the independent Wi-Fi pairing logic and keeps the external scrcpy overlay. A future native-rendering proof of concept must supply a Windows camera source directly from Android/scrcpy (or another independent Windows rendering component) and then test Cameo on the target PowerPoint build. It must not require the OBS process, plugin or virtual camera.

That separate project must validate camera-source registration, supported PowerPoint automation, formatting/transitions, Presenter View, mixed DPI, rotation, latency, permissions, prolonged use and recording/export behavior. A live camera object in a PPTX does not itself store a permanent recording. No custom camera driver or native slide-frame injection is implemented in 0.0.003.

## Primary sources

- [Microsoft: Shapes.AddMediaObject2](https://learn.microsoft.com/en-us/office/vba/api/powerpoint.shapes.addmediaobject2) — file-based linked/embedded media API.
- [Microsoft: Presenting with Cameo](https://support.microsoft.com/en-us/powerpoint/presenting-with-cameo) — native camera feed object and camera selection.
- [OBS: Virtual Camera Guide](https://obsproject.com/kb/virtual-camera-guide) — scene/source output as a webcam.
- [scrcpy: Video4Linux](https://github.com/Genymobile/scrcpy/blob/master/doc/v4l2.md) — Linux loopback camera output.
- [scrcpy: Window documentation](https://github.com/Genymobile/scrcpy/blob/master/doc/window.md) — title, borderless mode and window geometry.
