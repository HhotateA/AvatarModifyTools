using HhotateA.AvatarModifyTools.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HhotateA.AvatarModifyTools.TextureModifyTool
{
    public class TextureModifyTool : EditorWindow
    {
        [MenuItem("Window/HhotateA/にゃんにゃんアバターペインター(TextureModifyTool)")]
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
        private MeshCreater meshCreater;
        private TexturePainter[] texturePainters;
        
        private TexturePainter texturePainter
        {
            get
            {
                if (texturePainters != null)
                {
                    if (editIndex != -1 && editIndex < texturePainters.Length)
                    {
                        return texturePainters[editIndex];
                    }
                }

                return null;
            }
        }
        private int editIndex = -1;
        
        // 各種オプション
        private bool loadUVMap = true;
        private bool straightMode = false; // 定規モード
        private Vector2 straightBuffer = Vector2.zero;
        private bool squareMode = true; // 正方形固定(TextureWindow)
        private bool maskAllLayers = true;

        private Color brushBuffer;
        
        //private TexturePenTool.ExtraTool currentPenmode;
        
        // 表示切替用バッファー
        private Vector2 rendsScroll = Vector2.zero;
        private Vector2 layersScroll = Vector2.zero;
        private Texture2D newLayer;
        private bool deleateLayerActive = false;

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
                        new TexturePenTool("bcad5870702a5b843944d470f70a2b7a","Pen",TexturePenTool.ExtraTool.Default,2.5f,0.03f, 1f,null), 
                        new TexturePenTool("8ad291433c7f5354eacb47ad671c2c38","Splay",TexturePenTool.ExtraTool.Default,0.05f,0.04f, 0f,null), 
                        new TexturePenTool("b4686199bb51a574db19610a6e0fd2fa","Eraser",TexturePenTool.ExtraTool.Default,2f,0.02f, 1f,Color.clear), 
                        new TexturePenTool("b9567d145a3193046939b1cb6b5eea56","Stamp",TexturePenTool.ExtraTool.Stamp,1f,0.1f, 0f,null), 
                        new TexturePenTool("44a2288e1ff487947b5c586fe23cbb95","Fill",TexturePenTool.ExtraTool.Fill,1f,0.03f, 0f,null), 
                        new TexturePenTool("6cadb4cec0b66a449af35ab94a1085db","Gaussian",TexturePenTool.ExtraTool.Gaussian,2f,0.01f, 0f,Color.clear), 
                        new TexturePenTool("61a3819337bf0184b84c23f1838e049f","ColorPick",TexturePenTool.ExtraTool.ColorPick,0,0, 0,null),
                    };
                }

                return _penTools;
            }
        }
        
        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(380)))
                {
                    EditorGUILayout.Space();
                    avatar = EditorGUILayout.ObjectField("", avatar, typeof(GameObject), true) as GameObject;
                    if (GUILayout.Button("Setup"))
                    {
                        Release();
                        var rends = avatar.GetComponentsInChildren<Renderer>();
                        var mcs = rends.Select(r => new MeshCreater(r)).ToArray();
                        meshCreater = new MeshCreater(avatar.transform, mcs);
                        meshCreater.CombineMesh();
                        avatarMonitor = new AvatarMonitor(avatar.transform);
                        texturePainters = meshCreater.GetMaterials()
                            .Select(m => new TexturePainter(m.mainTexture)).ToArray();
                        GenerateEditMesh();
                        currentMaterials = meshCreater.GetMaterials();
                        editMaterials = currentMaterials.Select(m => new Material(m)).ToArray();
                        if (loadUVMap)
                        {
                            for (int i = 0; i < texturePainters.Length; i++)
                            {
                                if (meshCreater.GetMaterials()[i].mainTexture != null)
                                {
                                    texturePainters[i].ReadUVMap(meshCreater.CreateSubMesh(i));
                                    texturePainters[i].AddLayer();
                                    texturePainters[i].SetLayerActive(0, true);
                                    texturePainters[i].SetLayerActive(1, false);
                                    texturePainters[i].SetLayerActive(2, true);
                                    texturePainters[i].ChangeEditLayer(2);
                                    ReplaceMaterials(currentMaterials[i], editMaterials[i]);
                                }
                            }
                        }
                    }
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();

                    if (meshCreater == null)
                    {
                        loadUVMap = EditorGUILayout.Toggle("Load UVMap", loadUVMap);
                        EditorGUILayout.Space();
                        return;
                    }
                    
                    rendsScroll = EditorGUILayout.BeginScrollView(rendsScroll);
                    
                    using (new EditorGUILayout.VerticalScope())
                    {
                        for (int i = 0; i < meshCreater.GetMaterials().Length; i++)
                        {
                            if (meshCreater.GetMaterials()[i].mainTexture == null) continue;
                            // Todo この辺のボタンの画像か
                            //if (GUILayout.Button(new GUIContent(meshCreater.GetMaterials()[i].name,meshCreater.GetMaterials()[i].mainTexture),GUILayout.Height(75),GUILayout.Width(300)))
                            if (GUILayout.Button(meshCreater.GetMaterials()[i].name))
                            {
                                editIndex = i;
                                editMeshCollider.sharedMesh = meshCreater.CreateSubMesh(i);
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space();
                    loadUVMap = EditorGUILayout.Toggle("Load UVMap", loadUVMap);
                    EditorGUILayout.Space();
                    squareMode = EditorGUILayout.Toggle("Texture Square Mode", squareMode);
                    EditorGUILayout.Space();
                    deleateLayerActive = EditorGUILayout.Toggle("Deleate Layer", deleateLayerActive);
                    EditorGUILayout.Space();
                    
                    if (texturePainter == null) return;

                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        LayerButtons();
                    }
                    
                    EditorGUILayout.LabelField("");

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(!texturePainter.CanUndo()))
                        {
                            if (GUILayout.Button("Undo"))
                            {
                                texturePainter.UndoEditTexture();
                            }
                        }

                        using (new EditorGUI.DisabledScope(!texturePainter.CanRedo()))
                        {
                            if (GUILayout.Button("Redo"))
                            {
                                texturePainter.RedoEditTexture();
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
                        if (pen.extraTool == TexturePenTool.ExtraTool.Stamp)
                        {
                            EditorGUILayout.Space();
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                //straightMode = EditorGUILayout.Toggle("Copy Texture", straightMode);
                                using (new EditorGUI.DisabledScope(straightMode))
                                {
                                    if (GUILayout.Button("Copy"))
                                    {
                                        straightMode = true;
                                    }
                                }
                                using (new EditorGUI.DisabledScope(!straightMode))
                                {
                                    if (GUILayout.Button("Paste"))
                                    {
                                        straightMode = false;
                                    }
                                }
                            }

                            if (straightMode)
                            {
                                maskAllLayers = EditorGUILayout.Toggle("Copy All Layers", maskAllLayers);
                            }
                            else
                            {
                                EditorGUILayout.LabelField("");
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

                    if (GUILayout.Button("Export"))
                    {
                        var path = EditorUtility.SaveFilePanel("Export", "Assets", meshCreater.GetMaterials()[editIndex].name+"Texture", "png");
                        if (string.IsNullOrWhiteSpace(path)) return;
                        currentMaterials[editIndex] = new Material(editMaterials[editIndex]);
                        // テクスチャの保存
                        currentMaterials[editIndex].mainTexture = texturePainter.SaveTexture(path);
                        // マテリアルの保存
                        AssetDatabase.CreateAsset(currentMaterials[editIndex],FileUtil.GetProjectRelativePath(path).Replace(".png", ".mat"));
                        //editTexture[editIndex] = texturePainter.SaveTexture(path);
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Save"))
                        {
                            var path = EditorUtility.SaveFilePanel("Save", "Assets", meshCreater.GetMaterials()[editIndex].name+"_Layers", "layersavedata.asset");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            texturePainter.SaveLayerData(path);
                        }

                        if (GUILayout.Button("Load"))
                        {
                            var path = EditorUtility.OpenFilePanel("Load", "Assets",  "layersavedata.asset");
                            if (string.IsNullOrWhiteSpace(path)) return;
                            texturePainter.LoadLayerData(path);
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
                            texturePainter?.Display( (int) position.width-400, (int) position.width-400, squareMode,rotateButton,moveButton);
                        }
                    }
                    else
                    if((int) position.width - 400 - ((int) position.height - 10) > 0)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            avatarMonitor?.Display((int) position.width - 400 - ((int) position.height - 10), (int) position.height - 10,rotateButton,moveButton);
                            texturePainter?.Display( (int) position.height - 10, (int) position.height - 10, squareMode,rotateButton,moveButton);
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
                            texturePainter?.Display( (int) position.width-400, (int) position.height/2 - 10,squareMode,rotateButton,moveButton);
                        }
                    }
                    else
                    if((int) position.width - 400 - ((int) position.height - 10) > 0)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            avatarMonitor?.Display(((int) position.width - 400)/2 - 5, (int) position.height - 5,rotateButton,moveButton);
                            texturePainter?.Display( ((int) position.width - 400)/2 - 5, (int) position.height - 5,squareMode,rotateButton,moveButton);
                        }
                    }
                }
                
                // 左クリックマウスを話したとき(一筆終わったとき)にUndoCashを貯める
                if (Event.current.isMouse && Event.current.button == 0)
                {
                    if (avatarMonitor.IsInDisplay(Event.current.mousePosition))
                    {
                        AvatarPaint();
                        if (Event.current.type == EventType.MouseUp)
                        {
                            texturePainter.AddCash();
                        }
                    }

                    if (texturePainter.IsInDisplay(Event.current.mousePosition))
                    {
                        TexturePaint();
                        if (Event.current.type == EventType.MouseUp)
                        {
                            texturePainter.AddCash();
                        }
                    }
                }
            }
            editMaterials[editIndex].mainTexture = texturePainter?.GetTexture();
        }
        
        public bool LayerButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                newLayer = (Texture2D)EditorGUILayout.ObjectField(newLayer, typeof(Texture2D),
                    false, GUILayout.Width(60), GUILayout.Height(60));

                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUI.DisabledScope(texturePainter?.LayerCount() > 16))
                    {
                        if (GUILayout.Button("Add Layer", GUILayout.Height(25)))
                        {
                            texturePainter.AddLayer(newLayer);
                            newLayer = null;
                        }

                        GUILayout.Space(5);

                        if (GUILayout.Button("Add Mask", GUILayout.Height(25)))
                        {
                            texturePainter.AddMask(brushColor,gradient);
                        }
                    }
                    /*if (GUILayout.Button("Deleate Layer", GUILayout.Width(300)))
                    {
                        deleateLayerActive = !deleateLayerActive;
                    }*/
                }
            }
            EditorGUILayout.LabelField("");
            layersScroll = EditorGUILayout.BeginScrollView(layersScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
            //layersScroll = GUI.BeginScrollView(GUILayoutUtility.GetRect(300, 300),layersScroll,new Rect(0, 0, 300, 300));
            texturePainter.LayerButtons(deleateLayerActive);
            EditorGUILayout.EndScrollView();

            return false;
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

        void AvatarPaint()
        {
            if (straightMode)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor?.GetTriangle(editMeshCollider,(tri, pos) =>
                    {
                        straightBuffer = meshCreater.GetUVDelta(editIndex, tri, pos);
                    });
                }
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Default)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri, pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            texturePainter.DrawLine(straightBuffer, uv, brushColor, gradient, pen.brushWidth,
                                pen.brushStrength, pen.brushPower);
                        });
                    }
                }
                else
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == drawButton)
                    {
                        avatarMonitor?.GetDragTriangle(editMeshCollider, (tt, pt, tf, pf) =>
                        {
                            var from = meshCreater.GetUVDelta(editIndex, tf, pf);
                            var to = meshCreater.GetUVDelta(editIndex, tt, pt);
                            var delta = meshCreater.GetUVdelta(editIndex, tt, pt);
                            // あまりにUVが飛んでたら処理をやめる 2.5fの部分はよしなに
                            if (Vector2.Distance(to, from) / Vector3.Distance(pt, pf) < delta * 1.2f)
                            {
                                texturePainter?.DrawLine(from, to, brushColor, gradient,pen.brushWidth * delta, pen.brushStrength, pen.brushPower);
                            }
                        });
                    }
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.Fill)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton && !meshCreater.IsComputeLandVertexes())
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
                                texturePainter.FillTriangles(
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
                    if ((Event.current.type == EventType.MouseDown||Event.current.type == EventType.MouseDrag) &&
                        Event.current.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri,pos) =>
                        {
                            texturePainter.FillTriangle(editMeshCollider.sharedMesh,tri,brushColor,(int) pen.brushStrength);
                        });
                    }
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.Stamp)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri, pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            var stamp = texturePainter.GetStamp(straightBuffer, uv, maskAllLayers);
                            UpdateStampTexture(stamp);
                        });
                    }
                }
                else
                {
                    if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                    {
                        avatarMonitor?.GetTriangle(editMeshCollider, (tri,pos) =>
                        {
                            var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                            var delta = meshCreater.GetUVdelta(editIndex, tri, pos);
                            var axi = meshCreater.GetUVAxi(editIndex, tri, avatarMonitor.WorldSpaceCameraUp()).normalized;
                            texturePainter.DrawStamp(pen.icon,uv,new Vector2(pen.brushWidth*pen.brushStrength,pen.brushWidth/pen.brushStrength)*delta, brushColor, -Mathf.Atan2(axi.y,axi.x)+pen.brushPower*Mathf.PI*2f);
                        });
                    }
                }
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Gaussian)
            {
                if (Event.current.type == EventType.MouseDrag && Event.current.button == drawButton)
                {
                    avatarMonitor?.GetDragTriangle(editMeshCollider, (tt, pt, tf, pf) =>
                    {
                        var from = meshCreater.GetUVDelta(editIndex, tf, pf);
                        var to = meshCreater.GetUVDelta(editIndex, tt, pt);
                        var delta = meshCreater.GetUVdelta(editIndex, tt, pt);
                        // あまりにUVが飛んでたら処理をやめる
                        if (Vector2.Distance(to, from) / Vector3.Distance(pt, pf) < delta * 1f)
                        {
                            texturePainter?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, pen.brushPower);
                        }
                    });
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.ColorPick)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor?.GetTriangle(editMeshCollider, (tri,pos) =>
                    {
                        var uv = meshCreater.GetUVDelta(editIndex, tri, pos);
                        brushColors[colorIndex] = texturePainter.SpuitColor(uv,maskAllLayers);
                    });
                }
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Brush)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    brushBuffer = brushColors[colorIndex];
                }
                if (Event.current.type == EventType.MouseDrag && Event.current.button == drawButton)
                {
                    avatarMonitor?.GetDragTriangle(editMeshCollider, (tt, pt, tf, pf) =>
                    {
                        var from = meshCreater.GetUVDelta(editIndex, tf, pf);
                        var to = meshCreater.GetUVDelta(editIndex, tt, pt);
                        var delta = meshCreater.GetUVdelta(editIndex, tt, pt);
                        // あまりにUVが飛んでたら処理をやめる
                        if (Vector2.Distance(to, from) / Vector3.Distance(pt, pf) < delta * 1f)
                        {
                            brushBuffer = Color.Lerp(brushBuffer,texturePainter.SpuitColor(from,false),pen.brushPower);
                            texturePainter?.DrawLine(from, to, brushBuffer * gradient.Evaluate(Vector2.Distance(from, to)/pen.brushWidth),new Gradient(), pen.brushWidth, pen.brushStrength, 0.5f);
                            texturePainter?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, 0.1f);
                        }
                    });
                }
            }
        }

        void TexturePaint()
        {
            if (straightMode)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    straightBuffer  = texturePainter.Touch();
                }
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Default)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton)
                    {
                        var uv  = texturePainter.Touch();
                        texturePainter.DrawLine(straightBuffer, uv, brushColor, gradient, pen.brushWidth, pen.brushStrength, pen.brushPower);
                    }
                }
                else
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == drawButton)
                    {
                        texturePainter?.Touch((from, to) =>
                        {
                            texturePainter.DrawLine(from, to, brushColor, gradient, pen.brushWidth, pen.brushStrength, pen.brushPower);
                        });
                    }
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.Fill)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton)
                    {
                        var uv  = texturePainter.Touch();
                        texturePainter.FillColor(straightBuffer,uv,brushColor,gradient,pen.brushWidth,(int) pen.brushStrength,maskAllLayers);
                    }
                }
                else
                {
                    if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                    {
                        var uv  = texturePainter.Touch();
                        texturePainter.FillColor(uv,brushColor,gradient,pen.brushWidth,(int) pen.brushStrength,maskAllLayers);
                    }
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.Stamp)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton)
                    {
                        var uv  = texturePainter.Touch();
                        var stamp = texturePainter.GetStamp(straightBuffer, uv, maskAllLayers);
                        UpdateStampTexture(stamp);
                    }
                }
                else
                {
                    if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                    {
                        var uv  = texturePainter.Touch();
                        texturePainter.DrawStamp(pen.icon,uv,new Vector2(pen.brushWidth*pen.brushStrength,pen.brushWidth/pen.brushStrength), brushColor ,pen.brushPower*Mathf.PI*2f);
                    }
                }
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Gaussian)
            {
                if (straightMode)
                {
                    if (Event.current.type == EventType.MouseUp && Event.current.button == drawButton)
                    {
                        var uv  = texturePainter.Touch();
                        texturePainter?.Gaussian(straightBuffer, uv, pen.brushWidth, pen.brushStrength, pen.brushPower);
                    }
                }
                else
                {
                    if (Event.current.type == EventType.MouseDrag && Event.current.button == drawButton)
                    {
                        texturePainter?.Touch((from, to) =>
                        {
                            texturePainter?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, pen.brushPower);
                        });
                    }
                }
            }

            if (pen.extraTool == TexturePenTool.ExtraTool.ColorPick)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    var uv  = texturePainter.Touch();
                    brushColors[colorIndex] = texturePainter.SpuitColor(uv,maskAllLayers);
                }
            }
            
            if (pen.extraTool == TexturePenTool.ExtraTool.Brush)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    brushBuffer = brushColors[colorIndex];
                }
                if (Event.current.type == EventType.MouseDrag && Event.current.button == drawButton)
                {
                    texturePainter?.Touch((from, to) =>
                    {
                        brushBuffer = Color.Lerp(brushBuffer,texturePainter.SpuitColor(from,false),pen.brushPower);
                        texturePainter?.DrawLine(from, to, brushBuffer * gradient.Evaluate(Vector2.Distance(from, to)/pen.brushWidth),new Gradient(), pen.brushWidth, pen.brushStrength, 0.5f);
                        texturePainter?.Gaussian(from, to, pen.brushWidth, pen.brushStrength, 0.1f);
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
                pen.brushStrength = Mathf.Sqrt(((float)pen.icon.width / (float)texturePainter.GetEditTexture().width)/((float)pen.icon.height / (float)texturePainter.GetEditTexture().height));
                pen.brushWidth = ((float)pen.icon.width / (float)texturePainter.GetEditTexture().width) / pen.brushStrength;
            }
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
                    var i = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));
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
                Stamp,
                Fill,
                ColorPick,
                Gaussian,
                Brush
            }
        }
    }
}