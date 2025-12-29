# 🧟‍♂️ VR Escape Prototype  
### _Physics. Parkour. Panic. Built for Quest 3 in Unity 6._

A physics-driven VR escape prototype focused on **weight, instability, and bad decisions**.  
This is not a power fantasy. This is a “don’t drop your gun while climbing” fantasy.

The goal is simple: **escape**.

---

## 🎯 Game Concept

A VR escape game built around:

- Physical gun handling with real weight  
- Parkour and climbing using physics forces  
- Minimal assistance, no sticky hands  
- Tension over comfort  
- Enemies that pressure you to move, not fight  

You start in a tight alley with a rifle on your back and a pistol on your hip.  
You climb, scramble, lose balance, drop things, panic, recover, and escape… or you don’t.

---

## 🔧 Core Design Pillars

- **Physics first**  
  Weight, torque, leverage, and gravity matter.

- **Unforgiving interaction**  
  If you place an object badly, it falls.  
  If you hold a rifle with one hand, it sinks.

- **Movement through environment**  
  You escape by climbing, pulling, vaulting, and improvising.

- **Pressure, not power**  
  Enemies exist to rush you forward, not to be farmed.

---

## 📦 Current State of the Project

### ✅ Done
- [x] Unity 6 project setup with build scenes (MainMenu/Game)  
- [x] XR packages installed (OpenXR, XR Interaction Toolkit, XR Hands, Input System, URP)  
- [x] XR Plug-in Management configured with OpenXR loader  
- [x] XR input actions asset with XR bindings  
- [x] Custom rig prefab with hand drivers, bone curl, and locomotion in the Game scene  
- [x] Physics grab system with grab points on weapon prefabs  
- [x] Main menu scene with XR UI laser pointer + menu manager scripts  

### 🚧 In Progress
**Legend:** ⏳ = active ⛔ = blocked 🧪 = working but unstable

#### 🧠 Design / Planning
_No active design docs tracked yet._

#### 🔧 Implementation
- [ ] ⏳ Floor calibration flow wired into the Game scene  
  → Done when: TapFloorCalibrator is in-scene and updates the gameplay floor reliably

#### 🧪 First-pass Integration
_No integration passes tracked yet._

### ❌ Not Implemented Yet
#### 🧱 Core Gameplay Systems (Not Started)
- [ ] Weapon firing/reload/holsters  
- [ ] Climbing/parkour movement  
- [ ] Enemy AI/pressure systems  
- [ ] Escape loop / fail-retry loop  

#### 🧰 Supporting Systems (Later)
- [ ] Level blockout beyond the test plane  
- [ ] Save/load pipeline  
- [ ] Performance profiling pass  
- [ ] Quest/Android build validation  

▶️ **Next Focus**
- Wire the floor calibration flow into the Game scene
- Run a Quest build with current OpenXR settings to validate tracking + input
- Define the first weapon interaction milestone (pickup → aim → fire)

### 🧾 Audit Notes
- (DONE) XR packages installed: `Packages/manifest.json` includes OpenXR/XR Interaction Toolkit/XR Hands/Input System.
- (DONE) OpenXR loader configured: `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` and `ProjectSettings/EditorBuildSettings.asset`.
- (DONE) XR input actions asset present with XR bindings: `Assets/InputSystem_Actions.inputactions`.
- (DONE) Game scene uses rig prefab: `Assets/Scenes/Game.unity` references `Assets/Prefabs/Rig.prefab`.
- (DONE) Rig prefab includes locomotion + hand drivers/bone curl: `Assets/Prefabs/Rig.prefab`, `Assets/Scripts/Locomotion.cs`, `Assets/Scripts/HandDriver.cs`, `Assets/Scripts/HandBoneDriver.cs`.
- (DONE) Physics grab system + grab points on weapons: `Assets/Scripts/HandGrabPhysics.cs`, `Assets/Scripts/GrabPoint.cs`, `Assets/Prefabs/Weapons/Rifle.prefab`.
- (DONE) Main menu XR UI scripts wired: `Assets/Scenes/MainMenu.unity` uses `Assets/Scripts/Menus/LaserPointer.cs` and `Assets/Scripts/Menus/MenuManager.cs`.
- (DONE) Floor calibration script is wired into MainMenu; a button triggers `BeginCalibration` in `Assets/Scripts/FloorCalculation/TapFloorCalibrator.cs`.

This is intentionally early. The foundation matters more than rushing features.

---

## 🔫 Planned Core Features

### Weapon Physics
- Rifle that becomes unstable when held with one hand  
- Pistol holstered on the hip  
- No artificial weapon stabilization  
- Weapons can slide or fall if placed poorly  

### Parkour & Climbing
- Pulling yourself up using ledges, boxes, or props  
- Climbing driven by actual forces, not snapping  
- Slipping, overreaching, and bad grip are possible  

### Enemies
- Zombies or infected creatures  
- They apply pressure  
- Standing still is the wrong choice  

### Inventory / Holsters
- Physical rifle back holster  
- Hip pistol holster  
- Possible backpack system later  

---

## 🗺️ Roadmap

### Short Term
- Unity 6 project initialized  
- XR Interaction Toolkit setup (Quest 3)  
- Basic XR rig and hands  
- Grabbing and releasing objects  
- First weapon weight prototype  

### Mid Term
- Climbing prototype using physics  
- Alley test environment  
- Basic enemy pressure logic  
- Failure and retry loop  

### Long Term
- Polished escape level  
- Refined physics tuning  
- Audio, tension, pacing  
- Performance optimization  
- Internal screaming  

---

## 🛠️ Tech Stack

- Unity 6  
- Meta Quest 3  
- XR Interaction Toolkit  
- Unity Input System  
- Physics-driven interactions  

---

## 🌟 Why This Project Exists

Because VR is at its best when it feels **physical**, **messy**, and **dangerous**.  
Because climbing while holding a rifle should be a terrible idea.  
And because escaping by improvisation is more interesting than standing still and shooting waves.

This project is about **weight, panic, and motion**, not comfort.
