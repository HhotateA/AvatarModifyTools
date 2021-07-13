using HhotateA.AvatarModifyTools.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace HhotateA.AvatarModifyTools.MeshModifyTool
{
    public class MeshModifyTool : EditorWindow
    {
        [MenuItem("Window/HhotateA/にゃんにゃんメッシュエディター(MeshModifyTool)")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MeshModifyTool>();
            wnd.titleContent = new GUIContent("にゃんにゃんメッシュエディター");
        }

        // ボタン設定
        private int drawButton = 0;
        private int rotateButton = 1;
        private int moveButton = 2;
        
        // 改造するメッシュのルートオブジェクト
        private GameObject avatar;
        // avatar配下のMeshオブジェクト
        private Renderer[] rends;
        // rendsごとのMeshsCreater
        private MeshCreater[] meshsCreaters;
        // rendsにもともと入っていたメッシュの保存
        private Mesh[] defaultMeshs;
        // 現在編集中のrendsのindex
        private int editIndex = -1;
        // 現在編集中のMeshsCreaterを取得する便利関数
        MeshCreater editMeshCreater
        {
            get
            {
                if (meshsCreaters == null) return null;
                if (editIndex == -1 || editIndex >= meshsCreaters.Length) return null;
                return meshsCreaters[editIndex];
            }
        }

        // MergeBone機能用
        private Animator originHuman;
        private Animator targetHuman;
        private MergeBoneMode mergeBoneMode;
        
        // Remesh機能用
        private int triangleCount = 0;

        private SkinnedMeshRenderer weightOrigin;
        
        // カメラ表示用補助クラス
        AvatarMonitor avatarMonitor;
        private const int previewLayer = 2;
        private Vector2 rendsScroll = Vector2.zero;
        
        // スカルプトモード，ペン設定
        private MeshPenTool.ExtraTool penMode = MeshPenTool.ExtraTool.Default;
        private float brushPower = 0.001f;
        private float brushWidth = 0.03f;
        private float brushStrength = 1f;
        private bool xMirror, yMirror, zMirror;
        bool selectMode => (penMode == MeshPenTool.ExtraTool.SelectLand || 
                            penMode == MeshPenTool.ExtraTool.UnSelectLand ||
                            penMode == MeshPenTool.ExtraTool.SelectVertex ||
                            penMode == MeshPenTool.ExtraTool.UnSelectVertex);
        
        // 頂点編集モード用，操作点
        private GameObject controllPoint_from;
        private GameObject controllPoint_to;
        
        // ワイヤーフレーム表示用オブジェクト
        private GameObject controllMesh_edit;
        private MeshCollider controllMesh_editCollider;
        private MeshFilter controllMesh_editFilter;
        // メッシュ編集モード用，頂点リスト
        private List<int> controll_vertexes = new List<int>();
        // メッシュ編集モード用オブジェクト
        private GameObject controllMesh_select;
        private MeshFilter controllMesh_selectFilter;

        // メッシュ編集モード用Transform
        private Vector3 transformPosition;
        private Vector3 transformRotation;
        private Vector3 transformScale;
        
        // UV変形用
        private UVViewer uvViewer;
        private Vector4 uvTexelSize;
        private Material activeMaterial;

        // 保存するBlendShapeの名前
        private string blendShapeName = "BlendShapeName";

        // 各種設定項目
        private bool activeExperimentalAlpha = false;
        private bool activeExperimentalBeta = false;
        private bool isSelectVertex = true;
        private bool isRandomizeVertex = false;
        private bool isRealtimeTransform = false;
        private bool isSelectOverlappingVertexes = true;
        private bool isVertexRemove = false;
        private bool isSaveAll = false;
        
        // 不安定項目を非有効化する設定
        private bool disableNotRecommend = true;
        
        // 編集ツールプリセット
        private MeshPenTool[] _penTools;
        private MeshPenTool[] penTools
        {
            get
            {
                if (_penTools == null)
                {
                    _penTools = new MeshPenTool[4]
                    {
                        new MeshPenTool("9a511d19d82d2f847b4945e967929e53","Smooth",MeshPenTool.ExtraTool.Default,2f,0.003f,0.03f), 
                        new MeshPenTool("e8e8182176f763e48850311a752c0e02","Liner",MeshPenTool.ExtraTool.Default,1f,-0.01f,0.03f), 
                        new MeshPenTool("37b755ed53afcfc408a733b5f4580816","Constant",MeshPenTool.ExtraTool.Default,10f,null,0f), 
                        new MeshPenTool("00da790a00e523643a5e648c07452823","Detail",MeshPenTool.ExtraTool.DetailMode,null,null,0f), 
                    };
                }

                return _penTools;
            }
        }

        // 拡張編集ツールプリセット
        private MeshPenTool[] _extraTools;
        MeshPenTool[] extraTools
        {
            get
            {
                if (_extraTools == null)
                {
                    _extraTools = new MeshPenTool[4]
                    {
                        new MeshPenTool("b3cf85d36df40664caa41453c91c4a10","SelectLand",MeshPenTool.ExtraTool.SelectLand,null,null,0f),
                        new MeshPenTool("0a05c27f8b748874f8c8a001841555cd","UnSelectLand",MeshPenTool.ExtraTool.UnSelectLand,null,null,0f),
                        new MeshPenTool("f1ed57a871c76eb458353aec0d255979","SelectVertex",MeshPenTool.ExtraTool.SelectVertex,null,null,0f),
                        new MeshPenTool("69b440a5753b0864d9ab6f9591898c87","UnSelectVertex",MeshPenTool.ExtraTool.UnSelectVertex,null,null,0f),
                    };
                }
                return _extraTools;
            }
        }
        
        // 拡張編集ツールプリセット
        private MeshPenTool[] _betaTools;
        MeshPenTool[] betaTools
        {
            get
            {
                if (_betaTools == null)
                {
                    _betaTools = new MeshPenTool[4]
                    {
                        new MeshPenTool("","WeightCopy",MeshPenTool.ExtraTool.WeightCopy,null,null,0f),
                        new MeshPenTool("","Eraser",MeshPenTool.ExtraTool.TriangleEraser,null,null,0f),
                        new MeshPenTool("","Decimate",MeshPenTool.ExtraTool.Decimate,null,null,0f),
                        new MeshPenTool("","Subdivision",MeshPenTool.ExtraTool.Subdivision,null,null,0f),
                    };
                }
                return _betaTools;
            }
        }

        private int casheCount = -1;
        
        // 最終選択頂点のデータ
        private bool displayRawData => activeExperimentalBeta;
        Vector3 rawPosition = Vector3.zero;
        Vector3 rawNormal = Vector3.up;
        Vector3 rawTangent = Vector3.right;
        Color rawColor = Color.white;
        private Vector2[] rawUVs = Enumerable.Range(0, 8).Select(_ => Vector2.zero).ToArray();
        KeyValuePair<int,float>[] rawWeights = new KeyValuePair<int,float>[4];

        private int[] rawIDs = Enumerable.Range(0, 3).ToArray();


        /// <summary>
        /// 表示部，実装は置かないこと
        /// </summary>
        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // ウィンドウ左側
                using (new EditorGUILayout.VerticalScope())
                {
                    avatar = EditorGUILayout.ObjectField("", avatar, typeof(GameObject), true) as GameObject;
                    if (GUILayout.Button("Setup"))
                    {
                        Setup(avatar);
                    }

                    if (rends == null) return;
                    
                    EditorGUILayout.Space();

                    rendsScroll = EditorGUILayout.BeginScrollView(rendsScroll);
                    using (new EditorGUILayout.VerticalScope( GUI.skin.box ))
                    {
                        for(int i=0;i<rends.Length;i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                rends[i].gameObject.SetActive(EditorGUILayout.Toggle("",
                                    rends[i].gameObject.activeSelf,GUILayout.Width(20)));
                                using (new EditorGUI.DisabledScope(editIndex == i))
                                {
                                    if (GUILayout.Button(rends[i].name, GUILayout.Width(250)))
                                    {
                                        rends[i].gameObject.SetActive(true);
                                        SelectMeshCreater(i);
                                    }
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                    
                    // 選択中オブジェクトが非アクティブになったら，選択解除
                    if (editIndex != -1)
                    {
                        if (!rends[editIndex].gameObject.activeSelf)
                        {
                            SelectMeshCreater(-1);
                        }
                    }
                    
                    EditorGUILayout.Space();

                    activeExperimentalAlpha = EditorGUILayout.Toggle("ActiveExperimental", activeExperimentalAlpha);
                    if (activeExperimentalAlpha)
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            isSelectVertex = EditorGUILayout.Toggle("SelectVertexMode", isSelectVertex);
                            isRealtimeTransform = EditorGUILayout.Toggle("RealtimeTransform", isRealtimeTransform, GUILayout.Width(200));
                            // isRemoveAsBlendShape = EditorGUILayout.Toggle("DeleteAsBlendShape", isRemoveAsBlendShape, GUILayout.Width(155));
                            isSelectOverlappingVertexes = EditorGUILayout.Toggle("SelectOverlapping",isSelectOverlappingVertexes, GUILayout.Width(155));

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (activeExperimentalAlpha)
                                {
                                    foreach (var penTool in extraTools)
                                    {
                                        if (penTool.Button(ref penMode, ref brushPower, ref brushWidth, ref brushStrength))
                                        {
                                            if (penMode != MeshPenTool.ExtraTool.DetailMode) DestroyControllPoint();
                                            if (!selectMode) ReloadMesh(false);
                                        }
                                    }
                                }
                            }

                            if (selectMode)
                            {
                                using (new EditorGUI.DisabledScope(editMeshCreater?.IsComputeLandVertexes() ?? false))
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        if (GUILayout.Button("SelectAll"))
                                        {
                                            ResetSelect();
                                            RevertSelect(editMeshCreater.VertexsCount() - 1);
                                            ResetSelectTransform(editMeshCreater);
                                            ReloadMesh(false, editMeshCreater, controll_vertexes);
                                        }

                                        if (GUILayout.Button("SelectNone"))
                                        {
                                            ResetSelect();
                                            ResetSelectTransform(editMeshCreater);
                                            ReloadMesh(false, editMeshCreater, controll_vertexes);
                                        }

                                        if (GUILayout.Button("RevertSelect"))
                                        {
                                            RevertSelect(editMeshCreater.VertexsCount() - 1);
                                            ResetSelectTransform(editMeshCreater);
                                            ReloadMesh(false, editMeshCreater, controll_vertexes);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    EditorGUILayout.Space();
                    
                    activeExperimentalBeta = EditorGUILayout.Toggle("ActiveExperimentalBeta", activeExperimentalBeta);
                    if (activeExperimentalBeta)
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            isVertexRemove = EditorGUILayout.Toggle("DeleteVertex", isVertexRemove);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUI.DisabledScope(editMeshCreater == null && !isSaveAll))
                                {
                                    GUILayout.Label("MergeBones", GUILayout.Width(100));
                                    if (GUILayout.Button("HumanBone", GUILayout.Width(90)))
                                    {
                                        SaveAll(() =>
                                        {
                                            if (rends[editIndex].GetType() == typeof(SkinnedMeshRenderer))
                                            {
                                                var rend = rends[editIndex] as SkinnedMeshRenderer;
                                                DisableNonHumanBone(rend);
                                                DeleateDisableBone();
                                                ReloadMesh(false);
                                            }
                                        });
                                    }

                                    if (GUILayout.Button("ActiveBones", GUILayout.Width(90)))
                                    {
                                        SaveAll(() =>
                                        {
                                            if (rends[editIndex].GetType() == typeof(SkinnedMeshRenderer))
                                            {
                                                var rend = rends[editIndex] as SkinnedMeshRenderer;
                                                DeleateDisableBone();
                                                ReloadMesh(false);
                                            }
                                        });
                                    }
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUI.DisabledScope(originHuman == null))
                                {
                                    targetHuman =
                                        EditorGUILayout.ObjectField("", targetHuman, typeof(Animator), true,
                                                GUILayout.Width(100)) as
                                            Animator;
                                    using (new EditorGUI.DisabledScope(targetHuman == null))
                                    {
                                        mergeBoneMode =
                                            (MergeBoneMode) EditorGUILayout.EnumPopup("", mergeBoneMode,
                                                GUILayout.Width(55));
                                        using (new EditorGUI.DisabledScope(editMeshCreater == null && !isSaveAll))
                                        {
                                            if (GUILayout.Button("ChangeBone", GUILayout.Width(125)))
                                            {
                                                if (mergeBoneMode == MergeBoneMode.merge)
                                                {
                                                    SaveAll(MergeBone);
                                                }
                                                else if (mergeBoneMode == MergeBoneMode.combinate)
                                                {
                                                    SaveAll(CombineBone);
                                                }
                                                else if (mergeBoneMode == MergeBoneMode.constraint)
                                                {
                                                    ConstraintBone();
                                                }
                                                else
                                                {
                                                    MoveBone();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label("CombineMesh", GUILayout.Width(100));
                                if (GUILayout.Button("ActiveMesh",GUILayout.Width(90)))
                                {
                                    CombineMesh();
                                }
                                using (new EditorGUI.DisabledScope(editMeshCreater==null && !isSaveAll))
                                {
                                    if (GUILayout.Button("Material",GUILayout.Width(90)))
                                    {
                                        SaveAll(CombineMaterial);
                                    }
                                }
                            }
                            
                            EditorGUILayout.Space();
                            
                            EditorGUILayout.LabelField("NotRecommended",GUILayout.Width(120));
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                foreach (var betaTool in betaTools)
                                {
                                    if (betaTool.Button(ref penMode, ref brushPower, ref brushWidth, ref brushStrength))
                                    {
                                        if (penMode != MeshPenTool.ExtraTool.DetailMode) DestroyControllPoint();
                                        if (penMode == MeshPenTool.ExtraTool.Decimate) DecimateSelect();
                                        if (penMode == MeshPenTool.ExtraTool.Subdivision) SubdivisionSelect();
                                    }
                                }
                            }
                            
                            using (new EditorGUI.DisabledScope(disableNotRecommend))
                            {
                                // あんまよくない
                                isRandomizeVertex = EditorGUILayout.Toggle("RandomizeVertex", isRandomizeVertex);

                                // 動かない（重すぎる）
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    weightOrigin = EditorGUILayout.ObjectField("", weightOrigin,
                                            typeof(SkinnedMeshRenderer), true, GUILayout.Width(155)) as
                                        SkinnedMeshRenderer;
                                    
                                    if (GUILayout.Button("CopyBoneWeight",GUILayout.Width(125)))
                                    {
                                        if (weightOrigin)
                                        {
                                            editMeshCreater.CopyBoneWeight(new SkinnedMeshRenderer[1] {weightOrigin});

                                        }
                                    }
                                }
                                
                                // 精度悪い，重い
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    triangleCount = EditorGUILayout.IntField("", triangleCount,GUILayout.Width(155));
                                    if (GUILayout.Button("Decimate",GUILayout.Width(125)))
                                    {
                                        Decimate();
                                    }
                                }
                            }
                        }
                    }
                    EditorGUILayout.Space();
                    

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(penMode == MeshPenTool.ExtraTool.TriangleEraser))
                        {
                            using (new EditorGUI.DisabledScope(!editMeshCreater?.CanUndo() ?? false))
                            {
                                if (GUILayout.Button("Undo"))
                                {
                                    DestroyControllPoint();
                                    editMeshCreater?.UndoCaches();
                                    ReloadMesh(false,editMeshCreater,controll_vertexes);
                                }
                            }

                            using (new EditorGUI.DisabledScope(!editMeshCreater?.CanRedo() ?? false))
                            {
                                if (GUILayout.Button("Redo"))
                                {
                                    DestroyControllPoint();
                                    editMeshCreater?.RedoCaches();
                                    ReloadMesh(false,editMeshCreater,controll_vertexes);
                                }
                            }
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        foreach (var penTool in penTools)
                        {
                            if (penTool.Button(ref penMode, ref brushPower, ref brushWidth, ref brushStrength))
                            {
                                if (penMode != MeshPenTool.ExtraTool.DetailMode) DestroyControllPoint();
                                if (!selectMode) ReloadMesh(false);
                            }
                        }
                    }

                    if(penMode == MeshPenTool.ExtraTool.Default)
                    {
                        brushPower = EditorGUILayout.Slider("BrushPower",brushPower, -0.03f, 0.03f);
                        brushWidth = EditorGUILayout.Slider("BrushWidth",brushWidth, 0f, 0.1f);
                        brushStrength = EditorGUILayout.Slider("BrushStrength",brushStrength, 0, 10);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Mirror(x,y,z)",GUILayout.Width(120));
                            xMirror = EditorGUILayout.Toggle("", xMirror,GUILayout.Width(50));
                            yMirror = EditorGUILayout.Toggle("", yMirror,GUILayout.Width(50));
                            zMirror = EditorGUILayout.Toggle("", zMirror,GUILayout.Width(50));
                        }
                    }
                    else
                    if (penMode == MeshPenTool.ExtraTool.DetailMode || penMode == MeshPenTool.ExtraTool.Subdivision)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            Vector3 p;
                            if (controllPoint_to)
                            {
                                p = EditorGUILayout.Vector3Field("", controllPoint_to.transform.localPosition);
                                if (p != controllPoint_to.transform.localPosition)
                                {
                                    controllPoint_to.transform.localPosition = p;
                                }
                            }
                            else
                            {
                                p = EditorGUILayout.Vector3Field("", Vector3.zero);
                            }

                            if (GUILayout.Button("Transform"))
                            {
                                if (controllPoint_from && controllPoint_to)
                                {
                                    TransfromMeshMirror(editMeshCreater,controllPoint_from.transform.localPosition,controllPoint_to.transform.localPosition);
                                    GenerateControllPoint(editMeshCreater,controllPoint_to.transform.localPosition);
                                }
                            }

                            if (isRealtimeTransform)
                            {
                                if (controllPoint_from && controllPoint_to)
                                {
                                    if ( Vector3.Distance(controllPoint_from.transform.position,
                                        controllPoint_to.transform.position) > 0.001f)
                                    {
                                        TransfromMeshMirror(editMeshCreater,controllPoint_from.transform.localPosition,controllPoint_to.transform.localPosition,false);
                                        controllPoint_from.transform.localPosition = controllPoint_to.transform.localPosition;
                                        casheCount = 100;
                                    }
                                    else
                                    {
                                        casheCount--;
                                    }

                                    if (casheCount == 0)
                                    {
                                        TransfromMeshMirror(editMeshCreater,controllPoint_from.transform.localPosition,controllPoint_to.transform.localPosition,true);
                                    }
                                }
                            }
                        }
                        brushWidth = EditorGUILayout.Slider("BrushWidth",brushWidth, 0f, 0.1f);
                        brushStrength = EditorGUILayout.Slider("BrushStrength",brushStrength, 0, 10);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Mirror(x,y,z)",GUILayout.Width(120));
                            xMirror = EditorGUILayout.Toggle("", xMirror,GUILayout.Width(50));
                            yMirror = EditorGUILayout.Toggle("", yMirror,GUILayout.Width(50));
                            zMirror = EditorGUILayout.Toggle("", zMirror,GUILayout.Width(50));
                        }
                    }
                    else
                    if (selectMode)
                    {
                        using (new EditorGUI.DisabledScope(editMeshCreater?.IsComputeLandVertexes() ?? false))
                        {
                            if (isRealtimeTransform && controllMesh_select)
                            {
                                controllMesh_select.transform.localPosition = EditorGUILayout.Vector3Field("Position", controllMesh_select.transform.localPosition);
                                controllMesh_select.transform.localRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", controllMesh_select.transform.localEulerAngles));
                                controllMesh_select.transform.localScale = EditorGUILayout.Vector3Field("Scale", controllMesh_select.transform.localScale);
                            }
                            else
                            {
                                transformPosition = EditorGUILayout.Vector3Field("Position", transformPosition);
                                transformRotation = EditorGUILayout.Vector3Field("Rotation", transformRotation);
                                transformScale = EditorGUILayout.Vector3Field("Scale", transformScale);
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("Transform"))
                                {
                                    TransfomControllMesh();
                                }
                                if (GUILayout.Button("Copy"))
                                {
                                    CopyControllMesh();
                                }
                                if (GUILayout.Button("Deleate"))
                                {
                                    DeleateControllMesh();
                                }
                            }
                        }
                    }
                    
                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        blendShapeName = EditorGUILayout.TextField("", blendShapeName,GUILayout.Width(155));

                        using (new EditorGUI.DisabledScope(!editMeshCreater?.CanUndo() ?? true))
                        {
                            if (GUILayout.Button("SaveAsBlendShape",GUILayout.Width(125)))
                            {
                                editMeshCreater.SaveAsBlendshape(blendShapeName);
                                ReloadMesh(false);
                                editMeshCreater.ResetCaches();
                            }
                        }
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("SaveAll",GUILayout.Width(50));
                        isSaveAll = EditorGUILayout.Toggle("", isSaveAll,GUILayout.Width(50));
                        using (new EditorGUI.DisabledScope(editMeshCreater == null && !isSaveAll))
                        {
                            if (GUILayout.Button("Save"))
                            {
                                SaveAll(()=>Save());
                            }
                        }
                    }
                    
                    EditorGUILayout.Space();
                    
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    if (avatarMonitor != null)
                    {
                        var avatarMonitorWidth = displayRawData ? 1100 : 300;
                        avatarMonitor.Display( (int) position.width-avatarMonitorWidth, (int) position.height-10,rotateButton,moveButton);
                        if (editIndex != -1)
                        {
                            AvatarMonitorTouch(editMeshCreater);
                        }
                    }
                }
                
                if (displayRawData)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            rawPosition = EditorGUILayout.Vector3Field("Position", rawPosition);
                            rawNormal = EditorGUILayout.Vector3Field("Normal", rawNormal);
                            rawTangent = EditorGUILayout.Vector3Field("Tangent", rawTangent);
                            rawColor = EditorGUILayout.ColorField("Color", rawColor);
                            for (int i = 0; i < rawUVs.Length; i++)
                            {
                                rawUVs[i] = EditorGUILayout.Vector2Field("UV" + i, rawUVs[i]);
                            }

                            for (int i = 0; i < rawWeights.Length; i++)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    rawWeights[i] = new KeyValuePair<int, float>(
                                        EditorGUILayout.IntField("BoneIndex" + i, rawWeights[i].Key),
                                        EditorGUILayout.FloatField("Weight", rawWeights[i].Value));
                                }
                            }

                            if (GUILayout.Button("UpdateData"))
                            {
                                SetRawData();
                            }
                        }

                        EditorGUILayout.Space();

                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                for (int i = 0; i < rawIDs.Length; i++)
                                {
                                    rawIDs[i] = EditorGUILayout.IntField("", rawIDs[i]);
                                }
                            }

                            if (GUILayout.Button("GenerateTriangle"))
                            {
                                GenerateTriangle();
                            }
                        }

                        EditorGUILayout.Space();
                        
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("UV_ST");
                                uvTexelSize = EditorGUILayout.Vector4Field("", uvTexelSize);
                            }
                            using (new EditorGUI.DisabledScope(uvTexelSize.x==0f || uvTexelSize.y==0f))
                            {
                                if (GUILayout.Button("TransformUV"))
                                {
                                    editMeshCreater.TransformUV( new Vector2(1f/uvTexelSize.x,1f/uvTexelSize.y),new Vector2(-uvTexelSize.z,-uvTexelSize.w),controll_vertexes);
                                    ReloadMesh(false);
                                    uvTexelSize = new Vector4(1f,1f,0f,0f);
                                    uvViewer?.ReadUVMap(editMeshCreater.CreateEditMesh(controll_vertexes));
                                }
                            }
                            uvViewer?.UVTextureSize(new Vector2(uvTexelSize.x,uvTexelSize.y),new Vector2(uvTexelSize.z,uvTexelSize.w));
                            uvViewer?.Display(700,700);
                            if (GUILayout.Button("Divided 4"))
                            {
                                editMeshCreater.TransformUV(new Vector2(0.5f,0.5f),new Vector2(0f,0f));
                                ReloadMesh(false);
                                uvTexelSize = new Vector4(1f,1f,0f,0f);
                                uvViewer?.BaseTextureSize(new Vector2(2f,2f),new Vector2(0f,0f));
                                uvViewer?.ReadUVMap(editMeshCreater.CreateEditMesh(controll_vertexes));
                                activeMaterial.SetTextureScale("_MainTex",new Vector2(2f,2f));
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 毎フレーム更新する
        /// </summary>
        private void Update () {
            Repaint();
        }

        /// <summary>
        /// Window閉じるとき処理，メッシュの後片付け
        /// </summary>
        private void OnDestroy()
        {
            DestroyControllMeshes();
            for(int i=0;i<rends.Length;i++)
            {
                rends[i].SetMesh(defaultMeshs[i]);
            }
            avatarMonitor?.Release();
            avatarMonitor = null;
        }

        void Setup(GameObject anim)
        {
            DestroyControllMeshes();
            rends = anim.transform.GetComponentsInChildren<Renderer>();
            defaultMeshs = rends.Select(m => m.GetMesh()).ToArray();
            meshsCreaters = rends.Select(m => new MeshCreater(m)).ToArray();
            
            if(avatarMonitor!=null) avatarMonitor.Release();
            avatarMonitor = new AvatarMonitor(anim.transform);
            
            originHuman = anim.GetComponent<Animator>();
            if(originHuman != null) originHuman = originHuman.isHuman ? originHuman : null;
            editIndex = -1;
        }

        void AddRend(Renderer rend)
        {
            int i = rends.Length;
            rends = rends.Append(rend).ToArray();
            defaultMeshs = defaultMeshs.Append(rend.GetMesh()).ToArray();
            meshsCreaters = meshsCreaters.Append(new MeshCreater(rend)).ToArray();
            SelectMeshCreater(i);
        }

        void SelectMeshCreater(int i)
        {
            DestroyControllMeshes();
            editIndex = i;
            if(editIndex!=-1) ReloadMesh(false,editMeshCreater,controll_vertexes);
        }

        void SaveAll(Action saveAction)
        {
            if (isSaveAll)
            {
                int max = rends.Length;
                for (int i = 0; i < max; i++)
                {
                    SelectMeshCreater(i);
                    saveAction?.Invoke();
                }
            }
            else
            {
                saveAction?.Invoke();
            }
        }

        bool Save(Action onSave = null)
        {
            var path = EditorUtility.SaveFilePanel("Save", "Assets", rends[editIndex].name+"_Edit", "mesh");
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path).Split('.')[0];
                            
            DestroyEditMesh();
            DestroyControllPoint();
            
            onSave?.Invoke();
            
            defaultMeshs[editIndex] = SaveMeshCreater(meshsCreaters[editIndex],dir,file,rends[editIndex]);;
            return true;
        }
        
        /// <summary>
        /// MeshCreaterのメッシュをファイルに保存する
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="dir"></param>
        /// <param name="file"></param>
        /// <param name="rend"></param>
        /// <returns></returns>
        Mesh SaveMeshCreater(MeshCreater mc,string dir,string file,Renderer rend = null,Transform root = null)
        {
            if (isRandomizeVertex)
            {
                var mat = new Material(Shader.Find("HhotateA/Decryption"));
                var p = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Shader.Find("HhotateA/Decryption")));
                File.WriteAllText(Path.Combine(p,"Keys.cginc"), mc.Encryption(mat));
                mc.MaterialAtlas(Path.Combine(dir,file));
                mc.CombineMesh();
            }

            mc.Create(false);
            var m = mc.Save(Path.Combine(dir, file + ".mesh"));

            if (rend != null)
            {
                rend.SetMesh(m);

                rend.sharedMaterials = mc.GetMaterials();
            }

            return m;
        }

        void CombineMesh()
        {
            var path = EditorUtility.SaveFilePanel("Save", "Assets", "CombineMesh", "mesh");
            if (string.IsNullOrWhiteSpace(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path).Split('.')[0];
            
            if (rends.FirstOrDefault(r => r.name == file))
            {
                for (int i = 0; i < 999; i++)
                {
                    var n = file + "_" + i;
                    if (rends.FirstOrDefault(r => r.name == n) == null)
                    {
                        file = n;
                        break;
                    }
                }
            }
            var meshCreater = new MeshCreater(avatar.transform,meshsCreaters);
            var m = meshCreater.Save(Path.Combine(dir, file + ".mesh"));
                
            var sm = meshCreater.ToSkinMesh(file,avatar.transform);
            
            sm.transform.position = rends[0].transform.position;
            sm.transform.rotation = rends[0].transform.rotation;
            
            var smsm = sm.GetComponent<SkinnedMeshRenderer>();
            smsm.SetMesh(m);
            // editmeshCreaterの切り替え
            AddRend(smsm);
        }

        void CombineMaterial()
        {
            var path = EditorUtility.SaveFilePanel("Save", "Assets", "CombineMaterial", "png");
            if (string.IsNullOrWhiteSpace(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path).Split('.')[0];
            
            if (rends.FirstOrDefault(r => r.name == file))
            {
                for (int i = 0; i < 999; i++)
                {
                    var n = file + "_" + i;
                    if (rends.FirstOrDefault(r => r.name == n) == null)
                    {
                        file = n;
                        break;
                    }
                }
            }
            
            editMeshCreater.MaterialAtlas(Path.Combine(dir,file));
            var m = editMeshCreater.Save(Path.Combine(dir, file + ".mesh"));
            var sm = editMeshCreater.ToSkinMesh(file,avatar.transform);
            
            sm.transform.position = rends[0].transform.position;
            sm.transform.rotation = rends[0].transform.rotation;
            
            var smsm = sm.GetComponent<SkinnedMeshRenderer>();
            smsm.SetMesh(m);
            // editmeshCreaterの切り替え
            AddRend(smsm);
        }
        
        /// <summary>
        /// カメラ表示，インタラクト処理
        /// </summary>
        /// <param name="mc"></param>
        void AvatarMonitorTouch(MeshCreater mc)
        {
            if (penMode == MeshPenTool.ExtraTool.Default)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetControllPoint(GetEditMeshCollider(), isSelectVertex,
                        h => { TransfromMeshMirror(mc, h); });
                }
            }
            else
            if (penMode == MeshPenTool.ExtraTool.DetailMode)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetControllPoint(GetEditMeshCollider(), isSelectVertex,
                        h => { GenerateControllPoint(mc, h); });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.TriangleEraser)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), h =>
                    {
                        mc.RemoveTriangle(h);
                        ReloadMesh(true, mc);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.SelectLand)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), h =>
                    {
                        mc.ComputeLandVertexes(mc.GetVertexIndex(h)[0], v =>
                        {
                            if (!controll_vertexes.Contains(v)) controll_vertexes.Add(v);
                        }, _ =>
                        {
                            ResetSelectTransform(mc);
                            ReloadMesh(false, mc, controll_vertexes);
                        }, isSelectOverlappingVertexes && activeExperimentalAlpha);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.UnSelectLand)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), h =>
                    {
                        mc.ComputeLandVertexes(mc.GetVertexIndex(h)[0], v =>
                        {
                            if (controll_vertexes.Contains(v)) controll_vertexes.Remove(v);
                        }, _ =>
                        {
                            ResetSelectTransform(mc);
                            ReloadMesh(false, mc, controll_vertexes);
                        }, isSelectOverlappingVertexes && activeExperimentalAlpha);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.SelectVertex)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), h =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var v = mc.GetVertexIndex(h)[i];
                            if (!controll_vertexes.Contains(v)) controll_vertexes.Add(v);
                        }

                        ResetSelectTransform(mc);
                        ReloadMesh(false, mc, controll_vertexes);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.UnSelectVertex)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), h =>
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var v = mc.GetVertexIndex(h)[i];
                            if (controll_vertexes.Contains(v)) controll_vertexes.Remove(v);
                        }

                        ResetSelectTransform(mc);
                        ReloadMesh(false, mc, controll_vertexes);
                    });
                }
            }
            else
            if (penMode == MeshPenTool.ExtraTool.WeightCopy)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetVertex(GetEditMeshCollider(), (h, p) =>
                    {
                        GenerateControllPoint(mc, p);
                        mc.WeightCopy(controll_vertexes, h);
                        //penMode = PenTool.ExtraTool.Default;
                        ReloadMesh(false, mc, controll_vertexes);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.Decimate)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), h =>
                    {
                        mc.Decimate(h);
                        ReloadMesh(true, mc);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.Subdivision)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), (h, p) =>
                    {
                        GenerateControllPoint(mc, p);
                        mc.Subdivision(h, p, isSelectVertex);
                        ReloadMesh(true, mc);
                    });
                }
            }

            if (displayRawData)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == drawButton)
                {
                    avatarMonitor.GetVertex(GetEditMeshCollider(), (h, p) =>
                    {
                        GetRawData(h);
                        InitializeUVViewer(h);
                    });
                }
            }
        }

        void InitializeUVViewer(int v)
        {
            editMeshCreater.GetTriangleList(new List<int>(v), (s, l) =>
            {
                var m = editMeshCreater.GetMaterials()[s];
                if (uvViewer == null)
                {
                    uvViewer = new UVViewer(m);
                }
                else
                if (activeMaterial != m)
                {
                    uvViewer.Release();
                    uvViewer = new UVViewer(m);
                }
                activeMaterial = m;
                
                uvViewer?.ReadUVMap(editMeshCreater.CreateEditMesh(controll_vertexes));
            },3);

        }

        /// <summary>
        /// 頂点編集モード用コントロールポイントの作成
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="pos"></param>
        void GenerateControllPoint(MeshCreater mc,Vector3 pos,bool twoControll = true)
        {
            DestroyControllPoint();
            
            controllPoint_from = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            controllPoint_from.name = "EditVertex_From";
            controllPoint_from.transform.localScale = new Vector3(0.009f,0.009f,0.009f);
            controllPoint_from.transform.SetParent(mc.RendBone);
            controllPoint_from.transform.localPosition = pos;
            controllPoint_from.hideFlags = HideFlags.HideAndDontSave;
            var mat_from = new Material(Shader.Find("Unlit/Color"));
            mat_from.color = Color.blue;
            controllPoint_from.GetComponent<Renderer>().sharedMaterial = mat_from;

            if (twoControll)
            {                        
                controllPoint_to = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                controllPoint_to.name = "EditVertex_To";
                controllPoint_to.transform.localScale = new Vector3(0.01f,0.01f,0.01f);
                controllPoint_to.transform.SetParent(mc.RendBone);
                controllPoint_to.transform.localPosition = pos;
                controllPoint_to.hideFlags = HideFlags.HideAndDontSave;
                var mat_to = new Material(Shader.Find("Unlit/Color"));
                mat_to.color = Color.red;
                controllPoint_to.GetComponent<Renderer>().sharedMaterial = mat_to;

                if (isRealtimeTransform)
                {
                    controllPoint_to.hideFlags = HideFlags.DontSave;
                    Selection.activeGameObject = controllPoint_to;
                }
            }
        }
        
        /// <summary>
        /// 頂点編集モード用コントロールポイントの削除
        /// </summary>
        void DestroyControllPoint()
        {
            if(controllPoint_from) DestroyImmediate(controllPoint_from);
            if(controllPoint_to) DestroyImmediate(controllPoint_to);
        }
        
        /// <summary>
        /// Mirrorを適応した変形
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="from"></param>
        /// <param name="to_null">nullならカメラ方向に変形</param>
        /// <param name="cashes"></param>
        void TransfromMeshMirror(MeshCreater mc,Vector3 from, Vector3? to_null = null,bool cashes = true)
        {
            var to = to_null ?? mc.CalculateLocalVec(-avatarMonitor.WorldSpaceCameraVec());
            bool isVec = to_null == null;
            
            TransformMesh(mc,from,to,isVec);
            
            if (xMirror)
            {
                var from_m = new Vector3(-from.x,from.y,from.z);
                var to_m = new Vector3(-to.x,to.y,to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            if (yMirror)
            {
                var from_m = new Vector3(from.x,-from.y,from.z);
                var to_m = new Vector3(to.x,-to.y,to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            if (zMirror)
            {
                var from_m = new Vector3(from.x,from.y,-from.z);
                var to_m = new Vector3(to.x,to.y,-to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            if (xMirror&&yMirror)
            {
                var from_m = new Vector3(-from.x,-from.y,from.z);
                var to_m = new Vector3(-to.x,-to.y,to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            if (yMirror&&zMirror)
            {
                var from_m = new Vector3(from.x,-from.y,-from.z);
                var to_m = new Vector3(to.x,-to.y,-to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            if (zMirror&&xMirror)
            {
                var from_m = new Vector3(-from.x,from.y,-from.z);
                var to_m = new Vector3(-to.x,to.y,-to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            if (xMirror&&yMirror&&zMirror)
            {
                var from_m = new Vector3(-from.x,-from.y,-from.z);
                var to_m = new Vector3(-to.x,-to.y,-to.z);
                TransformMesh(mc,from_m,to_m,isVec);
            }
            
            ReloadMesh(cashes,mc);
        }
        
        /// <summary>
        /// 頂点編集
        /// </summary>
        /// <param name="mc"></param>
        /// <param name="from"></param>
        /// <param name="to">isVecならローカル方向</param>
        /// <param name="isVec"></param>
        void TransformMesh(MeshCreater mc,Vector3 from,Vector3 to,bool isVec)
        {
            if (isVec)
            {
                mc.TransformMesh(from, to, brushPower, brushWidth, brushStrength);
            }
            else
            {
                mc.TransformMesh(from, to, brushWidth, brushStrength);
            }
        }

        /// <summary>
        /// メッシュ編集，選択解除
        /// </summary>
        void ResetSelect()
        {
            controll_vertexes = new List<int>();
        }

        /// <summary>
        /// メッシュ編集，選択反転
        /// </summary>
        /// <param name="max"></param>
        void RevertSelect(int max)
        {
            controll_vertexes = Enumerable.Range(0, max).Where(n => !controll_vertexes.Contains(n)).ToList();
        }

        /// <summary>
        /// UITransformリセット
        /// </summary>
        /// <param name="mc"></param>
        void ResetSelectTransform(MeshCreater mc)
        {
            transformPosition = mc.ComputeCenterPoint(controll_vertexes);
            transformRotation = Vector3.zero;
            transformScale = Vector3.one;
            uvTexelSize = new Vector4(1f,1f,0f,0f);
        }
        
        /// <summary>
        /// メッシュ編集，メッシュ移動
        /// </summary>
        void TransfomControllMesh()
        {
            if (isRealtimeTransform)
            {
                editMeshCreater?.TransformVertexes(
                    controll_vertexes, 
                    controllMesh_select.transform.localPosition,
                    controllMesh_select.transform.localEulerAngles,
                    controllMesh_select.transform.localScale);
                controllMesh_select.transform.localPosition = Vector3.zero;
                controllMesh_select.transform.localRotation = Quaternion.identity;
                controllMesh_select.transform.localScale = Vector3.one;
            }
            else
            {
                editMeshCreater?.TransformVertexes(
                    controll_vertexes, 
                    transformPosition,
                    transformRotation,
                    transformScale);
                transformPosition = editMeshCreater.ComputeCenterPoint(controll_vertexes);
                transformRotation = Vector3.zero;
                transformScale = Vector3.one;
            }
            ReloadMesh(true,editMeshCreater,controll_vertexes);
        }
        
        /// <summary>
        /// メッシュ編集，メッシュコピーと移動
        /// </summary>
        void CopyControllMesh()
        {
            var vertexBefor = editMeshCreater?.VertexsCount() ?? 0;
            editMeshCreater?.CopyVertexes(controll_vertexes, false);
            var vertexAfter = editMeshCreater?.VertexsCount() ?? 0;
            controll_vertexes = Enumerable.Range(vertexBefor, vertexAfter - vertexBefor).ToList();
            TransfomControllMesh();
        }
        
        /// <summary>
        /// メッシュ編集，メッシュ削除
        /// </summary>
        void DeleateControllMesh()
        {
            editMeshCreater?.RemoveVertexesTriangles(controll_vertexes,false,isVertexRemove);
            ResetSelect();
            ReloadMesh( isVertexRemove,editMeshCreater,controll_vertexes);
        }
        
        /// <summary>
        /// 編集用メッシュの一括削除
        /// </summary>
        void DestroyControllMeshes()
        {
            DestroyEditMesh();
            DestroySelectMesh();
            DestroyControllPoint();
            ResetSelect();
        }
        
        /// <summary>
        /// メッシュの更新
        /// </summary>
        /// <param name="cashes"></param>
        /// <param name="mc"></param>
        /// <param name="verts"></param>
        void ReloadMesh(bool cashes = true,MeshCreater mc = null,List<int> verts = null,Renderer rend = null)
        {
            if (mc==null) mc = editMeshCreater;
            if (verts == null) verts = controll_vertexes;
            if (rend == null) rend = rends?[editIndex];
            rend.SetMesh(mc.Create(false));

            if (selectMode)
            {
                SetEditMesh(mc,CreateEditMesh(mc, verts));
            }
            else
            if (verts.Count == 0)
            {
                SetEditMesh(mc,mc.GetMesh());
            }
            else
            {
                ResetSelect();
                SetEditMesh(mc,CreateEditMesh(mc, verts));
            }
            
            if(cashes) mc.AddCaches();
            
            if (isRealtimeTransform && verts.Count > 0 && selectMode)
            {
                var wp = mc.ComputeCenterPoint(verts);
                SetSelectMesh(mc,CreateSelectMesh(mc, verts,wp));
                controllMesh_select.transform.localPosition = wp;
            }
            else
            {
                DestroySelectMesh();
            }

            triangleCount = mc.TrianglesCount();

        }
        
        /// <summary>
        /// ワイヤーフレームのメッシュコライダー取得
        /// </summary>
        /// <returns></returns>
        MeshCollider GetEditMeshCollider()
        {
            return controllMesh_editCollider;
        }

        /// <summary>
        /// ワイヤーフレーム用メッシュの更新
        /// </summary>
        GameObject SetEditMesh(MeshCreater mc,Mesh mesh)
        {
            if (controllMesh_edit == null)
            {
                controllMesh_edit = mc.ToMesh("EditMesh",true);
                controllMesh_edit.hideFlags = HideFlags.HideAndDontSave;
                controllMesh_edit.layer = previewLayer;
                controllMesh_editCollider = controllMesh_edit.GetComponent<MeshCollider>();
                controllMesh_editFilter = controllMesh_edit.GetComponent<MeshFilter>();
                var rend = controllMesh_edit.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("HhotateA/OverlayWireFrame"));
                mat.SetFloat("_ZTest",4);
                rend.sharedMaterials = Enumerable.Range(0,rend.sharedMaterials.Length).
                    Select(_=> mat).ToArray();
            }
            
            controllMesh_editFilter.sharedMesh = mesh;
            controllMesh_editCollider.sharedMesh = controllMesh_editFilter.sharedMesh;

            return controllMesh_edit;
        }
        
        /// <summary>
        /// ワイヤーフレーム用メッシュの作成
        /// </summary>
        Mesh CreateEditMesh(MeshCreater mc,List<int> verts)
        {
            if (verts.Count == 0)
            {
                return mc.GetMesh();
            }
            else
            {
                return mc.CreateEditMesh(verts,null, mc.GetMesh());
            }
        }

        /// <summary>
        /// ワイヤーフレーム用メッシュの削除
        /// </summary>
        void DestroyEditMesh(MeshCreater mc = null)
        {
            if (controllMesh_edit)
            {
                DestroyImmediate(controllMesh_edit);
            }
        }

        /// <summary>
        /// メッシュ編集モード用メッシュの更新
        /// </summary>
        GameObject SetSelectMesh(MeshCreater mc,Mesh mesh)
        {
            if (controllMesh_select == null)
            {
                controllMesh_select = mc.ToMesh("SelectMesh", false);
                controllMesh_select.hideFlags = HideFlags.DontSave;
                controllMesh_selectFilter = controllMesh_select.GetComponent<MeshFilter>();
                var rend = controllMesh_select.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("HhotateA/OverlayWireFrame"));
                mat.SetFloat("_ZTest",2);
                rend.sharedMaterials = Enumerable.Range(0,rend.sharedMaterials.Length).
                    Select(_=> mat).ToArray();
            }
            controllMesh_selectFilter.sharedMesh = mesh;
            if (isRealtimeTransform)
            {
                Selection.activeGameObject = controllMesh_select;
            }
            return controllMesh_select;
        }

        /// <summary>
        /// メッシュ編集モード用メッシュの作成
        /// </summary>
        Mesh CreateSelectMesh(MeshCreater mc,List<int> verts,Vector3? wp)
        {
            if (controllMesh_select)
            {
                var mesh = mc.CreateEditMesh(null, verts, controllMesh_selectFilter.sharedMesh,-wp);
            
                return mesh;
            }
            else
            {
                var mesh = mc.CreateEditMesh(null, verts,null,-wp);
            
                return mesh;
            }
        }
        
        /// <summary>
        /// メッシュ編集モード用メッシュの削除
        /// </summary>
        void DestroySelectMesh()
        {
            if (controllMesh_select)
            {
                DestroyImmediate(controllMesh_select);
            }
        }

        /// <summary>
        /// ポリゴン数削減
        /// たぶん動かない
        /// </summary>
        void Decimate()
        {
            if (editMeshCreater.TrianglesCount() != triangleCount)
            {
                int decimate = editMeshCreater.TrianglesCount() /
                               (editMeshCreater.TrianglesCount() - triangleCount);
                int decimateIndex = decimate;
                while (editMeshCreater.TrianglesCount() > triangleCount)
                {
                    if (decimateIndex > editMeshCreater.TrianglesCount()) decimateIndex = decimate;
                    editMeshCreater.Decimate(decimateIndex);
                    decimateIndex += decimate;
                }

                ReloadMesh(false);
            }
        }
        
        void DecimateSelect()
        {
            var tris = editMeshCreater.GetTriangleList(controll_vertexes);
            for (int i = 0; i < tris.Count; i++)
            {
                int index = tris[i] - i * 4;
                if(index>0) editMeshCreater.Decimate(index);
            }
            ReloadMesh(false);
        }

        void SubdivisionSelect()
        {
            var tris = editMeshCreater.GetTriangleList(controll_vertexes);
            for (int i = 0; i < tris.Count; i++)
            {
                editMeshCreater.Subdivision(tris[i]-i);
            }
            ReloadMesh(false);
        }

        enum MergeBoneMode
        {
            combinate,
            merge,
            constraint,
            //move
        }

        /// <summary>
        /// ボーンの参照先を変更する
        /// </summary>
        void MergeBone()
        {
            // 先ずセーブ情報取得
            var path = EditorUtility.SaveFilePanel("Save", "Assets", rends[editIndex].name+"_Edit", "mesh");
            if (string.IsNullOrWhiteSpace(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path).Split('.')[0];
                
            var sm = editMeshCreater.ToSkinMesh(file,avatar.transform);
            
            sm.transform.position = rends[0].transform.position;
            sm.transform.rotation = rends[0].transform.rotation;
            
            // editMeshCreaterの切り替え
            var smsm = sm.GetComponent<SkinnedMeshRenderer>();
            AddRend(smsm);
            
            // ここからボーンの参照
            smsm.bones = editMeshCreater.ChangeBones(targetHuman,originHuman,true);
            smsm.rootBone = editMeshCreater.RootBone;
            
            var m = editMeshCreater.Save(Path.Combine(dir, file + ".mesh"));
            defaultMeshs[editIndex] = m;
            rends[editIndex].SetMesh(m);
            sm.transform.SetParent(targetHuman.transform);
        }
        
        /// <summary>
        /// ボーンの参照先を，コンストレイントボーンに変更する
        /// </summary>
        void CombineBone()
        {
            // 先ずセーブ情報取得
            var path = EditorUtility.SaveFilePanel("Save", "Assets", rends[editIndex].name+"_Edit", "mesh");
            if (string.IsNullOrWhiteSpace(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path).Split('.')[0];
            
            var sm = editMeshCreater.ToSkinMesh(file,avatar.transform);
            
            sm.transform.position = rends[0].transform.position;
            sm.transform.rotation = rends[0].transform.rotation;
            
            // editMeshCreaterの切り替え
            var smsm = sm.GetComponent<SkinnedMeshRenderer>();
            AddRend(smsm);
            
            // ここからボーンの参照
            var t = targetHuman.GetHumanBones();
            var o = originHuman.GetHumanBones();
            var p = targetHuman.transform.Find(avatar.name + "_bones") ??
                new GameObject(avatar.name + "_bones").transform;
            p.SetParent(targetHuman.transform);
            p.localPosition = Vector3.zero;
            p.localRotation = Quaternion.identity;
            p.localScale = Vector3.one;
            
            var boneTable = new Dictionary<Transform,Transform>();
            
            smsm.bones = editMeshCreater.ChangeBones(targetHuman,originHuman, 
                true,b =>
                {
                    var bp = b.parent;
                    if (t.Contains(bp))
                    {
                        var bpobject = p.Find(bp.name);
                        if (bpobject == null)
                        {
                            bpobject = new GameObject(bp.name).transform;
                            bpobject.SetPositionAndRotation(bp.position,bp.rotation);
                            bpobject.SetParent(p);
                            var c = bpobject.gameObject.AddComponent<ParentConstraint>();
                            c.AddSource(new ConstraintSource()
                            {
                                sourceTransform = bp,
                                weight = 1f
                            });
                            c.constraintActive = true;
                        }
                        b.SetParent(bpobject,false);
                    }
                }, rb =>
                {
                    editMeshCreater.RootBone = rb;
                });

            smsm.bones = editMeshCreater.ApplyBoneTable(boneTable).ToArray();
            smsm.rootBone = editMeshCreater.RootBone;

            var m = editMeshCreater.Save(Path.Combine(dir, file + ".mesh"));
            defaultMeshs[editIndex] = m;
            rends[editIndex].SetMesh(m);
            sm.transform.SetParent(targetHuman.transform);
        }
        
        /// <summary>
        /// コンストレイントボーンを設定する
        /// </summary>
        void ConstraintBone()
        {
            var t = targetHuman.GetHumanBones();
            var o = originHuman.GetHumanBones();
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] && o[i])
                {
                    var s = new ConstraintSource()
                    {
                        sourceTransform = t[i],
                        weight = 1f
                    };
                    var c = o[i].gameObject.AddComponent<ParentConstraint>();
                    c.AddSource(s);
                }
            }
        }
        
        /// <summary>
        /// ボーンを参照先に移動する
        /// </summary>
        void MoveBone()
        {
            var t = targetHuman.GetHumanBones();
            var o = originHuman.GetHumanBones();
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] && o[i])
                {
                    o[i].transform.SetPositionAndRotation(t[i].position,o[i].rotation);
                }
            }
        }

        void GetTableInChild(ref Dictionary<Transform,Transform> table,Transform bone,Transform target)
        {
            Debug.Log(bone.transform.childCount+"child"+bone.name);
            foreach (Transform cb in bone.transform)
            {
                var ct = target.Find(cb.name);
                if (ct != null)
                {
                    table.Add(cb,ct);
                    GetTableInChild(ref table, cb, ct);
                }
            }
        }

        /// <summary>
        /// HumanBone以外を参照から外す
        /// </summary>
        /// <param name="rend"></param>
        void DisableNonHumanBone(SkinnedMeshRenderer rend)
        {
            if (originHuman != null)
            {
                var humanBones = originHuman.GetHumanBones();
                foreach (var bone in rend.bones)
                {
                    if(!humanBones.Contains(bone)) bone.gameObject.SetActive(false);
                }
            }
            ReloadMesh(false);
        }

        /// <summary>
        /// ヒエラルキー上で非アクティブなボーンを参照から外す
        /// </summary>
        /// <param name="rend"></param>
        void DeleateDisableBone()
        {
            // 先ずセーブ情報取得
            var path = EditorUtility.SaveFilePanel("Save", "Assets", rends[editIndex].name+"_Edit", "mesh");
            if (string.IsNullOrWhiteSpace(path)) return;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileName(path).Split('.')[0];
            
            var sm = editMeshCreater.ToSkinMesh(file,avatar.transform);
            
            sm.transform.position = rends[0].transform.position;
            sm.transform.rotation = rends[0].transform.rotation;
            
            // editMeshCreaterの切り替え
            var smsm = sm.GetComponent<SkinnedMeshRenderer>();
            AddRend(smsm);
            
            smsm.bones = editMeshCreater.MergeDisableBones();

            smsm.sharedMesh = editMeshCreater.Save(Path.Combine(dir,file+".mesh"));
            ReloadMesh(false);
        }

        void GetRawData(int vid)
        {
            if (editMeshCreater != null)
            {
                rawPosition = editMeshCreater.GetPosition(vid);
                rawNormal = editMeshCreater.GetNormal(vid);
                rawTangent = editMeshCreater.GetTangent(vid);
                rawColor = editMeshCreater.GetColor(vid);
                rawUVs = editMeshCreater.GetUVs(vid);
                rawWeights = editMeshCreater.GetWeightData(vid);

                rawIDs[0] = rawIDs[1];
                rawIDs[1] = rawIDs[2];
                rawIDs[2] = vid;
            }
        }

        void SetRawData()
        {
            if (editMeshCreater != null)
            {
                editMeshCreater.SetRawData(rawIDs[2],rawPosition,rawNormal,rawTangent,rawColor,rawUVs,rawWeights);
                ReloadMesh(false);
            }
        }

        void GenerateTriangle()
        {
            if (editMeshCreater != null)
            {
                editMeshCreater.AddTriangle(rawIDs[0],rawIDs[1],rawIDs[2]);
                ReloadMesh(false);
            }
        }
            
        /// <summary>
        /// 編集ツールの設定値保存用
        /// </summary>
        class MeshPenTool
        {
            private Texture icon;
            private string name;

            private ExtraTool? extraTool;
            private float? brushPower;
            private float? brushWidth;
            private float? brushStrength;

            public MeshPenTool(string guid, string n,
                ExtraTool? e = null,
                float? s = null,
                float? p = null,
                float? w = null)
            {
                if (!string.IsNullOrWhiteSpace(guid))
                {
                    var i = AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));
                    icon = i;
                }
                name = n;
                extraTool = e;
                brushPower = p;
                brushWidth = w;
                brushStrength = s;
            }
            public MeshPenTool(Texture i, string n,
                ExtraTool? e = null,
                float? s = null,
                float? p = null,
                float? w = null)
            {
                icon = i;
                name = n;
                extraTool = e;
                brushPower = p;
                brushWidth = w;
                brushStrength = s;
            }

            public bool Button(ref ExtraTool e,ref float p,ref float w,ref float s)
            {
                using (new EditorGUI.DisabledScope(
                    e == (extraTool ?? e) &&
                    p == (brushPower ?? p) &&
                    w == (brushWidth ?? w) &&
                    s == (brushStrength ?? s)))
                {
                    if (icon)
                    {
                        if (GUILayout.Button(icon))
                        {
                            e = extraTool ?? e;
                            p = brushPower ?? p;
                            w = brushWidth ?? w;
                            s = brushStrength ?? s;
                            return true;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(name))
                        {
                            e = extraTool ?? e;
                            p = brushPower ?? p;
                            w = brushWidth ?? w;
                            s = brushStrength ?? s;
                            return true;
                        }
                    } 
                }

                return false;
            }
            
            public enum ExtraTool
            {
                Default,
                DetailMode,
                TriangleEraser,
                SelectLand,
                UnSelectLand,
                SelectVertex,
                UnSelectVertex,
                WeightCopy,
                Decimate,
                Subdivision
            }

            private bool FloatEqual(float a, float? b)
            {
                return Mathf.Abs(a - b ?? a) < 0.01f;
            }
        }
    }
}
