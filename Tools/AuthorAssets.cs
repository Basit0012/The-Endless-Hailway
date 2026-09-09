using System;
using System.IO;

public class AuthorAssets
{
    public static void Main()
    {
        string baseDir = Directory.GetCurrentDirectory();
        string audioDir = Path.Combine(baseDir, "Assets/_Project/Audio");
        string paintTexDir = Path.Combine(baseDir, "Assets/_Project/Art/Textures/Paintings");
        string paintMatDir = Path.Combine(baseDir, "Assets/_Project/Materials/Paintings");
        string anomDir = Path.Combine(baseDir, "Assets/_Project/ScriptableObjects/Anomalies");
        string loopDir = Path.Combine(baseDir, "Assets/_Project/ScriptableObjects/Loops");

        if (!Directory.Exists(paintMatDir)) Directory.CreateDirectory(paintMatDir);

        Console.WriteLine("1. Writing Audio Meta Files...");
        WriteAudioMeta(Path.Combine(audioDir, "sfx_heartbeat.wav.meta"), "a1111111111111111111111111111101", false);
        WriteAudioMeta(Path.Combine(audioDir, "sfx_breathing.wav.meta"), "a1111111111111111111111111111102", false);
        WriteAudioMeta(Path.Combine(audioDir, "amb_whispers.wav.meta"), "a1111111111111111111111111111103", true);
        WriteAudioMeta(Path.Combine(audioDir, "sfx_jumpscare.wav.meta"), "a1111111111111111111111111111104", false);

        Console.WriteLine("2. Writing Painting Texture Meta Files...");
        for (int i = 0; i < 5; i++)
        {
            string texMeta = Path.Combine(paintTexDir, string.Format("T_Painting_Stage{0}.png.meta", i));
            string guid = string.Format("b222222222222222222222222222220{0}", i);
            WriteTextureMeta(texMeta, guid);
        }

        Console.WriteLine("3. Writing Painting Material Files...");
        for (int i = 0; i < 5; i++)
        {
            string matPath = Path.Combine(paintMatDir, string.Format("M_Painting_Stage{0}.mat", i));
            string matMeta = matPath + ".meta";
            string matGuid = string.Format("c333333333333333333333333333330{0}", i);
            string texGuid = string.Format("b222222222222222222222222222220{0}", i);
            WriteMaterial(matPath, string.Format("M_Painting_Stage{0}", i), texGuid);
            WriteGenericMeta(matMeta, matGuid);
        }

        Console.WriteLine("4. Writing PaintingSet Asset...");
        string setPath = Path.Combine(anomDir, "Set_WallPaintings.asset");
        string setMeta = setPath + ".meta";
        WritePaintingSet(setPath);
        WriteGenericMeta(setMeta, "d4444444444444444444444444444400");

        Console.WriteLine("5. Writing Anomaly Assets...");
        WritePaintingAnomaly(Path.Combine(anomDir, "Anom_Painting_Stage2.asset"), "Anom_Painting_Stage2", "c3333333333333333333333333333302", "The framed landscape transforms into an uncanny dollhouse room with a staring porcelain doll.");
        WriteGenericMeta(Path.Combine(anomDir, "Anom_Painting_Stage2.asset.meta"), "d4444444444444444444444444444402");

        WritePaintingAnomaly(Path.Combine(anomDir, "Anom_Painting_Stage3.asset"), "Anom_Painting_Stage3", "c3333333333333333333333333333303", "The doll painting corrupts into weeping black oil and an unhinged screaming mouth.");
        WriteGenericMeta(Path.Combine(anomDir, "Anom_Painting_Stage3.asset.meta"), "d4444444444444444444444444444403");

        WritePaintingAnomaly(Path.Combine(anomDir, "Anom_Painting_Stage4.asset"), "Anom_Painting_Stage4", "c3333333333333333333333333333304", "The painting depicts the corridor in flames with the Observer reaching out from the canvas.");
        WriteGenericMeta(Path.Combine(anomDir, "Anom_Painting_Stage4.asset.meta"), "d4444444444444444444444444444404");

        WriteObserverAggressiveAnomaly(Path.Combine(anomDir, "Anom_Observer_Aggressive.asset"));
        WriteGenericMeta(Path.Combine(anomDir, "Anom_Observer_Aggressive.asset.meta"), "d4444444444444444444444444444405");

        Console.WriteLine("6. Updating Loop Assets...");
        UpdateLoop(Path.Combine(loopDir, "Loop_03_FirstEncounter.asset"), "d4444444444444444444444444444402");
        UpdateLoop(Path.Combine(loopDir, "Loop_05_DeepWrongness.asset"), "d4444444444444444444444444444403");
        UpdateLoop(Path.Combine(loopDir, "Loop_06_TheDarkening.asset"), "d4444444444444444444444444444404", "d4444444444444444444444444444405");

        Console.WriteLine("Finished authoring assets successfully!");
    }

