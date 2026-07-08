# System Feature Gap Matrix

| System Group | Code (Logic) | Data (SO) | Scene (Mono) | UI | Save/Log | Validator | Gap Notes |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Activities** | Yes | Yes | Yes | No | Yes | Yes | Missing UI panels for most activities. |
| **Battle** | Yes | Yes | Yes | Yes | No | Yes | Missing Save/Resume capability for mid-battle. |
| **Shops / Services** | Yes | Yes | Yes | Partial | Yes | Yes | UI exists but needs integration to Scene logic. |
| **Pokemon Core** | Yes | Yes | Yes | Partial | No | Yes | Missing Party Save/Load specific logic or UI integration. |
| **Social / Dialogues**| Yes | Yes | Yes | Partial | Yes | Yes | Needs robust UI for speech bubbles and menus. |
| **World & Environment**| Yes | Yes | Yes | No | Yes | Yes | No map UI or environment feedback UI. |
| **LifePaths & Roles** | Yes | Yes | Yes | Yes | Yes | Yes | Validator needs polish (as per GDP). UI exists via LifePathUIManager. |
| **Competitions** | Yes | Yes | Yes | No | Yes | Yes | Missing registration/bracket UIs. |

## Legend
- **Code:** Base logic, managers, and system definitions exist.
- **Data (SO):** ScriptableObject definition classes exist. (Note: actual asset instances may be low).
- **Scene (Mono):** MonoBehaviour classes exist to be attached to GameObjects.
- **UI:** UI manager scripts or UI views exist.
- **Save/Log:** Player log or save data scripts exist.
- **Validator:** Content audit or validator scripts cover this system.

## Immediate UI Gaps
- Activities and Competitions have no dedicated UI components implemented yet.
- World/Environment lacks visual feedback/UI integration.
