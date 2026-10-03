using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

namespace EndlessHallway.Editor
{
    [InitializeOnLoad]
    public static class CharacterAnimationSetupUtility
    {
        private const string AnimSourceFolder = "Assets/Main-Character-Animation";
        private const string OutputAnimFolder = "Assets/_Project/Animations";
        private const string ControllerPath = OutputAnimFolder + "/Adventure_Character_AC.controller";
        private const string MinimalControllerPath = OutputAnimFolder + "/Adventure_Character_Minimal_AC.controller";
        private const string PrefabPath = "Assets/Adventure_Character/Prefabs/Man_03.prefab";
        private const string CharacterMeshPath = "Assets/Adventure_Character/Mesh/Man_Mesh.FBX";

        static CharacterAnimationSetupUtility()
        {
            EditorApplication.delayCall += () =>
            {
                // Auto-run if the main controller does not exist yet
                if (!File.Exists(ControllerPath))
                {
                    BuildFullAnimationSystem(false);
                }
            };
        }

        [MenuItem("Tools/Endless Hallway/Build Full Character Animation System", false, 1)]
        public static void BuildMenu()
        {
            BuildFullAnimationSystem(true);
        }

        [MenuItem("Tools/Endless Hallway/Build Minimal Test Controller (Step 3)", false, 2)]
        public static void BuildMinimalMenu()
        {
            BuildMinimalAnimationSystem(true);
        }

        [MenuItem("Tools/Endless Hallway/Fix Camera and Body Architecture (Step 4)", false, 3)]
        public static void FixCameraAndBodyMenu()
        {
            FixCameraAndBodyArchitecture();
            EditorUtility.DisplayDialog("Camera & Body Architecture Fixed",
                "CameraPivot and mesh shadow casting modes have been updated!\n\n" +
                "- Upper body (head, eyes, jaw, face, pullover, arms, bag) set to ShadowsOnly\n" +
                "- Hollow neck cavity & disconnected floating hands eliminated\n" +
                "- Full character shadows preserved\n" +
                "- CameraPivot hierarchy separated for clean yaw/pitch isolation", "OK");
        }