    private static void WriteAudioMeta(string path, string guid, bool is3D)
    {
        string content = string.Format(@"fileFormatVersion: 2
guid: {0}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 1
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 1
  loadInBackground: 0
  ambisonic: 0
  3D: {1}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
", guid, is3D ? 1 : 0);
        File.WriteAllText(path, content);
    }

    private static void WriteTextureMeta(string path, string guid)
    {
        string content = string.Format(@"fileFormatVersion: 2
guid: {0}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaIsTransparency: 0
  spriteAnimatorIndex: 0
  assetBundleName: 
  assetBundleVariant: 
", guid);
        File.WriteAllText(path, content);
    }

    private static void WriteGenericMeta(string path, string guid)
    {
        string content = string.Format(@"fileFormatVersion: 2
guid: {0}
NativeFormatImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
", guid);
        File.WriteAllText(path, content);
    }

    private static void WriteMaterial(string path, string name, string texGuid)
    {
        string content = string.Format(@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &-7511754225479150587
MonoBehaviour:
  m_ObjectHideFlags: 11
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d0353a89b1f911e48b9e16bdc9f2e058, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: Unity.RenderPipelines.Universal.Editor::UnityEditor.Rendering.Universal.AssetVersion
  version: 10
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {0}
  m_Shader: {{fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
  m_ValidKeywords: []
  m_InvalidKeywords: []
  m_LightmapFlags: 4
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap:
    RenderType: Opaque
  disabledShaderPasses:
  - MOTIONVECTORS
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {1}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}
    m_Ints: []
    m_Floats:
    - _Smoothness: 0.15
    - _Metallic: 0
    m_Colors:
    - _BaseColor: {{r: 1, g: 1, b: 1, a: 1}}
", name, texGuid);
        File.WriteAllText(path, content);
    }

    private static void WritePaintingSet(string path)
    {
        string content = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 76bac12c3ed980649933c03be6e2a017, type: 3}
  m_Name: Set_WallPaintings
  m_EditorClassIdentifier: Assembly-CSharp::EndlessHallway.Anomaly.PaintingSet
  stageMaterials:
  - {fileID: 2100000, guid: c3333333333333333333333333333300, type: 2}
  - {fileID: 2100000, guid: c3333333333333333333333333333301, type: 2}
  - {fileID: 2100000, guid: c3333333333333333333333333333302, type: 2}
  - {fileID: 2100000, guid: c3333333333333333333333333333303, type: 2}
  - {fileID: 2100000, guid: c3333333333333333333333333333304, type: 2}
";
        File.WriteAllText(path, content);
    }

    private static void WritePaintingAnomaly(string path, string id, string matGuid, string desc)
    {
        string content = string.Format(@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 4fb275bb01d544f4b8a51952f770a580, type: 3}}
  m_Name: {0}
  m_EditorClassIdentifier: Assembly-CSharp::EndlessHallway.Anomaly.AnomalyDefinitionSO
  anomalyId: {0}
  description: {1}
  targetObjectId: WallPainting_A
  anomalyType: 2
  positionOffset: {{x: 0, y: 0, z: 0}}
  rotationOffset: {{x: 0, y: 0, z: 0}}
  activeState: 1
  targetMaterial: {{fileID: 2100000, guid: {2}, type: 2}}
  isLightFlickering: 1
  flickerIntervalMin: 0.05
  flickerIntervalMax: 0.25
  lightColor: {{r: 1, g: 1, b: 1, a: 1}}
  setDoorLocked: 1
  setDoorOpen: 0
  audioClip: {{fileID: 0}}
  loopAudio: 0
  audioVolume: 1
  volumeProfile: {{fileID: 0}}
  observerSpawnPointId: FarHallway
  observerState: 1
", id, desc, matGuid);
        File.WriteAllText(path, content);
    }

    private static void WriteObserverAggressiveAnomaly(string path)
    {
        string content = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4fb275bb01d544f4b8a51952f770a580, type: 3}
  m_Name: Anom_Observer_Aggressive
  m_EditorClassIdentifier: Assembly-CSharp::EndlessHallway.Anomaly.AnomalyDefinitionSO
  anomalyId: Anom_Observer_Aggressive
  description: Observer stalks aggressively down the corridor if the player looks away or lingers.
  targetObjectId: ObserverEntity
  anomalyType: 7
  positionOffset: {x: 0, y: 0, z: 0}
  rotationOffset: {x: 0, y: 0, z: 0}
  activeState: 1
  targetMaterial: {fileID: 0}
  isLightFlickering: 1
  flickerIntervalMin: 0.05
  flickerIntervalMax: 0.25
  lightColor: {r: 1, g: 1, b: 1, a: 1}
  setDoorLocked: 1
  setDoorOpen: 0
  audioClip: {fileID: 0}
  loopAudio: 0
  audioVolume: 1
  volumeProfile: {fileID: 0}
  observerSpawnPointId: FarHallway
  observerState: 4
";
        File.WriteAllText(path, content);
    }

    private static void UpdateLoop(string path, params string[] newAnomGuids)
    {
        if (!File.Exists(path)) return;
        string text = File.ReadAllText(path);
        foreach (var guid in newAnomGuids)
        {
            if (!text.Contains(guid))
            {
                text = text.TrimEnd() + "\n  - {fileID: 11400000, guid: " + guid + ", type: 2}\n";
            }
        }
        File.WriteAllText(path, text);
    }
}
