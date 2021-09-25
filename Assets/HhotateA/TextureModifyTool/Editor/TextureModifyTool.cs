/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using HhotateA.AvatarModifyTools.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace HhotateA.AvatarModifyTools.TextureModifyTool
{
    public class TextureModifyTool : EditorWindow
    {
        [MenuItem("Window/HhotateA/にゃんにゃんアバターペインター(TextureModifyTool)",false,-11)]
        static void  ShowWindow()
        {
            TextureModifyTool wnd = (TextureModifyTool)EditorWindow.GetWindow(typeof(TextureModifyTool));
            wnd.titleContent = new GUIContent("にゃんにゃんアバターペインター");
        }
        
        // ボタン設定
        private int drawButton = 0;
        private int rotateButton = 1;
        private int moveButton = 2;
        
        // 改造するメッシュのルートオブジェクト
        private GameObject avatar;
        
        // 改造前後のマテリアルの保持
        private Material[] currentMaterials = new Material[0];
        private Material[] editMaterials = new Material[0];
        
        // 判定用オブジェクト
        private MeshCollider editMeshCollider;
        
        // Modify用クラス
        private AvatarMonitor avatarMonitor;
        private TexturePreviewer texturePreviewer;
        private MeshCreater meshCreater;
        private TextureCreator[] textureCreators;
        private TextureCreator textureCreator
        {
            get
            {
                if (textureCreators != null)
                {
                    if (editIndex != -1 && editIndex < textureCreators.Length)
                    {
                        return textureCreators[editIndex];
                    }
                }

                return null;
            }
        }
        private int editIndex = -1;
        private bool isEnablePreview = true;
        
        ReorderableList layerReorderableList;
        
        // 各種オプション
        private bool loadUVMap = true;
        private bool straightMode = false; // 定規モード
        private Vector2 straightBuffer = Vector2.zero;
        private bool squareMode = true; // 正方形固定(TextureWindow)
        private bool maskAllLayers = true;
        private bool isDragBuffer = false;

        private bool keyboardShortcut = true;

        private Color brushBuffer;
        
        // 表示切替用バッファー
        private Vector2 rendsScroll = Vector2.zero;
        private Vector2 layersScroll = Vector2.zero;

        //private Texture stampIcon;

        // ブラシ色選択
        private int colorIndex = 0;
        Color brushColor => pen.brushColor ?? brushColors[colorIndex];
        Color[] brushColors = new Color[5]{Color.white,Color.black,Color.red, Color.green, Color.blue};
        // グラデーション
        private Gradient gradient = new Gradient();
        // ペン選択
        private int penIndex = 0;
        private TexturePenTool pen => penTools[penIndex];
        private TexturePenTool[] _penTools;
        private TexturePenTool[] penTools
        {
            get
            {
                if (_penTools == null)
                {
                    _penTools = new TexturePenTool[7]
                    {
                        new TexturePenTool(EnvironmentGUIDs.penTooIcon,"Pen",TexturePenTool.ExtraTool.Default,2.5f,0.03f, 1f,null), 
                        new TexturePenTool(EnvironmentGUIDs.splayToolIcon,"Splay",TexturePenTool.ExtraTool.Default,0.05f,0.04f, 0f,null), 
                        new TexturePenTool(EnvironmentGUIDs.eraserToolIcon,"Eraser",TexturePenTool.ExtraTool.Default,2f,0.02f, 1f,Color.clear), 
                        new TexturePenTool(EnvironmentGUIDs.stampToolIcon,"Stamp",TexturePenTool.ExtraTool.StampPaste,1f,0.1f, 0f,null), 
                        new TexturePenTool(EnvironmentGUIDs.fillTooIcon,"Fill",TexturePenTool.ExtraTool.Fill,1f,0.03f, 0f,null), 
                        new TexturePenTool(EnvironmentGUIDs.gaussianToolIcon,"Gaussian",TexturePenTool.ExtraTool.Gaussian,2f,0.01f, 0f,Color.clear), 
                        new TexturePenTool(EnvironmentGUIDs.colorPickToolIcon,"ColorPick",TexturePenTool.ExtraTool.ColorPick,0,0, 0,null),
                    };
                }

                return _penTools;
            }
        }
        
        private void OnGUI()
        {
            var ec = Event.current;
            if (meshCreater == null)
            {
                WindowBase.TitleStyle("にゃんにゃんテクスチャエディター");
                WindowBase.DetailStyle("Unityだけでアバターのテクスチャ改変ができるツールです．",EnvironmentGUIDs.readme);
                avatar = EditorGUILayout.ObjectField("", avatar, typeof(GameObject), true) as GameObject;
                if (GUILayout.Button("Setup"))
                {
                    Setup();
                }
                        
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                loadUVMap = EditorGUILayout.Toggle("Load UVMap", loadUVMap);
                
                WindowBase.Signature();
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(380)))
                {
                    rendsScroll = EditorGUILayout.BeginScrollView(rendsScroll);
                    using (new EditorGUILayout.VerticalScope())
                    {
                        for (int i = 0; i < meshCreater.GetMaterials().Length; i++)
                        {
                            if (meshCreater.GetMaterials()[i] == null) continue;
                            if (meshCreater.GetMaterials()[i].mainTexture == null) continue;
                            // Todo この辺のボタンの画像か
                            //if (GUILayout.Button(new GUIContent(meshCreater.GetMaterials()[i].name,meshCreater.GetMaterials()[i].mainTexture),GUILayout.Height(75),GUILayout.Width(300)))
                            if (GUILayout.Button(meshCreater.GetMaterials()[i].name))
                            {
                                SelectRend(i);
                            }

                        }
                    }
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space();
                    loadUVMap = EditorGUILayout.Toggle("Load UVMap", loadUVMap);
                    EditorGUILayout.Space();
                    squareMode = EditorGUILayout.Toggle("Texture Square Mode", squareMode);
                    EditorGUILayout.Space();
                    isEnablePreview = EditorGUILayout.Toggle("Enable Preview", isEnablePreview);
                    EditorGUILayout.Space();
                    
                    if (textureCreator == null) return;
                    if (texturePreviewer == null) return;

                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                using (new EditorGUI.DisabledScope(textureCreator?.LayerCount() > 16))
                                {
                                    if (GUILayout.Button("Add Layer"))
                                    {
                                        textureCreator.AddLayer(brushColor, gradient);
                                    }

                                    if (GUILayout.Button("Add Mask"))
                                    {
                                        textureCreator.AddMask(brushColor, gradient);
                                    }

                                    if (GUILayout.Button("Combine Layers"))
                                    {
                                        textureCreator.AddLayer(textureCreator.GetTemporaryTexture());
                                    }

                                    if (GUILayout.Button("Load Texture File"))
                                    {
                                        var path = EditorUtility.OpenFilePanel("Load", "Assets", "png");
                                        if (string.IsNullOrWhiteSpace(path)) return;
                                        byte[] bytes = File.ReadAllBytes(path);
                                        Texture2D tex = new Texture2D(2, 2);
                                        tex.LoadImage(bytes);
                                        textureCreator.AddLayer(tex);
                                    }
                                }
                            }
                        }
                        EditorGUILayout.LabelField("");
                        layersScroll = EditorGUILayout.BeginScrollView(layersScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                        layerReorderableList.DoLayoutList();
                        EditorGUILayout.EndScrollView();
                    }
                    
                    EditorGUILayout.LabelField("");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(!textureCreator.CanUndo()))
                        {
                            if (GUILayout.Button("Undo"))
                            {
                                textureCreator.UndoEditTexture();
                            }
                        }

                        using (new EditorGUI.DisabledScope(!textureCreator.CanRedo()))
                        {
                            if (GUILayout.Button("Redo"))
                            {
                                textureCreator.RedoEditTexture();
                            }
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int i = 0; i < brushColors.Length; i++)
                        {
                            if (colorIndex == i)
                            {
                                brushColors[i] = EditorGUILayout.ColorField(new GUIContent(""), brushColors[i],
                                    false,
                                    true,
                                    false, GUILayout.Width(70), GUILayout.Height(50));
                            }
                            else
                            {
                                Texture2D texture = new Texture2D(64, 64);

                                for (int y = 0; y < texture.height; y++)
                                {
                                    for (int x = 0; x < texture.width; x++)
                                    {
                                        texture.SetPixel(x, y, brushColors[i]);
                                    }
                                }

                                texture.Apply();
                                if (GUILayout.Button(texture, GUILayout.Width(65), GUILayout.Height(50)))
                                {
                                    colorIndex = i;
                                }
                            }
                        }

                        using (new EditorGUI.DisabledScope(penIndex == 6))
                        {
                            if (penTools[6].Button(30, 50))
                            {
                                penIndex = 6;
                            }
                        }
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (pen.extraTool == TexturePenTool.ExtraTool.Default)
                        {
                            if (straightMode)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    if (GUILayout.Button("Reset", GUILayout.Width(150)))
                                    {
                                        gradient = new Gradient();
                                    }

                                    gradient = EditorGUILayout.GradientField("", gradient);
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("");
                            }
                            straightMode = EditorGUILayout.Toggle("Straight Line", straightMode);
                            EditorGUILayout.Space();
                            
                            pen.brushStrength = EditorGUILayout.Slider("Brush Strength", pen.brushStrength, 0.5f, 2.5f);
                            EditorGUILayout.Space();
                            pen.brushWidth = EditorGUILayout.Slider("Brush Width", pen.brushWidth, 0.0f, 0.05f);
                            EditorGUILayout.Space();
                            pen.brushPower = EditorGUILayout.Slider("Opaque", pen.brushPower, 0f, 1f);
                        }
                        else
                        if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy || pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
                        {
                            EditorGUILayout.Space();
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                //straightMode = EditorGUILayout.Toggle("Copy Texture", straightMode);
                                using (new EditorGUI.DisabledScope(pen.extraTool == TexturePenTool.ExtraTool.StampCopy))
                                {
                                    if (GUILayout.Button("Copy"))
                                    {
                                        pen.extraTool = TexturePenTool.ExtraTool.StampCopy;
                                    }
                                }
                                using (new EditorGUI.DisabledScope(pen.extraTool == TexturePenTool.ExtraTool.StampPaste))
                                {
                                    if (GUILayout.Button("Paste"))
                                    {
                                        pen.extraTool = TexturePenTool.ExtraTool.StampPaste;
                                    }
                                }
                            }

                            if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy)
                            {
                                maskAllLayers = EditorGUILayout.Toggle("Copy All Layers", maskAllLayers);
                            }
                            else
                            if(pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
                            {
                                straightMode = EditorGUILayout.Toggle("Straight Line", straightMode);
                            }
                            
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                var stamp = (Texture2D) EditorGUILayout.ObjectField(pen.icon, typeof(Texture2D), false,
                                    GUILayout.Width(60), GUILayout.Height(60));
                                if (stamp != null)
                                {
                                    UpdateStampTexture(stamp);
                                }

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    pen.brushStrength = EditorGUILayout.FloatField("Aspect Ratio", pen.brushStrength);
                                    EditorGUILayout.Space();
                                    pen.brushWidth = EditorGUILayout.Slider("Size", pen.brushWidth, 0.0f, 1f);
                                    EditorGUILayout.Space();
                                    pen.brushPower =
                                        EditorGUILayout.Slider("Rotation", pen.brushPower * 360f, 0f, 360f) /
                                        360f;
                                }
                            }
                        }
                        else
                        if (pen.extraTool == TexturePenTool.ExtraTool.Fill)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Reset", GUILayout.Width(150)))
                                {
                                    gradient = new Gradient();
                                }

                                gradient = EditorGUILayout.GradientField("", gradient);
                            }
                            straightMode = EditorGUILayout.Toggle("StraightGradation", straightMode);
                            EditorGUILayout.Space();
                            
                            pen.brushStrength = EditorGUILayout.Slider("Area Expansion", pen.brushStrength, 0f, 10f);
                            EditorGUILayout.Space();
                            pen.brushWidth = EditorGUILayout.Slider("Threshold", pen.brushWidth, 0.0f, 0.1f);
                            EditorGUILayout.Space();
                            maskAllLayers = EditorGUILayout.Toggle("Mask All Texture", maskAllLayers);
                        }
                        else if (pen.extraTool == TexturePenTool.ExtraTool.ColorPick)
                        {
                            EditorGUILayout.LabelField("");
                            EditorGUILayout.LabelField("");
                            
                            EditorGUILayout.LabelField("");
                            EditorGUILayout.Space();
                            EditorGUILayout.LabelField("");
                            EditorGUILayout.Space();
                            maskAllLayers = EditorGUILayout.Toggle("Read All Layers", maskAllLayers);
                        }
                        else
                        if (pen.extraTool == TexturePenTool.ExtraTool.Gaussian)
                        {
                            EditorGUILayout.LabelField("");
                            straightMode = EditorGUILayout.Toggle("Straight Line", straightMode);
                            EditorGUILayout.Space();
                            
                            pen.brushStrength = EditorGUILayout.Slider("Brush Strength", pen.brushStrength, 0.5f, 2.5f);
                            EditorGUILayout.Space();
                            pen.brushWidth = EditorGUILayout.Slider("Brush Width", pen.brushWidth, 0.0f, 0.05f);
                            EditorGUILayout.Space();
                            pen.brushPower = EditorGUILayout.Slider("Blur Power", pen.brushPower, 0f, 1f);
                        }
                        else
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Reset", GUILayout.Width(150)))
                                {
                                    gradient = new Gradient();
                                }
                                gradient = EditorGUILayout.GradientField("", gradient);
                            }
                            straightMode = EditorGUILayout.Toggle("straightMode", straightMode);
                            EditorGUILayout.Space();
                            
                            maskAllLayers = EditorGUILayout.Toggle("maskAllLayers", maskAllLayers);
                            pen.brushStrength = EditorGUILayout.Slider("pen.brushStrength", pen.brushStrength, 0.5f, 2.5f);
                            pen.brushWidth = EditorGUILayout.Slider("pen.brushWidth", pen.brushWidth, 0.0f, 0.05f);
                            pen.brushPower = EditorGUILayout.Slider("pen.brushPower", pen.brushPower, 0f, 1f);
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            //penTool.Button(ref currentPenmode,ref brushPower,ref brushWidth, );
                            using (new EditorGUI.DisabledScope(penIndex == i))
                            {
                                if (penTools[i].Button())
                                {
                                    penIndex = i;
                                }
                            }
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Export Material"))
                        {
                            var path = EditorUtility.SaveFilePanel("Export", "Assets", meshCreater.GetMaterials()[editIndex].name+"Texture", "mat");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            currentMaterials[editIndex] = new Material(editMaterials[editIndex]);
                            // テクスチャの保存
                            currentMaterials[editIndex].mainTexture = textureCreator.SaveTexture(path.Replace(".mat", ".png"));
                            // マテリアルの保存
                            AssetDatabase.CreateAsset(currentMaterials[editIndex],FileUtil.GetProjectRelativePath(path));
                        }
                        if (GUILayout.Button("Export Texture"))
                        {
                            var path = EditorUtility.SaveFilePanel("Export", "Assets", meshCreater.GetMaterials()[editIndex].name+"Texture", "png");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            // テクスチャの保存
                            textureCreator.SaveTexture(path);
                        }
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Save"))
                        {
                            var path = EditorUtility.SaveFilePanel("Save", "Assets", meshCreater.GetMaterials()[editIndex].name+"_Layers", "layersavedata.asset");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            SaveLayerData(path);
                        }

                        if (GUILayout.Button("Load"))
                        {
                            var path = EditorUtility.OpenFilePanel("Load", "Assets",  "layersavedata.asset");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            LoadLayerData(path);
                        }
                        
                        if (GUILayout.Button("Add"))
                        {
                            var path = EditorUtility.OpenFilePanel("Load", "Assets",  "layersavedata.asset");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            LoadAddLayerData(path);
                        }
                    }

                    EditorGUILayout.Space();
                }

                if (squareMode)
                {
                    if ((int) position.height - 10 - ((int) position.width - 400) > 0)
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            avatarMonitor?.Display((int) position.width-400, (int) position.height - 10 - ((int) position.width - 400),rotateButton,moveButton);
                            texturePreviewer?.Display( (int) position.width-400, (int) position.width-400, squareMode,rotateButton,moveButton);
                        }
                    }
                    else
                    if((int) position.width - 400 - ((int) position.height - 10) > 0)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            avatarMonitor?.Display((int) position.width - 400 - ((int) position.height - 10), (int) position.height - 10,rotateButton,moveButton);
                            texturePreviewer?.Display( (int) position.height - 10, (int) position.height - 10, squareMode,rotateButton,moveButton);
                        }
                    }
                }
                else
                {
                    if ((int) position.height - 10 - ((int) position.width - 400) > 0)
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            avatarMonitor?.Display((int) position.width-400, (int) position.height/2 - 10,rotateButton,moveButton);
                            texturePreviewer?.Display( (int) position.width-400, (int) position.height/2 - 10,squareMode,rotateButton,moveButton);
                        }
                    }
                    else
                    if((int) position.width - 400 - ((int) position.height - 10) > 0)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            avatarMonitor?.Display(((int) position.width - 400)/2 - 5, (int) position.height - 5,rotateButton,moveButton);
                            texturePreviewer?.Display( ((int) position.width - 400)/2 - 5, (int) position.height - 5,squareMode,rotateButton,moveButton);
                        }
                    }
                }
                
                // 左クリックマウスを話したとき(一筆終わったとき)にUndoCashを貯める
                if (ec.isMouse && ec.button == 0)
                {
                    if (avatarMonitor.IsInDisplay(ec.mousePosition))
                    {
                        AvatarPaint();
                        if (ec.type == EventType.MouseUp)
                        {
                            textureCreator.AddCash();
                        }
                    }

                    if (texturePreviewer.IsInDisplay(ec.mousePosition))
                    {
                        TexturePaint();
                        if (ec.type == EventType.MouseUp)
                        {
                            textureCreator.AddCash();
                        }
                    }

                    if (ec.type == EventType.MouseDown)
                    {
                        isDragBuffer = true;
                    }
                    else
                    if (ec.type == EventType.MouseUp)
                    {
                        isDragBuffer = false;
                    }
                }

                if (isEnablePreview)
                {
                    Preview();
                }
                else
                {
                    texturePreviewer.PreviewClear();
                }
            }
            editMaterials[editIndex].mainTexture = isEnablePreview ? texturePreviewer?.GetTexture() : textureCreator?.GetTexture();

            // キー関係
            if (keyboardShortcut)
            {
                if (ec.type == EventType.KeyDown)
                {
                    if (ec.keyCode == KeyCode.LeftShift || ec.keyCode == KeyCode.RightShift)
                    {
                        straightMode = true;
                    }
                
                    if (ec.keyCode == KeyCode.LeftControl || ec.keyCode == KeyCode.RightControl)
                    {
                        if (pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
                        {
                            pen.extraTool = TexturePenTool.ExtraTool.StampCopy;
                        }
                    }
                }
                if (ec.type == EventType.KeyUp)
                {
                    if (ec.keyCode == KeyCode.LeftShift || ec.keyCode == KeyCode.RightShift)
                    {
                        straightMode = false;
                    }

                    if (ec.keyCode == KeyCode.LeftControl || ec.keyCode == KeyCode.RightControl)
                    {
                        if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy)
                        {
                            pen.extraTool = TexturePenTool.ExtraTool.StampPaste;
                        }
                    }
                }
            }
        }
        
        void Setup()
        {
            var rends = avatar.GetComponentsInChildren<Renderer>();
            var mcs = rends.Select(r => new MeshCreater(r)).ToArray();
            meshCreater = new MeshCreater(avatar.transform, mcs);
            meshCreater.CombineMesh();
            avatarMonitor = new AvatarMonitor(avatar.transform);
            currentMaterials = meshCreater.GetMaterials().Where(m=>m!=null).Where(m=>m.mainTexture!=null).ToArray();
            textureCreators = currentMaterials.Select(m => new TextureCreator(m.mainTexture)).ToArray();
            GenerateEditMesh();
            editMaterials = currentMaterials.Select(m => new Material(m)).ToArray();
            if (loadUVMap)
            {
                for (int i = 0; i < textureCreators.Length; i++)
                {
                    textureCreators[i].ReadUVMap(meshCreater.CreateSubMesh(i));
                    textureCreators[i].AddLayer();
                    textureCreators[i].SetLayerActive(0, true);
                    textureCreators[i].SetLayerActive(1, false);
                    textureCreators[i].SetLayerActive(2, true);
                    textureCreators[i].ChangeEditLayer(0);
                    ReplaceMaterials(currentMaterials[i], editMaterials[i]);
                }
            }
        }

        void SelectRend(int i)
        {
            editIndex = i;
            editMeshCollider.sharedMesh = meshCreater.CreateSubMesh(i);
            texturePreviewer = new TexturePreviewer(textureCreator);
            layerReorderableList = new ReorderableList(textureCreator.LayerDatas, typeof(LayerData),true,false,true,true)
            {
                elementHeight = 60,
                drawElementCallback = LayersDisplay,
                onReorderCallback = l => textureCreator.LayersUpdate(),
                onRemoveCallback = l => textureCreator.DeleateLayer(l.index),
                onAddCallback = l => textureCreator.AddLayer(),
                // onSelectCallback = l => textureCreator.ChangeEditLayer(l.index)
            };
        }

        void GenerateEditMesh()
        {
            DestroyEditMesh();
            var go = new GameObject("EditMesh");
            go.layer = 2;
            go.transform.SetParent(avatar.transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            editMeshCollider = go.AddComponent<MeshCollider>();
        }

        void DestroyEditMesh()
        {
            if (editMeshCollider) DestroyImmediate(editMeshCollider.gameObject);
        }

        private void Update()
        {
            Repaint();
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Release()
        {
            DestroyEditMesh();
            for (int i = 0; i < editMaterials.Length; i++)
            {
                ReplaceMaterials(editMaterials[i],currentMaterials[i]);
            }
        }

        private void ReplaceMaterials(Material from, Material to)
        {
            var rends = avatar.GetComponentsInChildren<Renderer>();
            foreach (var rend in rends)
            {
                var mats = new List<Material>();
                foreach (var mat in rend.sharedMaterials)
                {
                    if (mat == from)
                    {
                        mats.Add(to);
                    }
                    else
                    {
                        mats.Add(mat);
                    }
                }

                rend.sharedMaterials = mats.ToArray();
            }
        }

        void Preview()
        {
            avatarMonitor?.GetTriangle(editMeshCollider, (tri, pos) =>
            {
                var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                var delta = meshCreater.GetUVdelta(editIndex, tri, pos);
                
                if (pen.extraTool == TexturePenTool.ExtraTool.Default)
                {
                    if(straightMode)
                    {
                        if (isDragBuffer)
                        {
                            texturePreviewer.PreviewLine(straightBuffer, uv, brushColors[colorIndex], pen.brushWidth, pen.brushStrength);
                        }
                        else
                        {
                            texturePreviewer.PreviewPoint( uv, brushColors[colorIndex], pen.brushWidth, pen.brushStrength);
                        }
                    }
                    else
                    {
                        texturePreviewer.PreviewPoint( uv, brushColors[colorIndex], pen.brushWidth*delta, pen.brushStrength);
                    }
                }
                else
                if (pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
                {
                    if(straightMode)
                    {
                        if (isDragBuffer)
                        {
                            var uvmax = new Vector2(Mathf.Max(straightBuffer.x,uv.x), Mathf.Max(straightBuffer.y,uv.y));
                            var uvmin = new Vector2(Mathf.Min(straightBuffer.x,uv.x), Mathf.Min(straightBuffer.y,uv.y));
                            var uvwet = (uvmax + uvmin) * 0.5f;
                            var uvsha = uvmax - uvmin;
                            texturePreviewer.PreviewStamp(pen.icon, uvwet, uvsha, brushColors[colorIndex], pen.brushPower * Mathf.PI * 2f);
                        }
                        else
                        {
                            texturePreviewer.PreviewPoint(uv, brushColors[colorIndex], 0.005f*delta, 2.5f);
                        }
                    }
                    else
                    {
                        var axi = meshCreater.GetUVAxi(editIndex, tri, avatarMonitor.WorldSpaceCameraUp()).normalized;
                        texturePreviewer.PreviewStamp(pen.icon,uv,new Vector2(pen.brushWidth*pen.brushStrength,pen.brushWidth/pen.brushStrength)*delta, brushColors[colorIndex], -Mathf.Atan2(axi.y,axi.x)+pen.brushPower*Mathf.PI*2f);
                    }
                }
                else
                if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy)
                {
                    if(isDragBuffer)
                    {
                        texturePreviewer.PreviewBox(straightBuffer, uv, brushColors[colorIndex], 0.003f*delta, 1f);
                    }
                    else
                    {
                        texturePreviewer.PreviewPoint(uv, brushColors[colorIndex], 0.005f*delta, 2.5f);
                    }
                }
                else
                {
                    texturePreviewer.PreviewPoint(uv, brushColors[colorIndex], 0.005f*delta, 2.5f);
                }
            });
            
            {
                var uv = texturePreviewer.Touch();

                if (pen.extraTool == TexturePenTool.ExtraTool.Default)
                {
                    if(straightMode && isDragBuffer)
                    {
                        texturePreviewer.PreviewLine(straightBuffer, uv, brushColors[colorIndex], pen.brushWidth, pen.brushStrength);
                    }
                    else
                    {
                        texturePreviewer.PreviewPoint(uv, brushColors[colorIndex], pen.brushWidth, pen.brushStrength);
                    }
                }
                else
                if (pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
                {
                    if(straightMode)
                    {
                        if (isDragBuffer)
                        {
                            var uvmax = new Vector2(Mathf.Max(straightBuffer.x,uv.x), Mathf.Max(straightBuffer.y,uv.y));
                            var uvmin = new Vector2(Mathf.Min(straightBuffer.x,uv.x), Mathf.Min(straightBuffer.y,uv.y));
                            var uvwet = (uvmax + uvmin) * 0.5f;
                            var uvsha = uvmax - uvmin;
                            texturePreviewer.PreviewStamp(pen.icon, uvwet,
                                uvsha,
                                brushColors[colorIndex], pen.brushPower * Mathf.PI * 2f);
                        }
                        else
                        {
                            texturePreviewer.PreviewPoint(uv, brushColor, 0.005f, 2.5f);
                        }
                    }
                    else
                    {
                        texturePreviewer.PreviewStamp(pen.icon, uv,
                            new Vector2(pen.brushWidth * pen.brushStrength, pen.brushWidth / pen.brushStrength),
                            brushColors[colorIndex], pen.brushPower * Mathf.PI * 2f);
                    }
                }
                else
                if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy)
                {
                    if(isDragBuffer)
                    {
                        texturePreviewer.PreviewBox(straightBuffer, uv, brushColors[colorIndex], 0.003f, 1f);
                    }
                    else
                    {
                        texturePreviewer.PreviewPoint(uv, brushColors[colorIndex], 0.005f, 2.5f);
                    }
                }
                else
                {
                    texturePreviewer.PreviewPoint(uv, brushColors[colorIndex], 0.005f, 2.5f);
                }
            }
        }

        void AvatarPaint()
        {
            var ec = Event.current;
            if (ec.type == EventType.MouseDown && ec.button == drawButton)
            {
                avatarMonitor?.GetTriangle(editMeshCollider,(tri, pos) =>
                {
                    straightBuffer = meshCreater.GetUVDelta(editIndex, tri, pos);
                });
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Default)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri, pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            textureCreator.DrawLine(straightBuffer, uv, brushColor, gradient, pen.brushWidth,
                                pen.brushStrength, pen.brushPower);
                        });
                    }
                }
                else
                {
                    if (ec.type == EventType.MouseDrag && ec.button == drawButton)
                    {
                        avatarMonitor?.GetDragTriangle(editMeshCollider, (tt, pt, tf, pf) =>
                        {
                            var from = meshCreater.GetUVDelta(editIndex, tf, pf);
                            var to = meshCreater.GetUVDelta(editIndex, tt, pt);
                            var delta = meshCreater.GetUVdelta(editIndex, tt, pt);
                            // あまりにUVが飛んでたら処理をやめる 2.5fの部分はよしなに
                            if (Vector2.Distance(to, from) / Vector3.Distance(pt, pf) < delta * 1.2f)
                            {
                                textureCreator?.DrawLine(from, to, brushColor, gradient,pen.brushWidth * delta, pen.brushStrength, pen.brushPower);
                            }
                        });
                    }
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.Fill)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton && !meshCreater.IsComputeLandVertexes())
                    {
                        //var controll_vertexes = new List<int>();
                        //var mesh = editMeshCollider.sharedMesh;
                        var offset = meshCreater.GetTriangleOffset(editIndex);
                        avatarMonitor.GetTriangle(editMeshCollider, (tri,pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            meshCreater.ComputeLandVertexes(meshCreater.GetVertexIndex(tri+offset)[0], v =>
                            {
                                //if (!controll_vertexes.Contains(v)) controll_vertexes.Add(v);
                                //fillTriangle.Add(v);
                            }, vs =>
                            {
                                textureCreator.FillTriangles(
                                    editMeshCollider.sharedMesh, 
                                    meshCreater.GetTriangleList(vs).Select(t=>t-offset).ToList(),
                                    brushColor,
                                    gradient,
                                    straightBuffer,
                                    uv,
                                    (int) pen.brushStrength
                                );
                            }, false);
                        });
                    }
                }
                else
                {
                    if ((ec.type == EventType.MouseDown||ec.type == EventType.MouseDrag) &&
                        ec.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri,pos) =>
                        {
                            textureCreator.FillTriangle(editMeshCollider.sharedMesh,tri,brushColor,(int) pen.brushStrength);
                        });
                    }
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy)
            {
                if (ec.type == EventType.MouseUp && ec.button == drawButton)
                {
                    avatarMonitor?.GetTriangle(editMeshCollider, (tri, pos) =>
                    {
                        var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                        var stamp = textureCreator.GetStamp(straightBuffer, uv, maskAllLayers);
                        UpdateStampTexture(stamp);
                    });
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton && !meshCreater.IsComputeLandVertexes())
                    {
                        avatarMonitor.GetTriangle(editMeshCollider, (tri,pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            var uvmax = new Vector2(Mathf.Max(straightBuffer.x,uv.x), Mathf.Max(straightBuffer.y,uv.y));
                            var uvmin = new Vector2(Mathf.Min(straightBuffer.x,uv.x), Mathf.Min(straightBuffer.y,uv.y));
                            var uvwet = (uvmax + uvmin) * 0.5f;
                            var uvsha = uvmax - uvmin;
                            textureCreator.DrawStamp(pen.icon, uvwet,
                                uvsha,
                                brushColor, pen.brushPower * Mathf.PI * 2f);
                        });
                    }
                }
                else
                {
                    if (ec.type == EventType.MouseDown && ec.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri,pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            var delta = meshCreater.GetUVdelta(editIndex, tri, pos);
                            var axi = meshCreater.GetUVAxi(editIndex, tri, avatarMonitor.WorldSpaceCameraUp()).normalized;
                            textureCreator.DrawStamp(pen.icon,uv,new Vector2(pen.brushWidth*pen.brushStrength,pen.brushWidth/pen.brushStrength)*delta, brushColor, -Mathf.Atan2(axi.y,axi.x)+pen.brushPower*Mathf.PI*2f);
                        });
                    }
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.Gaussian)
            {
                if (ec.type == EventType.MouseDrag && ec.button == drawButton)
                {
                    avatarMonitor?.GetDragTriangle(editMeshCollider, (tt, pt, tf, pf) =>
                    {
                        var from = meshCreater.GetUVDelta(editIndex, tf, pf);
                        var to = meshCreater.GetUVDelta(editIndex, tt, pt);
                        var delta = meshCreater.GetUVdelta(editIndex, tt, pt);
                        // あまりにUVが飛んでたら処理をやめる
                        if (Vector2.Distance(to, from) / Vector3.Distance(pt, pf) < delta * 1f)
                        {
                            textureCreator?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, pen.brushPower);
                        }
                    });
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.ColorPick)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
                {
                    avatarMonitor?.GetTriangle(editMeshCollider, (tri,pos) =>
                    {
                        var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                        brushColors[colorIndex] = textureCreator.SpuitColor(uv,maskAllLayers);
                    });
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.Brush)
            {
                if (ec.type == EventType.MouseDown)
                {
                    brushBuffer = brushColors[colorIndex];
                }
                if (ec.type == EventType.MouseDrag && ec.button == drawButton)
                {
                    avatarMonitor?.GetDragTriangle(editMeshCollider, (tt, pt, tf, pf) =>
                    {
                        var from = meshCreater.GetUVDelta(editIndex, tf, pf);
                        var to = meshCreater.GetUVDelta(editIndex, tt, pt);
                        var delta = meshCreater.GetUVdelta(editIndex, tt, pt);
                        // あまりにUVが飛んでたら処理をやめる
                        if (Vector2.Distance(to, from) / Vector3.Distance(pt, pf) < delta * 1f)
                        {
                            brushBuffer = Color.Lerp(brushBuffer,textureCreator.SpuitColor(from,false),pen.brushPower);
                            textureCreator?.DrawLine(from, to, brushBuffer * gradient.Evaluate(Vector2.Distance(from, to)/pen.brushWidth),new Gradient(), pen.brushWidth, pen.brushStrength, 0.5f);
                            textureCreator?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, 0.1f);
                        }
                    });
                }
            }
        }

        void TexturePaint()
        {
            var ec = Event.current;
            if (ec.type == EventType.MouseDown && ec.button == drawButton)
            {
                straightBuffer  = texturePreviewer.Touch();
            }
            
            var uv  = texturePreviewer.Touch();
            if (pen.extraTool == TexturePenTool.ExtraTool.Default)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton)
                    {
                        textureCreator.DrawLine(straightBuffer, uv, brushColor, gradient, pen.brushWidth, pen.brushStrength, pen.brushPower);
                    }
                }
                else
                {
                    if (ec.type == EventType.MouseDrag && ec.button == drawButton)
                    {
                        texturePreviewer?.Touch((from, to) =>
                        {
                            textureCreator.DrawLine(from, to, brushColor, gradient, pen.brushWidth, pen.brushStrength, pen.brushPower);
                        });
                    }
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.Fill)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton)
                    {
                        textureCreator.FillColor(straightBuffer,uv,brushColor,gradient,pen.brushWidth,(int) pen.brushStrength,maskAllLayers);
                    }
                }
                else
                {
                    if (ec.type == EventType.MouseDown && ec.button == drawButton)
                    {
                        textureCreator.FillColor(uv,brushColor,gradient,pen.brushWidth,(int) pen.brushStrength,maskAllLayers);
                    }
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.StampCopy)
            {
                if (ec.type == EventType.MouseUp && ec.button == drawButton)
                {
                    var stamp = textureCreator.GetStamp(straightBuffer, uv, maskAllLayers);
                    UpdateStampTexture(stamp);
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.StampPaste)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton)
                    {
                        var uvmax = new Vector2(Mathf.Max(straightBuffer.x,uv.x), Mathf.Max(straightBuffer.y,uv.y));
                        var uvmin = new Vector2(Mathf.Min(straightBuffer.x,uv.x), Mathf.Min(straightBuffer.y,uv.y));
                        var uvwet = (uvmax + uvmin) * 0.5f;
                        var uvsha = uvmax - uvmin;
                        textureCreator.DrawStamp(pen.icon, uvwet,
                            uvsha,
                            brushColor, pen.brushPower * Mathf.PI * 2f);
                    }
                }
                else
                {
                    if (ec.type == EventType.MouseDown && ec.button == drawButton)
                    {
                        textureCreator.DrawStamp(pen.icon, uv,
                            new Vector2(pen.brushWidth * pen.brushStrength, pen.brushWidth / pen.brushStrength),
                            brushColor, pen.brushPower * Mathf.PI * 2f);
                    }
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.Gaussian)
            {
                if (straightMode)
                {
                    if (ec.type == EventType.MouseUp && ec.button == drawButton)
                    {
                        textureCreator?.Gaussian(straightBuffer, uv, pen.brushWidth, pen.brushStrength, pen.brushPower);
                    }
                }
                else
                {
                    if (ec.type == EventType.MouseDrag && ec.button == drawButton)
                    {
                        texturePreviewer?.Touch((from, to) =>
                        {
                            textureCreator?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, pen.brushPower);
                        });
                    }
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.ColorPick)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
                {
                    brushColors[colorIndex] = textureCreator.SpuitColor(uv,maskAllLayers);
                }
            }
            else
            if (pen.extraTool == TexturePenTool.ExtraTool.Brush)
            {
                if (ec.type == EventType.MouseDown)
                {
                    brushBuffer = brushColors[colorIndex];
                }
                if (ec.type == EventType.MouseDrag && ec.button == drawButton)
                {
                    texturePreviewer?.Touch((from, to) =>
                    {
                        brushBuffer = Color.Lerp(brushBuffer,textureCreator.SpuitColor(from,false),pen.brushPower);
                        textureCreator?.DrawLine(from, to, brushBuffer * gradient.Evaluate(Vector2.Distance(from, to)/pen.brushWidth),new Gradient(), pen.brushWidth, pen.brushStrength, 0.5f);
                        textureCreator?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, 0.1f);
                    });
                }
            }
        }

        void UpdateStampTexture(Texture tex)
        {
            if(tex==null) return;
            if (pen.icon != tex)
            {
                pen.icon = tex;
                pen.brushStrength = Mathf.Sqrt(((float)pen.icon.width / (float)textureCreator.GetEditTexture().width)/((float)pen.icon.height / (float)textureCreator.GetEditTexture().height));
                pen.brushWidth = ((float)pen.icon.width / (float)textureCreator.GetEditTexture().width) / pen.brushStrength;
            }
        }

        void LayersDisplay(Rect rect, int index, bool active, bool focused)
        {
            rect.height -= 10;
            rect.y += 5;
            var data = textureCreator.GetLayerData(index);
            {
                /*{
                    var rh = rect;
                    rh.width = rect.height/3;
                    rh.y += rh.width;
                    data.active = EditorGUI.Toggle(rh,"", data.active);
                }
                rect.x += rect.height / 3;
                rect.width -= rect.height / 3;*/
                {
                    var rh = rect;
                    rh.width = rect.width/5;
                    { // 画像左,上部
                        var r = rh;
                        r.width = rh.width / 3;
                        r.height = rh.height / 3;
                        data.active = EditorGUI.Toggle(r,"", data.active);
                        r.x += r.width;
                        EditorGUI.LabelField(r,"");
                        r.x += r.width;
                        r.height -= 2;
                        r.y += 1;
                        if (GUI.Button(r,"＋"))
                        {
                            textureCreator.CopyLayer(index);
                        }
                    }
                    rh.y += rh.height/3;
                    { // 画面左，中部
                        var r = rh;
                        r.height = rh.height / 3;
                        data.name = EditorGUI.TextField(r,data.name);
                    }
                    rh.y += rh.height/3;
                    { // 画面左，下部
                        var r = rh;
                        r.width = rh.width * 2 / 3;
                        r.height = rh.height / 3;
                        EditorGUI.LabelField(r,"isMask");
                        r.x += r.width;
                        r.width = rh.width - r.width;
                        data.isMask = EditorGUI.Toggle(r,"", data.isMask);
                    }
                }
                rect.x += rect.width/5 + 5;
                rect.width -= rect.width/5 + 5;
                {
                    var rh = rect;
                    rh.width = rh.height;
                    using (new EditorGUI.DisabledScope(data == textureCreator.GetEditLayer()))
                    {
                        if (GUI.Button(rh,data.texture))
                        {
                            textureCreator.ChangeEditLayer(index);
                        }
                    }
                }
                rect.x += rect.height + 5;
                rect.width -= rect.height + 5;
                {
                    var rh = rect;
                    rh.width = rh.width * 2 / 5;
                    rh.height = rh.height / 3;
                    data.layerMode = (BlendMode) EditorGUI.EnumPopup(rh,"", data.layerMode);
                    rh.y += rh.height;
                    data.scale = EditorGUI.Vector2Field(rh,"", data.scale);
                    rh.y += rh.height;
                    data.offset = EditorGUI.Vector2Field(rh,"", data.offset);
                }
                rect.x += rect.width * 2 / 5 + 5;
                rect.width -= rect.width * 2 / 5 + 5;
                {
                    var rlabel = rect;
                    rlabel.width = rlabel.width * 1 / 4;
                    var rprop = rect;
                    rprop.width = rprop.width * 3 / 4;
                    rprop.x += rlabel.width;
                    if (data.layerMode == BlendMode.HSV)
                    {
                        rlabel.height = rlabel.height / 3;
                        rprop.height = rprop.height / 3;
                        EditorGUI.LabelField(rlabel,"H");
                        var h = GUI.HorizontalSlider(rprop,data.settings.x, -1f, 1f);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"S");
                        var s = GUI.HorizontalSlider(rprop,data.settings.y, -1f, 1f);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"V");
                        var v = GUI.HorizontalSlider(rprop,data.settings.z, -1f, 1f);
                        
                        data.settings = new Vector4(h,s,v,data.settings.w);
                    }
                    else
                    if(data.layerMode == BlendMode.Color)
                    {
                        rlabel.height = rlabel.height / 4;
                        rprop.height = rprop.height / 4;
                        EditorGUI.LabelField(rlabel,"From");
                        data.comparison = EditorGUI.ColorField( rprop, data.comparison);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"To");
                        data.color = EditorGUI.ColorField( rprop, data.color);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"Brightness");
                        var z = GUI.HorizontalSlider(rprop,data.settings.z, 0f, 1f);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"Threshold");
                        var w = GUI.HorizontalSlider(rprop,data.settings.w, 0f, 2f);
                        
                        data.settings = new Vector4(data.settings.x,data.settings.y,z,w);
                    }
                    else
                    if(data.layerMode == BlendMode.Bloom)
                    {
                        rlabel.height = rlabel.height / 3;
                        rprop.height = rprop.height / 3;
                        var rc = rect;
                        rc.height /= 3;
                        data.color = EditorGUI.ColorField(rc,new GUIContent(), data.color, true, true, true);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"Intensity");
                        var z = GUI.HorizontalSlider(rprop,data.settings.z, 1f, 10f);
                        rlabel.y += rlabel.height;
                        rprop.y += rprop.height;
                        EditorGUI.LabelField(rlabel,"Threshold");
                        var w = GUI.HorizontalSlider(rprop,data.settings.w, 0f, 2f);
                        
                        data.settings = new Vector4(data.settings.x,data.settings.y,z,w);
                    }
                    else
                    {
                        var rc = rect;
                        rc.height /= 4;
                        data.color = EditorGUI.ColorField(rc,"", data.color);
                    }
                }
            }
        }

        public void SaveLayerData(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            LayersSaveData tsd = ScriptableObject.CreateInstance<LayersSaveData>();
            tsd.SetLayersData(textureCreator.LayerDatas);
            AssetDatabase.CreateAsset(tsd,path);
        }
        public void LoadLayerData(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            var tsd = AssetDatabase.LoadAssetAtPath<LayersSaveData>(path);
            textureCreator.SetLayers(tsd.GetLayersData());
        }
        public void LoadAddLayerData(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            var tsd = AssetDatabase.LoadAssetAtPath<LayersSaveData>(path);
            textureCreator.AddLayers(tsd.GetLayersData());
        }
        
        public class TexturePenTool
        {
            public Texture icon;
            private string name;

            public ExtraTool extraTool { get; set; }
            public float brushStrength { get; set; }
            public float brushWidth { get; set; }
            public float brushPower { get; set; }
            public Color? brushColor { get; set; }

            public TexturePenTool(string guid, string n, ExtraTool e, float s,float w, float p, Color? c)
            {
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    var i = AssetUtility.LoadAssetAtGuid<Texture>(guid);
                    icon = i;
                }
                name = n;
                extraTool = e;
                brushWidth = w;
                brushStrength = s;
                brushPower = p;
                brushColor = c;
            }
            public TexturePenTool(Texture i, string n, ExtraTool e, float s, float w, float p, Color? c)
            {
                icon = i;
                name = n;
                extraTool = e;
                brushWidth = w;
                brushStrength = s;
                brushPower = p;
                brushColor = c;
            }

            public bool Button(int width = 60,int height = 60)
            {
                if (icon)
                {
                    if (GUILayout.Button(icon, GUILayout.Width(width), GUILayout.Height(height)))
                    {
                        return true;
                    }
                }
                else
                {
                    if (GUILayout.Button(name, GUILayout.Width(width), GUILayout.Height(height)))
                    {
                        return true;
                    }
                }

                return false;
            }
            
            public enum ExtraTool
            {
                Default,
                StampCopy,
                StampPaste,
                Fill,
                ColorPick,
                Gaussian,
                Brush
            }
        }
    }
}