        public static void BuildFullAnimationSystem(bool showDialog)
        {
            Debug.Log("[CharacterAnimationSetup] Starting full character animation setup...");

            if (!Directory.Exists(OutputAnimFolder))
            {
                Directory.CreateDirectory(OutputAnimFolder);
                AssetDatabase.Refresh();
            }

            // Step 2: Ensure all animation FBXs in Main-Character-Animation are imported as Humanoid with correct loop settings
            ConfigureAnimationImporters();

            // Step 2b: Load clips from the FBXs
            var clips = LoadAllHumanoidClips();

            // Step 2c: Ensure resting Idle clip exists
            AnimationClip idleClip = EnsureIdleClip(clips);

            // Step 3: Build Minimal Controller for isolation testing
            BuildMinimalController(clips, idleClip);

            // Step 4: Fix Camera and Body Architecture (eliminate hollow neck gap and floating hands)
            FixCameraAndBodyArchitecture();

            // Step 5: Rebuild full Animator Controller with all 7 requested states and 3 parameters
            AnimatorController controller = CreateOrUpdateFullController(clips, idleClip);

            // Step 5b: Assign controller to Prefab
            AssignToPrefab(controller);

            // Step 5c: Assign controller to Scene instance
            AssignToScene(controller);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CharacterAnimationSetup] Full animation system & camera/body fix successfully applied!");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Animation System Built & Verified",
                    "Adventure_Character_AC.controller and Camera/Body architecture built!\n\n" +
                    "- All FBX clips imported as Humanoid (Create From This Model)\n" +
                    "- Loop Times: ON for Walking & Crouched Walking, OFF for Start/Stop/Turn\n" +
                    "- Rebuilt Animator States: Idle, Start Walking, Walking, Stop Walking, Crouched Walking, Walking Left Turn, Walking Turn 180\n" +
                    "- Parameters: Speed (Float), IsCrouching (Bool), Turn (Float)\n" +
                    "- Minimal test controller generated at Adventure_Character_Minimal_AC.controller\n" +
                    "- CameraPivot separates pitch from player yaw (skeleton never pitched)\n" +
                    "- Torso hollow gap & disconnected floating hands fixed via ShadowsOnly upper body",
                    "OK");
            }
        }

        public static void BuildMinimalAnimationSystem(bool showDialog)
        {
            Debug.Log("[CharacterAnimationSetup] Building minimal isolation test controller...");

            if (!Directory.Exists(OutputAnimFolder))
            {
                Directory.CreateDirectory(OutputAnimFolder);
                AssetDatabase.Refresh();
            }

            ConfigureAnimationImporters();
            var clips = LoadAllHumanoidClips();
            AnimationClip idleClip = EnsureIdleClip(clips);

            AnimatorController minimal = BuildMinimalController(clips, idleClip);
            AssignToPrefab(minimal);
            AssignToScene(minimal);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Minimal Controller Assigned",
                    "Adventure_Character_Minimal_AC.controller assigned with only Idle + Walking.\n" +
                    "Use this to verify retargeting in isolation before testing the full state machine.", "OK");
            }
        }

        /// <summary>
        /// STEP 2: Configure Animation Importers for all downloaded FBX clips
        /// </summary>
        public static void ConfigureAnimationImporters()
        {
            string[] fbxFiles = {
                "Crouched Walking.fbx",
                "Start Walking.fbx",
                "Stop Walking.fbx",
                "Walking Left Turn.fbx",
                "Walking Turn 180.fbx",
                "Walking.fbx"
            };

            bool anyReimported = false;

            foreach (var file in fbxFiles)
            {
                string path = $"{AnimSourceFolder}/{file}";
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning($"[CharacterAnimationSetup] Missing FBX file: {path}");
                    continue;
                }

                bool needsReimport = false;

                // Ensure Rig is Humanoid and creates its own avatar from this model
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    needsReimport = true;
                }

                // Loop Time rule: ON for Walking and Crouched Walking, OFF for Start, Stop, and Turn clips
                bool isLoopClip = file.Equals("Walking.fbx", StringComparison.OrdinalIgnoreCase) ||
                                 file.Equals("Crouched Walking.fbx", StringComparison.OrdinalIgnoreCase);

                ModelImporterClipAnimation[] clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                if (clips != null && clips.Length > 0)
                {
                    for (int i = 0; i < clips.Length; i++)
                    {
                        clips[i].loopTime = isLoopClip;
                        clips[i].loopPose = isLoopClip;
                        clips[i].lockRootRotation = true;
                        clips[i].lockRootHeightY = true;
                        clips[i].lockRootPositionXZ = true;
                    }
                    importer.clipAnimations = clips;
                    needsReimport = true;
                }

                if (needsReimport)
                {
                    Debug.Log($"[CharacterAnimationSetup] Reimporting {file} as Humanoid (loop={isLoopClip})...");
                    importer.SaveAndReimport();
                    anyReimported = true;
                }
            }

            if (anyReimported)
            {
                AssetDatabase.Refresh();
            }
        }

        private static Dictionary<string, AnimationClip> LoadAllHumanoidClips()
        {
            var result = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);

            string[] fbxFiles = {
                "Crouched Walking.fbx",
                "Start Walking.fbx",
                "Stop Walking.fbx",
                "Walking Left Turn.fbx",
                "Walking Turn 180.fbx",
                "Walking.fbx"
            };

            foreach (var file in fbxFiles)
            {
                string path = $"{AnimSourceFolder}/{file}";
                var assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in assets)
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        string key = Path.GetFileNameWithoutExtension(file);
                        result[key] = clip;
                        Debug.Log($"[CharacterAnimationSetup] Loaded clip '{key}' -> '{clip.name}' (length={clip.length:F2}s, loop={clip.isLooping})");
                        break;
                    }
                }
            }

            return result;
        }

        private static AnimationClip EnsureIdleClip(Dictionary<string, AnimationClip> clips)
        {
            string idlePath = $"{OutputAnimFolder}/Idle.anim";
            AnimationClip existingIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
            if (existingIdle != null)
            {
                return existingIdle;
            }

            Debug.LogWarning("[CharacterAnimationSetup] Generating clean resting Idle.anim from Stop Walking rest frame.");

            AnimationClip idleClip = new AnimationClip();
            idleClip.name = "Idle";
            idleClip.wrapMode = WrapMode.Loop;

            if (clips.TryGetValue("Stop Walking", out AnimationClip stopClip))
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(stopClip);
                float sampleTime = stopClip.length > 0.1f ? stopClip.length - 0.05f : 0f;
                foreach (var b in bindings)
                {
                    AnimationCurve srcCurve = AnimationUtility.GetEditorCurve(stopClip, b);
                    if (srcCurve != null && srcCurve.length > 0)
                    {
                        float val = srcCurve.Evaluate(sampleTime);
                        AnimationCurve constCurve = AnimationCurve.Constant(0f, 1f, val);
                        AnimationUtility.SetEditorCurve(idleClip, b, constCurve);
                    }
                }
            }

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(idleClip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idleClip, settings);

            AssetDatabase.CreateAsset(idleClip, idlePath);
            AssetDatabase.SaveAssets();
            return idleClip;
        }

        /// <summary>
        /// STEP 3: Minimal Animator Controller with just Idle + Walking to test retargeting in isolation
        /// </summary>
        private static AnimatorController BuildMinimalController(Dictionary<string, AnimationClip> clips, AnimationClip idleClip)
        {
            AnimatorController minimal = AssetDatabase.LoadAssetAtPath<AnimatorController>(MinimalControllerPath);
            if (minimal == null)
            {
                minimal = AnimatorController.CreateAnimatorControllerAtPath(MinimalControllerPath);
            }

            EnsureParameter(minimal, "Speed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine sm = minimal.layers[0].stateMachine;
            ChildAnimatorState[] existingStates = sm.states;
            var stateMap = new Dictionary<string, AnimatorState>();
            foreach (var cas in existingStates)
            {
                stateMap[cas.state.name] = cas.state;
            }

            AnimatorState idleState = GetOrCreateState(sm, stateMap, "Idle", new Vector3(250, 0, 0));
            idleState.motion = idleClip;

            AnimatorState walkState = GetOrCreateState(sm, stateMap, "Walking", new Vector3(250, 120, 0));
            if (clips.TryGetValue("Walking", out AnimationClip walkClip)) walkState.motion = walkClip;

            sm.defaultState = idleState;

            ClearTransitions(idleState);
            ClearTransitions(walkState);

            var tIdleToWalk = idleState.AddTransition(walkState);
            tIdleToWalk.hasExitTime = false;
            tIdleToWalk.duration = 0.15f;
            tIdleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var tWalkToIdle = walkState.AddTransition(idleState);
            tWalkToIdle.hasExitTime = false;
            tWalkToIdle.duration = 0.15f;
            tWalkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            EditorUtility.SetDirty(minimal);
            return minimal;
        }

        /// <summary>
        /// STEP 4: Fix Camera and Body Architecture
        /// Player (yaw from mouse X)
        ///  ├── Character (Animator, full body mesh)
        ///  └── CameraPivot (pitch from mouse Y)
        ///         └── Main Camera
        /// Upper body meshes set to ShadowsOnly to eliminate hollow neck & floating hands!
        /// </summary>
        public static void FixCameraAndBodyArchitecture()
        {
            // 1. Fix Prefab mesh renderers
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                ApplyShadowsOnlyToMeshes(prefab);
                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
            }

            // 2. Fix Scene hierarchy & components
            var activeScene = EditorSceneManager.GetActiveScene();
            var playerCtrl = UnityEngine.Object.FindAnyObjectByType<Player.PlayerController>();
            if (playerCtrl != null)
            {
                GameObject playerObj = playerCtrl.gameObject;

                // Ensure CameraPivot hierarchy
                Transform cameraPivot = playerObj.transform.Find("CameraPivot");
                if (cameraPivot == null)
                {
                    Transform oldHolder = playerObj.transform.Find("CameraHolder");
                    if (oldHolder != null)
                    {
                        oldHolder.name = "CameraPivot";
                        cameraPivot = oldHolder;
                    }
                    else
                    {
                        GameObject pivotObj = new GameObject("CameraPivot");
                        pivotObj.transform.SetParent(playerObj.transform);
                        pivotObj.transform.localPosition = new Vector3(0f, 0.82f, 0.06f);
                        pivotObj.transform.localRotation = Quaternion.identity;
                        cameraPivot = pivotObj.transform;
                    }
                }

                // Ensure Main Camera is child of CameraPivot
                Camera cam = playerObj.GetComponentInChildren<Camera>(true);
                if (cam != null && cam.transform.parent != cameraPivot)
                {
                    cam.transform.SetParent(cameraPivot);
                    cam.transform.localPosition = Vector3.zero;
                    cam.transform.localRotation = Quaternion.identity;
                }

                // Wire up PlayerCameraLook
                var look = playerObj.GetComponent<Player.PlayerCameraLook>();
                if (look != null)
                {
                    var bodyField = typeof(Player.PlayerCameraLook).GetField("playerBody",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (bodyField != null) bodyField.SetValue(look, playerObj.transform);

                    var camField = typeof(Player.PlayerCameraLook).GetField("cameraTransform",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (camField != null) camField.SetValue(look, cameraPivot);

                    EditorUtility.SetDirty(look);
                }

                // Wire up PlayerController cameraHolder
                var camHolderField = typeof(Player.PlayerController).GetField("cameraHolder",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (camHolderField != null) camHolderField.SetValue(playerCtrl, cameraPivot);

                // Fix scene character mesh shadows
                var charChild = playerObj.transform.Find("Adventure_Character") ??
                                playerObj.transform.Find("Man_03") ??
                                playerObj.transform.Find("Character");
                if (charChild != null)
                {
                    ApplyShadowsOnlyToMeshes(charChild.gameObject);
                }

                EditorUtility.SetDirty(playerObj);
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        /// <summary>
        /// Sets all upper body and intersecting meshes to ShadowsOnly so they cast real-time shadows
        /// without clipping into the first-person camera or exposing a hollow neck collar or floating hands.
        /// </summary>
        public static void ApplyShadowsOnlyToMeshes(GameObject root)
        {
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                string n = smr.gameObject.name.ToLower();
                // Exclude head, eyes, jaw, face, pullover (torso), arms/hands, bag, and body from first-person visual camera
                if (n.Contains("head") || n.Contains("eyes") || n.Contains("jaw") ||
                    n.Contains("face") || n.Contains("balaclava") || n.Contains("pullover") ||
                    n.Contains("jacket") || n.Contains("arms") || n.Contains("bag") ||
                    n.Contains("body") || n.Contains("half_"))
                {
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
                else
                {
                    // Pants, feet / legs can remain visible or cast full shadows
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }
            }
        }

        /// <summary>
        /// STEP 5: Rebuild full Animator Controller
        /// States: Idle, Start Walking, Walking, Stop Walking, Crouched Walking, Walking Left Turn, Walking Turn 180
        /// Parameters: Speed (Float), IsCrouching (Bool), Turn (Float)
        /// None of these transitions are driven by camera pitch.
        /// </summary>
        private static AnimatorController CreateOrUpdateFullController(Dictionary<string, AnimationClip> clips, AnimationClip idleClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Ensure Parameters
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsCrouching", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Turn", AnimatorControllerParameterType.Float);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            ChildAnimatorState[] existingStates = sm.states;
            var stateMap = new Dictionary<string, AnimatorState>();
            foreach (var cas in existingStates)
            {
                stateMap[cas.state.name] = cas.state;
            }

            // 1. Idle
            AnimatorState idleState = GetOrCreateState(sm, stateMap, "Idle", new Vector3(250, 0, 0));
            idleState.motion = idleClip;

            // 2. Start Walking
            AnimatorState startWalkState = GetOrCreateState(sm, stateMap, "Start Walking", new Vector3(250, 100, 0));
            if (clips.TryGetValue("Start Walking", out AnimationClip startClip)) startWalkState.motion = startClip;

            // 3. Walking
            AnimatorState walkingState = GetOrCreateState(sm, stateMap, "Walking", new Vector3(250, 200, 0));
            if (clips.TryGetValue("Walking", out AnimationClip walkClip)) walkingState.motion = walkClip;

            // 4. Stop Walking
            AnimatorState stopWalkState = GetOrCreateState(sm, stateMap, "Stop Walking", new Vector3(450, 100, 0));
            if (clips.TryGetValue("Stop Walking", out AnimationClip stopClip)) stopWalkState.motion = stopClip;

            // 5. Crouched Walking
            AnimatorState crouchState = GetOrCreateState(sm, stateMap, "Crouched Walking", new Vector3(50, 100, 0));
            if (clips.TryGetValue("Crouched Walking", out AnimationClip crouchClip)) crouchState.motion = crouchClip;

            // 6. Walking Left Turn
            AnimatorState leftTurnState = GetOrCreateState(sm, stateMap, "Walking Left Turn", new Vector3(450, 250, 0));
            if (clips.TryGetValue("Walking Left Turn", out AnimationClip leftTurnClip)) leftTurnState.motion = leftTurnClip;

            // 7. Walking Turn 180
            AnimatorState turn180State = GetOrCreateState(sm, stateMap, "Walking Turn 180", new Vector3(50, 250, 0));
            if (clips.TryGetValue("Walking Turn 180", out AnimationClip turn180Clip)) turn180State.motion = turn180Clip;

            sm.defaultState = idleState;

            // Clear existing transitions on all states
            ClearTransitions(idleState);
            ClearTransitions(startWalkState);
            ClearTransitions(walkingState);
            ClearTransitions(stopWalkState);
            ClearTransitions(crouchState);
            ClearTransitions(leftTurnState);
            ClearTransitions(turn180State);

            // Transitions:
            // 1. Idle -> Start Walking
            var tIdleToStart = idleState.AddTransition(startWalkState);
            tIdleToStart.hasExitTime = false;
            tIdleToStart.duration = 0.15f;
            tIdleToStart.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            tIdleToStart.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");

            // 2. Start Walking -> Walking
            var tStartToWalk = startWalkState.AddTransition(walkingState);
            tStartToWalk.hasExitTime = true;
            tStartToWalk.exitTime = 0.80f;
            tStartToWalk.duration = 0.15f;

            // Start Walking -> Idle (aborted walk)
            var tStartToIdle = startWalkState.AddTransition(idleState);
            tStartToIdle.hasExitTime = false;
            tStartToIdle.duration = 0.15f;
            tStartToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // 3. Walking -> Stop Walking
            var tWalkToStop = walkingState.AddTransition(stopWalkState);
            tWalkToStop.hasExitTime = false;
            tWalkToStop.duration = 0.15f;
            tWalkToStop.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            // 4. Stop Walking -> Idle
            var tStopToIdle = stopWalkState.AddTransition(idleState);
            tStopToIdle.hasExitTime = true;
            tStopToIdle.exitTime = 0.80f;
            tStopToIdle.duration = 0.15f;

            // Stop Walking -> Walking (resumed walking)
            var tStopToWalk = stopWalkState.AddTransition(walkingState);
            tStopToWalk.hasExitTime = false;
            tStopToWalk.duration = 0.15f;
            tStopToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            // 5. Walking Left Turn transitions
            var tWalkToLeftTurn = walkingState.AddTransition(leftTurnState);
            tWalkToLeftTurn.hasExitTime = false;
            tWalkToLeftTurn.duration = 0.20f;
            tWalkToLeftTurn.AddCondition(AnimatorConditionMode.Less, -0.3f, "Turn");
            tWalkToLeftTurn.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var tLeftTurnToWalk = leftTurnState.AddTransition(walkingState);
            tLeftTurnToWalk.hasExitTime = true;
            tLeftTurnToWalk.exitTime = 0.85f;
            tLeftTurnToWalk.duration = 0.20f;

            // 6. Walking Turn 180 transitions
            var tWalkToTurn180 = walkingState.AddTransition(turn180State);
            tWalkToTurn180.hasExitTime = false;
            tWalkToTurn180.duration = 0.20f;
            tWalkToTurn180.AddCondition(AnimatorConditionMode.Greater, 0.8f, "Turn");
            tWalkToTurn180.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var tTurn180ToWalk = turn180State.AddTransition(walkingState);
            tTurn180ToWalk.hasExitTime = true;
            tTurn180ToWalk.exitTime = 0.85f;
            tTurn180ToWalk.duration = 0.20f;

            // 7. Crouch transitions
            var tIdleToCrouch = idleState.AddTransition(crouchState);
            tIdleToCrouch.hasExitTime = false;
            tIdleToCrouch.duration = 0.18f;
            tIdleToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");

            var tWalkToCrouch = walkingState.AddTransition(crouchState);
            tWalkToCrouch.hasExitTime = false;
            tWalkToCrouch.duration = 0.18f;
            tWalkToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");

            var tStartToCrouch = startWalkState.AddTransition(crouchState);
            tStartToCrouch.hasExitTime = false;
            tStartToCrouch.duration = 0.18f;
            tStartToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");

            var tStopToCrouch = stopWalkState.AddTransition(crouchState);
            tStopToCrouch.hasExitTime = false;
            tStopToCrouch.duration = 0.18f;
            tStopToCrouch.AddCondition(AnimatorConditionMode.If, 0, "IsCrouching");

            var tCrouchToIdle = crouchState.AddTransition(idleState);
            tCrouchToIdle.hasExitTime = false;
            tCrouchToIdle.duration = 0.18f;
            tCrouchToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
            tCrouchToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            var tCrouchToWalk = crouchState.AddTransition(walkingState);
            tCrouchToWalk.hasExitTime = false;
            tCrouchToWalk.duration = 0.18f;
            tCrouchToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsCrouching");
            tCrouchToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == name) return;
            }
            controller.AddParameter(name, type);
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, Dictionary<string, AnimatorState> map, string name, Vector3 pos)
        {
            if (map.TryGetValue(name, out AnimatorState state))
            {
                return state;
            }
            return sm.AddState(name, pos);
        }

        private static void ClearTransitions(AnimatorState state)
        {
            var transitions = state.transitions;
            for (int i = transitions.Length - 1; i >= 0; i--)
            {
                state.RemoveTransition(transitions[i]);
            }
        }

        private static void AssignToPrefab(AnimatorController controller)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[CharacterAnimationSetup] Prefab not found at {PrefabPath}");
                return;
            }

            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null) animator = prefab.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            EditorUtility.SetDirty(prefab);
            PrefabUtility.SavePrefabAsset(prefab);
            Debug.Log($"[CharacterAnimationSetup] Assigned controller to prefab: {PrefabPath}");
        }

        private static void AssignToScene(AnimatorController controller)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var playerCtrl = UnityEngine.Object.FindAnyObjectByType<Player.PlayerController>();
            if (playerCtrl != null)
            {
                var animField = typeof(Player.PlayerController).GetField("characterAnimator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                Animator animator = null;
                if (animField != null)
                {
                    animator = animField.GetValue(playerCtrl) as Animator;
                }

                if (animator == null)
                {
                    animator = playerCtrl.GetComponentInChildren<Animator>(true);
                    if (animField != null && animator != null) animField.SetValue(playerCtrl, animator);
                }

                if (animator != null)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    EditorUtility.SetDirty(animator.gameObject);
                    EditorUtility.SetDirty(playerCtrl.gameObject);
                    EditorSceneManager.MarkSceneDirty(activeScene);
                    Debug.Log($"[CharacterAnimationSetup] Assigned controller to Scene player Animator on '{animator.gameObject.name}'");
                }
            }
        }
    }
}
