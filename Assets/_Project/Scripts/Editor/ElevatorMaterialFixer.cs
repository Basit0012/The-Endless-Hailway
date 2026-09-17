using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace EndlessHallway.Editor
{
    [InitializeOnLoad]
    public static class ElevatorMaterialFixer
    {
        private const string MaterialsFolder = "Assets/_Project/Materials/Elevator";
        private const string TexturesFolder = "Assets/_Project/Textures/Elevator";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        static ElevatorMaterialFixer()
        {
            EditorApplication.delayCall += () =>
            {
                FixAllElevatorMaterials(false);
            };
        }

        [MenuItem("Tools/Endless Hallway/Fix Elevator Materials (URP Lit)", false, 0)]
        public static void FixMaterialsMenu()
        {
            FixAllElevatorMaterials(true);
        }

        public static void FixAllElevatorMaterials(bool showDialog)
        {
            // 1. Ensure URP Lit shader is available
            Shader urpLitShader = Shader.Find(UrpLitShaderName);
            if (urpLitShader == null)
            {
                Debug.LogError($"[ElevatorMaterialFixer] '{UrpLitShaderName}' shader not found! Confirm URP is active.");
                return;
            }

            if (!Directory.Exists(MaterialsFolder))
            {
                Directory.CreateDirectory(MaterialsFolder);
                AssetDatabase.Refresh();
            }

            // 2. Setup/ensure 5 materials with textures
            var matMap = new Dictionary<string, Material>();
            string[] matNames = {
                "lift_Material_u1_v1", // 0: Outer frame & doors
                "lift_Material_u2_v1", // 1: Control panels & call buttons
                "lift_Material_u3_v1", // 2: Structural steel frame
                "lift_Material_u1_v2", // 3: Housing & upper frame
                "lift_Material_u2_v2"  // 4: Threshold & lower brackets
            };

            for (int i = 0; i < matNames.Length; i++)
            {
                string matPath = $"{MaterialsFolder}/{matNames[i]}.mat";
                string texPath = $"{TexturesFolder}/texture_{i}_image_{i}.jpg";

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(urpLitShader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }

                if (mat.shader != urpLitShader)
                {
                    mat.shader = urpLitShader;
                }

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                }

                // Proper PBR parameters for Soviet industrial painted metal
                mat.SetColor("_BaseColor", Color.white);
                mat.SetColor("_Color", Color.white);
                mat.SetFloat("_Metallic", 0.65f);
                mat.SetFloat("_Smoothness", 0.45f);

                EditorUtility.SetDirty(mat);
                matMap[matNames[i]] = mat;
            }

            // 3. Ensure native cabin materials
            Material interiorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Elevator_Interior.mat");
            Material metalMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/M_Elevator_Metal.mat");

            // 4. Update scene objects
            int renderersFixed = 0;
            GameObject elevatorRoot = GameObject.Find("Elevator");
            if (elevatorRoot != null)
            {
                // Fix cabin interior walls, ceiling, floor (use M_Elevator_Interior so cabin is never pink)
                if (interiorMat != null)
                {
                    string[] cabinParts = { "Cabin_Floor", "Cabin_Ceiling", "Cabin_BackWall", "Cabin_LeftWall", "Cabin_RightWall" };
                    foreach (string part in cabinParts)
                    {
                        Transform t = elevatorRoot.transform.Find(part);
                        if (t != null)
                        {
                            Renderer r = t.GetComponent<Renderer>();
                            if (r != null)
                            {
                                r.sharedMaterial = interiorMat;
                                EditorUtility.SetDirty(r);
                                renderersFixed++;
                            }
                        }
                    }
                }

                // Fix sliding doors
                Transform leftDoor = elevatorRoot.transform.Find("ElevatorDoor_Left");
                Transform rightDoor = elevatorRoot.transform.Find("ElevatorDoor_Right");
                Material doorMat = matMap["lift_Material_u1_v1"] != null ? matMap["lift_Material_u1_v1"] : metalMat;
                if (leftDoor != null && leftDoor.GetComponent<Renderer>() != null)
                {
                    leftDoor.GetComponent<Renderer>().sharedMaterial = doorMat;
                    EditorUtility.SetDirty(leftDoor.GetComponent<Renderer>());
                    renderersFixed++;
                }
                if (rightDoor != null && rightDoor.GetComponent<Renderer>() != null)
                {
                    rightDoor.GetComponent<Renderer>().sharedMaterial = doorMat;
                    EditorUtility.SetDirty(rightDoor.GetComponent<Renderer>());
                    renderersFixed++;
                }

                // Fix Button panel
                Transform buttonPanel = elevatorRoot.transform.Find("ElevatorButtonPanel");
                if (buttonPanel != null && buttonPanel.GetComponent<Renderer>() != null)
                {
                    buttonPanel.GetComponent<Renderer>().sharedMaterial = matMap["lift_Material_u2_v1"];
                    EditorUtility.SetDirty(buttonPanel.GetComponent<Renderer>());
                    renderersFixed++;
                }

                // Fix all renderers in Old_ussr_elevator_entrance / Elevator_USSR_Entrance
                Renderer[] allRenderers = elevatorRoot.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in allRenderers)
                {
                    string rName = r.gameObject.name.ToLower();
                    Material targetMat = null;

                    if (rName.Contains("u1_v1")) targetMat = matMap["lift_Material_u1_v1"];
                    else if (rName.Contains("u2_v1")) targetMat = matMap["lift_Material_u2_v1"];
                    else if (rName.Contains("u3_v1")) targetMat = matMap["lift_Material_u3_v1"];
                    else if (rName.Contains("u1_v2")) targetMat = matMap["lift_Material_u1_v2"];
                    else if (rName.Contains("u2_v2")) targetMat = matMap["lift_Material_u2_v2"];

                    if (targetMat != null)
                    {
                        r.sharedMaterial = targetMat;
                        EditorUtility.SetDirty(r);
                        renderersFixed++;
                    }
                }

                // Mark scene dirty and save
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[ElevatorMaterialFixer] SUCCESS! Configured {matNames.Length} URP Lit materials and updated {renderersFixed} renderers.");
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Elevator Materials Fixed",
                    $"Successfully updated {matNames.Length} elevator materials to Universal Render Pipeline/Lit with authentic textures!",
                    "OK");
            }
        }
    }
}
