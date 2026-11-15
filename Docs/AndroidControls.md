# Android touch controls

Gameplay uses the existing PlayerRig, UseFocus, backpack, tablet and decoration actions.

- Bottom left: analog movement stick with dead zone.
- Right open area: drag to look. Two camera fingers pinch FOV smoothly between20 and90 degrees. Joystick/button fingers never participate in camera or pinch gestures.
- Top right: backpack, base tablet/Shop and pause icons.
- Bottom right: contextual interaction/tool action, jump, crouch and lamp. Store/release appear when holding an item.
- Decoration mode: confirm (valid surface only), rotate15 degrees and cancel. Aim at installed decor and use the hand button to move it; Store remains available in Shop.
- Tablet/menu controls accept direct touch: buttons and inventory cells, sensitivity/FOV sliders, field-guide/log/placed-item scrolling. A drag cannot buy an item or activate a button.
- Opening full-screen menus releases gameplay fingers and stops movement/look. Fingers already held when closing a menu must be lifted before controlling the player again. Canceled touches and screen/safe-area changes clear gesture ownership.

The HUD is an anchored safe-area Canvas with CanvasScaler (Expand,1440x800 reference). Action hit areas are96–112 reference units; original icons and antialiased translucent circular graphics are generated once at runtime. Base resources remain in the tablet. Keyboard hints are replaced only on mobile. Touch sensitivity is saved separately from desktop sensitivity.

Windows controls, shipped Windows executable, simulation, missions, sky and world assets are unchanged. Source menu hooks return the existing desktop behavior outside mobile or automated test mode.

Verification uses a separate PC regression build with an Input System synthetic multi-touch device and the existing runtime suite. This is not equivalent to testing an APK on physical Android hardware. No device was connected when checked.

Changed runtime files:
- Assets/LunarRelay/MobileControls.cs (+Unity .meta): safe-area Canvas, original icons, gesture ownership, FOV, menu touch bridge, slider/scroll helpers.
- Assets/LunarRelay/PlayerRig.cs: mobile input integration; original Windows branch preserved.
- Assets/LunarRelay/GameSession.cs: one mobile input update per frame; isolated touch-test entry point.
- Assets/LunarRelay/Interface.cs and InterfaceTheme.cs: mobile text/button/slider routing.
- Assets/LunarRelay/TabletUI.cs: touch inventory-cell selection.
- Assets/LunarRelay/BaseStatusUI.cs and TaskCredits.cs: mobile action hint, aim reticle and unobstructed credit labels.
- Assets/LunarRelay/DecorShop.cs: touch decoration commands and scrolling; excludes emulated mouse clicks on mobile.
- Assets/LunarRelay/EngineeringUI.cs and MissionPersistence.cs: mobile guide/log scrolling and keyboard-hint substitution.

Validation support: Assets/LunarRelay/SmokeTests.cs and Assets/Editor/BuildTools.cs.

Final regression:229 assertions passed (193 existing gameplay checks +36 touch/control checks),0 failed. Seven native IMGUI activation/slider/scroll checks were skipped because the hidden/batch player never repaints IMGUI. Their code is retained for a rendered native run. Direct menu-touch capture and cancellation were tested; actual touchscreen clicks in shop/inventory and device responsiveness remain to be checked on Android hardware. The Canvas-only HUD preview was rendered and inspected at1600x900.

Android artifact: Deliverables/LunarRelay-Android-Touch.apk (ARM64, Android8.0+,42.8MB). Final Android build, APK v2 signature and ZIP CRC checks passed. No physical device installation/play test was possible.
