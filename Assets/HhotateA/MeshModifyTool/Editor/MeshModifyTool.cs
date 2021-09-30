/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
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
        [MenuItem("Window/HhotateA/にゃんにゃんメッシュエディター(MeshModifyTool)",false,201)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MeshModifyTool>();
            wnd.titleContent = new GUIContent("にゃんにゃんメッシュエディター");
        }

        // ボタン設定
        private int drawButton = 0;
        private int rotateButton = 1;
        private int moveButton = 2;

        // ショートカット取得
        private bool keyboardShortcut = false;
        private bool keyboardShift = false;
        private bool keyboardCtr = false;
        private bool keyboardAlt = false;
        private int shortcutToolBuffer = -1;
        
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
                if (meshsCreaters == null)
                {
                    return null;
                }

                if (0 <= editIndex && editIndex < meshsCreaters.Length)
                {
                    return meshsCreaters[editIndex];
                }
                else
                {
                    return null;
                }

            }
        }
        
        // Remesh機能用
        private int triangleCount = 0;

        private SkinnedMeshRenderer weightOrigin;
        
        // カメラ表示用補助クラス
        AvatarMonitor avatarMonitor;
        private const int previewLayer = 2;
        private Vector2 rendsScroll = Vector2.zero;
        private bool viewOption = false;
        private Material wireFrameMaterial;
        Color wireFrameColor = Color.white;
        private Material[] defaultMaterials;
        private Material[] normalMaterials;
        private float normalAlpha = 0f;
        
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
        private bool isSelectVertex = true;
        private bool isRandomizeVertex = false;
        private bool isRealtimeTransform = true;
        private bool isSelectOverlappingVertexes = true;
        private bool isVertexRemove = false;
        
        private bool isSaveAll = false;
        private bool isGenerateNewMesh = false;
        
        private bool extendExperimental = false;
        private bool extendSaveOption = false;
        private bool extendRawdata = false;

        private bool isDecimateBone = false;
        private DecimateBoneMode decimateBoneMode;
        enum DecimateBoneMode
        {
            DeleateDisableBones,
            DeleateNonHumanoidBones,
        }
        
        // mesh simpler
        private float meshSimplerQuality = 0.5f;

        // MergeBone機能用
        private bool isMergeBone = false;
        /*private Animator originHuman;
        private Animator targetHuman;*/
        private GameObject targetHuman;
        private MergeBoneMode mergeBoneMode;
        enum MergeBoneMode
        {
            Merge,
            Constraint,
        }
        
        private bool isCombineMesh = false;
        private CombineMeshMode combineMeshMode;
        enum CombineMeshMode
        {
            CombineAllMesh,
            CombineActiveMesh,
        }

        private bool isCombineMaterial = false;
        private CombineMaterialMode combineMaterialMode;
        enum CombineMaterialMode
        {
            ByMaterial,
            ByShader,
            ForceCombine,
        }
        
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
                        new MeshPenTool(EnvironmentGUIDs.smoothTooIcon,"Smooth",MeshPenTool.ExtraTool.Default,2f,0.003f,0.03f), 
                        new MeshPenTool(EnvironmentGUIDs.linerToolIcon,"Liner",MeshPenTool.ExtraTool.Default,1f,-0.01f,0.03f), 
                        new MeshPenTool(EnvironmentGUIDs.constantToolIcon,"Constant",MeshPenTool.ExtraTool.Default,10f,null,0f), 
                        new MeshPenTool(EnvironmentGUIDs.detailToolIcon,"Detail",MeshPenTool.ExtraTool.DetailMode,null,null,0f), 
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
                        new MeshPenTool(EnvironmentGUIDs.selectLandToolIcon,"SelectLand",MeshPenTool.ExtraTool.SelectLand,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.unSelectLandToolIcon,"UnSelectLand",MeshPenTool.ExtraTool.UnSelectLand,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.selectVertexToolIcon,"SelectVertex",MeshPenTool.ExtraTool.SelectVertex,null,null,0f),
                        new MeshPenTool(EnvironmentGUIDs.unSelectVertexToolIcon,"UnSelectVertex",MeshPenTool.ExtraTool.UnSelectVertex,null,null,0f),
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
        Vector2 rowScroll = Vector2.zero;
        // private bool displayRawData => activeExperimentalBeta;
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
                    if (rends == null)
                    {
                        WindowBase.TitleStyle("にゃんにゃんメッシュエディター");
                        WindowBase.DetailStyle("Unityだけでアバターのメッシュ改変ができるツールです．",EnvironmentGUIDs.readme);
                        avatar = EditorGUILayout.ObjectField("", avatar, typeof(GameObject), true) as GameObject;
                        if (GUILayout.Button("Setup"))
                        {
                            Setup(avatar);
                        }
                        WindowBase.Signature();
                        return;
                    }
                    rendsScroll = EditorGUILayout.BeginScrollView(rendsScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
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

                    using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
                    {
                        EditorGUILayout.LabelField(" ");
                    }

                    //activeExperimentalAlpha = EditorGUILayout.Toggle("ActiveExperimental", activeExperimentalAlpha);
                    //if (activeExperimentalAlpha)
                    {
                        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                        {
                            isSelectVertex = EditorGUILayout.Toggle("SelectVertexMode", isSelectVertex);
                            isRealtimeTransform = EditorGUILayout.Toggle("RealtimeTransform", isRealtimeTransform,
                                GUILayout.Width(200));
                            // isRemoveAsBlendShape = EditorGUILayout.Toggle("DeleteAsBlendShape", isRemoveAsBlendShape, GUILayout.Width(155));
                            isSelectOverlappingVertexes = EditorGUILayout.Toggle("SelectOverlapping",
                                isSelectOverlappingVertexes, GUILayout.Width(155));
                            keyboardShortcut = EditorGUILayout.Toggle( new GUIContent("Keyboard Shortcut",
                                "Shortcuts : \n" +
                                "   Alt + Right Drag : Move \n" +
                                "   Alt + Left Drag : Rotate \n" +
                                "   Ctr + Z : Undo \n" +
                                "   Ctr + Y : Redo \n" +
                                "   Shift + Wheel : Power Change \n" +
                                "   Ctr Hold: Reverse Power \n" +
                                "   Alt + Wheel : Strength Change \n" +
                                "   SelectMode : \n" +
                                "      Shift Hold: SelectLand \n" +
                                "     Ctr Hold: UnSelect \n"), keyboardShortcut);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                // if (activeExperimentalAlpha)
                                {
                                    foreach (var penTool in extraTools)
                                    {
                                        if (penTool.Button(ref penMode, ref brushPower, ref brushWidth,
                                            ref brushStrength))
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

                            extendExperimental = EditorGUILayout.Foldout(extendExperimental, "Experimentals");
                            if (extendExperimental)
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    wireFrameColor = EditorGUILayout.ColorField("Wire Frame Color", wireFrameColor);
                                    normalAlpha = EditorGUILayout.Slider("Normal",normalAlpha,0f,1f);
                                    if (check.changed)
                                    {
                                        if (wireFrameMaterial != null)
                                        {
                                            wireFrameMaterial.SetColor("_Color",wireFrameColor);
                                        }
                                        
                                        if (normalAlpha > 0.1f)
                                        {
                                            CreateNormalMesh();
                                            foreach (var normalMaterial in normalMaterials)
                                            {
                                                normalMaterial.SetFloat("_NormalAlpha",normalAlpha);
                                            }
                                        }
                                        else
                                        {
                                            rends[editIndex].sharedMaterials = defaultMaterials.ToArray();
                                        }
                                    }
                                }
                                
                                if (editMeshCreater != null)
                                {
                                    using (var check = new EditorGUI.ChangeCheckScope())
                                    {
                                        var isRecalculateNormals = EditorGUILayout.Toggle("Recalculate Normals", editMeshCreater.IsRecalculateNormals);
                                        if (check.changed)
                                        {
                                            foreach (var meshsCreater in meshsCreaters)
                                            {
                                                meshsCreater.IsRecalculateNormals = isRecalculateNormals;
                                                meshsCreater.IsRecalculateBlendShapeNormals = isRecalculateNormals;
                                            }
                                        }
                                    }
                                }
                                
                                isVertexRemove = EditorGUILayout.Toggle("Delete Vertex", isVertexRemove);
                                
                                extendRawdata = EditorGUILayout.Toggle("View Raw Data", extendRawdata);

                                // EditorGUILayout.LabelField("NotRecommended", GUILayout.Width(120));
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    foreach (var betaTool in betaTools)
                                    {
                                        if (betaTool.Button(ref penMode, ref brushPower, ref brushWidth,
                                            ref brushStrength))
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

                                        if (GUILayout.Button("CopyBoneWeight", GUILayout.Width(125)))
                                        {
                                            if (weightOrigin)
                                            {
                                                editMeshCreater.CopyBoneWeight(
                                                    new SkinnedMeshRenderer[1] {weightOrigin});

                                            }
                                        }
                                    }

                                    // 精度悪い，重い
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        triangleCount = EditorGUILayout.IntField("", triangleCount, GUILayout.Width(155));
                                        if (GUILayout.Button("Decimate", GUILayout.Width(125)))
                                        {
                                            Decimate();
                                        }
                                    }
                                }
                                
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    meshSimplerQuality = EditorGUILayout.FloatField("", meshSimplerQuality, GUILayout.Width(155));
                                    if (GUILayout.Button("MeshSimplifier", GUILayout.Width(125)))
                                    {
                                        var meshSimplifier = new ThirdParty.MeshSimplifier.MeshSimplifier();
                                        meshSimplifier.Initialize(rends[editIndex].GetMesh());
                                        meshSimplifier.SimplifyMesh(meshSimplerQuality);
                                        rends[editIndex].SetMesh(meshSimplifier.ToMesh());
                                        meshsCreaters[editIndex] = new MeshCreater(rends[editIndex]);
                                        SelectMeshCreater(editIndex);
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
                    EditorGUILayout.Space();

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

                    extendSaveOption = EditorGUILayout.Foldout(extendSaveOption, "Save Option");
                    if (extendSaveOption)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("",GUILayout.Width(5));

                            using (new EditorGUILayout.VerticalScope())
                            {
                                if (isCombineMesh)
                                {
                                    using (new EditorGUI.DisabledScope(true))
                                    {
                                        EditorGUILayout.Toggle("Save All", false);
                                        EditorGUILayout.Toggle("Generate New Mesh", true);
                                    }
                                }
                                else
                                {
                                    isSaveAll = EditorGUILayout.Toggle("Save All", isSaveAll);
                                    if (isMergeBone)
                                    {
                                        using (new EditorGUI.DisabledScope(true))
                                        {
                                            EditorGUILayout.Toggle("Generate New Mesh", true);
                                        }
                                    }
                                    else
                                    {
                                        isGenerateNewMesh =
                                            EditorGUILayout.Toggle("Generate New Mesh", isGenerateNewMesh);
                                    }
                                }

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    isDecimateBone = EditorGUILayout.ToggleLeft("Decimate Bone", isDecimateBone,
                                        GUILayout.Width(130));
                                    using (new EditorGUI.DisabledScope(!isDecimateBone))
                                    {
                                        EditorGUILayout.LabelField("  ", GUILayout.Width(20));
                                        decimateBoneMode =
                                            (DecimateBoneMode) EditorGUILayout.EnumPopup("", decimateBoneMode,
                                                GUILayout.Width(145));
                                    }
                                }

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    isCombineMesh = EditorGUILayout.ToggleLeft("Combine Mesh", isCombineMesh,
                                        GUILayout.Width(130));
                                    using (new EditorGUI.DisabledScope(!isCombineMesh))
                                    {
                                        EditorGUILayout.LabelField("  ", GUILayout.Width(20));
                                        combineMeshMode =
                                            (CombineMeshMode) EditorGUILayout.EnumPopup("", combineMeshMode,
                                                GUILayout.Width(145));
                                    }
                                }

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    isCombineMaterial = EditorGUILayout.ToggleLeft("Combine Materials",
                                        isCombineMaterial,
                                        GUILayout.Width(130));
                                    using (new EditorGUI.DisabledScope(!isCombineMaterial))
                                    {
                                        EditorGUILayout.LabelField("  ", GUILayout.Width(20));
                                        combineMaterialMode =
                                            (CombineMaterialMode) EditorGUILayout.EnumPopup("", combineMaterialMode,
                                                GUILayout.Width(145));
                                    }
                                }

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    isMergeBone =
                                        EditorGUILayout.ToggleLeft("Change Bone", isMergeBone, GUILayout.Width(130));
                                    using (new EditorGUI.DisabledScope(!isMergeBone))
                                    {
                                        EditorGUILayout.LabelField("  ", GUILayout.Width(20));
                                        targetHuman = (GameObject) EditorGUILayout.ObjectField("", targetHuman,
                                            typeof(GameObject), true, GUILayout.Width(95));
                                        mergeBoneMode =
                                            (MergeBoneMode) EditorGUILayout.EnumPopup("", mergeBoneMode,
                                                GUILayout.Width(50));
                                    }
                                }
                            }
                        }

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
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(editMeshCreater == null && !isSaveAll && !isCombineMesh))
                        {
                            if (GUILayout.Button("Export"))
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
                        var avatarMonitorWidth = extendRawdata ? 750 : 300;
                        int positionDrag = keyboardShortcut && keyboardAlt ? drawButton : moveButton;
                        bool canNotTouch = keyboardShortcut && (keyboardShift || keyboardCtr);
                        bool canNotWheel = keyboardShortcut && (keyboardShift || keyboardAlt);
                        avatarMonitor.Display( (int) position.width-avatarMonitorWidth, (int) position.height-10,
                            rotateButton, positionDrag, !canNotTouch, !canNotWheel);
                        if (editIndex != -1)
                        {
                            AvatarMonitorTouch(editMeshCreater);
                        }
                    }
                }
                
                if (extendRawdata)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        rowScroll =  EditorGUILayout.BeginScrollView(rowScroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
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
                                    rawIDs[i] = EditorGUILayout.IntField("", rawIDs[i],GUILayout.Width(135));
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
                            uvViewer?.Display(400,400);
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
                        EditorGUILayout.EndScrollView();
                    }
                }
            }
            
            var ec = Event.current;
            // キー関係
            if (keyboardShortcut)
            {
                Undo.ClearAll();
                if (ec.type == EventType.KeyDown)
                {
                    if (ec.keyCode == KeyCode.LeftShift || ec.keyCode == KeyCode.RightShift)
                    {
                        keyboardShift = true;
                        if (penMode == MeshPenTool.ExtraTool.SelectVertex)
                        {
                            penMode = MeshPenTool.ExtraTool.SelectLand;
                        }
                        else
                        if (penMode == MeshPenTool.ExtraTool.UnSelectVertex)
                        {
                            penMode = MeshPenTool.ExtraTool.UnSelectLand;
                        }
                    }
                    if (ec.keyCode == KeyCode.LeftControl || ec.keyCode == KeyCode.RightControl)
                    {
                        keyboardCtr = true;
                        if (penMode == MeshPenTool.ExtraTool.SelectVertex)
                        {
                            penMode = MeshPenTool.ExtraTool.UnSelectVertex;
                        }
                        else
                        if (penMode == MeshPenTool.ExtraTool.SelectLand)
                        {
                            penMode = MeshPenTool.ExtraTool.UnSelectLand;
                        }
                    }
                    if (ec.keyCode == KeyCode.LeftAlt || ec.keyCode == KeyCode.RightAlt)
                    {
                        keyboardAlt = true;
                    }
                    if (ec.keyCode == KeyCode.Z)
                    {
                        DestroyControllPoint();
                        editMeshCreater?.UndoCaches();
                        ReloadMesh(false,editMeshCreater,controll_vertexes);
                    }
                    if (ec.keyCode == KeyCode.Y)
                    {
                        DestroyControllPoint();
                        editMeshCreater?.RedoCaches();
                        ReloadMesh(false,editMeshCreater,controll_vertexes);
                    }
                }
                
                if (ec.type == EventType.KeyUp)
                {
                    if (ec.keyCode == KeyCode.LeftShift || ec.keyCode == KeyCode.RightShift)
                    {
                        keyboardShift = false;
                        if (penMode == MeshPenTool.ExtraTool.SelectLand)
                        {
                            penMode = MeshPenTool.ExtraTool.SelectVertex;
                        }
                        else
                        if (penMode == MeshPenTool.ExtraTool.UnSelectLand)
                        {
                            penMode = MeshPenTool.ExtraTool.UnSelectVertex;
                        }
                    }
                    if (ec.keyCode == KeyCode.LeftControl || ec.keyCode == KeyCode.RightControl)
                    {
                        keyboardCtr = false;
                        if (penMode == MeshPenTool.ExtraTool.UnSelectVertex)
                        {
                            penMode = MeshPenTool.ExtraTool.SelectVertex;
                        }
                        else
                        if (penMode == MeshPenTool.ExtraTool.UnSelectLand)
                        {
                            penMode = MeshPenTool.ExtraTool.SelectLand;
                        }
                    }
                    if (ec.keyCode == KeyCode.LeftAlt || ec.keyCode == KeyCode.RightAlt)
                    {
                        keyboardAlt = false;
                    }
                }
                
                if (ec.type == EventType.ScrollWheel)
                {
                    if (keyboardShift)
                    {
                        brushWidth = brushWidth + -ec.delta.y * 0.001f;
                    }
                    if (keyboardAlt)
                    {
                        brushStrength = brushStrength + -ec.delta.y * 0.05f;
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
            rends = anim.transform.GetComponentsInChildren<Renderer>().Where(r=>r.GetMesh()!=null).ToArray();
            defaultMeshs = rends.Select(m => m.GetMesh()).ToArray();
            meshsCreaters = rends.Select(m => new MeshCreater(m)).ToArray();
            
            if(avatarMonitor!=null) avatarMonitor.Release();
            avatarMonitor = new AvatarMonitor(anim.transform);
            
            editIndex = -1;
        }

        void AddRend(Renderer rend)
        {
            if (rend == null) return;
            if (rend.GetMesh() == null) return;
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
            if (isSaveAll && !(isCombineMesh && combineMeshMode == CombineMeshMode.CombineAllMesh))
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
                if (editIndex == -1)
                {
                    SelectMeshCreater(0);
                }
                saveAction?.Invoke();
            }
        }

        bool Save()
        {
            var path = EditorUtility.SaveFilePanel("Save", "Assets", rends[editIndex].name+"_Edit", "mesh");
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);
            var file = Path.GetFileNameWithoutExtension(path);
                            
            DestroyEditMesh();
            DestroyControllPoint();
            ReloadMesh(false);

            MeshCreater mc = new MeshCreater(editMeshCreater);
            if (isCombineMesh)
            {
                if (combineMeshMode == CombineMeshMode.CombineActiveMesh)
                {
                    mc = new MeshCreater(avatar.transform,meshsCreaters.Where(m=>m.RendBone.gameObject.activeSelf).ToArray());
                }
                else if(combineMeshMode == CombineMeshMode.CombineAllMesh)
                {
                    mc = new MeshCreater(avatar.transform,meshsCreaters);
                }
            }

            if (isCombineMaterial)
            {
                if (combineMaterialMode == CombineMaterialMode.ByMaterial)
                {
                    mc.CombineMesh();
                }
                else if(combineMaterialMode == CombineMaterialMode.ByShader)
                {
                    mc.MaterialAtlas(Path.Combine(dir, file));
                }
                else if(combineMaterialMode == CombineMaterialMode.ForceCombine)
                {
                    mc.ForceCombine();
                }
            }

            if (isMergeBone && targetHuman != null)
            {
                if (mergeBoneMode == MergeBoneMode.Merge)
                {
                    //var sm = SaveMeshCreater(meshsCreaters[editIndex],dir,file);
                    mc = MergeBone( mc, dir, file);
                }
                else if(mergeBoneMode == MergeBoneMode.Constraint)
                {
                    mc = CombineBone( mc, dir, file);
                }
            }

            if (isDecimateBone)
            {
                if (decimateBoneMode == DecimateBoneMode.DeleateNonHumanoidBones)
                {
                    if (rends[editIndex] is SkinnedMeshRenderer)
                    {
                        DisableNonHumanBone(rends[editIndex] as SkinnedMeshRenderer);
                    }
                    mc = DeleateDisableBone(mc);
                }
                else if (decimateBoneMode == DecimateBoneMode.DeleateDisableBones)
                {
                    mc = DeleateDisableBone(mc);
                }
            }

            if (isCombineMesh && combineMeshMode == CombineMeshMode.CombineAllMesh)
            {
                var sm = SaveMeshCreater(mc,dir,file);
                AddRend(sm);
            }
            else if (isMergeBone && targetHuman != null)
            {
                var sm = SaveMeshCreater(mc,dir,file);
                sm.transform.SetParent(targetHuman.transform);
                AddRend(sm);
            }
            else if (isGenerateNewMesh)
            {
                var sm = SaveMeshCreater(mc,dir,file);
                AddRend(sm);
            }
            else
            {
                defaultMeshs[editIndex] = SaveMeshCreater(mc,dir,file,rends[editIndex]);
            }
            
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
        Mesh SaveMeshCreater(MeshCreater mc,string dir,string file,Renderer rend)
        {
            if (isRandomizeVertex)
            {
                var mat = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.DecryptionShader));
                var p = Path.GetDirectoryName(AssetDatabase.GetAssetPath(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.DecryptionShader)));
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
        
        SkinnedMeshRenderer SaveMeshCreater(MeshCreater mc,string dir,string file)
        {
            if (isRandomizeVertex)
            {
                var mat = new Material(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.DecryptionShader));
                var p = Path.GetDirectoryName(AssetDatabase.GetAssetPath(AssetUtility.LoadAssetAtGuid<Shader>(EnvironmentGUIDs.DecryptionShader)));
                File.WriteAllText(Path.Combine(p,"Keys.cginc"), mc.Encryption(mat));
                mc.MaterialAtlas(Path.Combine(dir,file));
                mc.CombineMesh();
            }

            mc.Create(false);
            var m = mc.Save(Path.Combine(dir, file + ".mesh"));

            var sm = mc.ToSkinMesh(file,avatar.transform);
        
            sm.transform.position = rends[0].transform.position;
            sm.transform.rotation = rends[0].transform.rotation;
            var smsm = sm.GetComponent<SkinnedMeshRenderer>();
            smsm.SetMesh(m);

            return smsm;
        }
        
        /// <summary>
        /// カメラ表示，インタラクト処理
        /// </summary>
        /// <param name="mc"></param>
        void AvatarMonitorTouch(MeshCreater mc)
        {
            // ショートカット使用中は書かない
            if (keyboardShortcut && keyboardAlt) return;
            var ec = Event.current;
            if (penMode == MeshPenTool.ExtraTool.Default)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
                {
                    avatarMonitor.GetControllPoint(GetEditMeshCollider(), isSelectVertex,
                        h => { TransfromMeshMirror(mc, h); });
                }
            }
            else
            if (penMode == MeshPenTool.ExtraTool.DetailMode)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
                {
                    avatarMonitor.GetControllPoint(GetEditMeshCollider(), isSelectVertex,
                        h => { GenerateControllPoint(mc, h); });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.TriangleEraser)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                        }, isSelectOverlappingVertexes);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.UnSelectLand)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                        }, isSelectOverlappingVertexes);
                    });
                }
            }
            else
            if(penMode == MeshPenTool.ExtraTool.SelectVertex)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
                {
                    avatarMonitor.GetTriangle(GetEditMeshCollider(), (h, p) =>
                    {
                        GenerateControllPoint(mc, p);
                        mc.Subdivision(h, p, isSelectVertex);
                        ReloadMesh(true, mc);
                    });
                }
            }

            if (extendRawdata)
            {
                if (ec.type == EventType.MouseDown && ec.button == drawButton)
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
            controllPoint_from.transform.localScale = new Vector3(0.009f,0.009f,0.009f) * avatarMonitor.GetBound;
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
                controllPoint_to.transform.localScale = new Vector3(0.01f,0.01f,0.01f) * avatarMonitor.GetBound;
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
                if (keyboardShortcut && keyboardCtr)
                {
                    mc.TransformMesh(from, to, -brushPower*avatarMonitor.GetBound, brushWidth*avatarMonitor.GetBound, brushStrength);
                }
                else
                {
                    mc.TransformMesh(from, to, brushPower*avatarMonitor.GetBound, brushWidth*avatarMonitor.GetBound, brushStrength);
                }
            }
            else
            {
                mc.TransformMesh(from, to, brushWidth*avatarMonitor.GetBound, brushStrength);
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
            DestroyNormalMesh();
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
            if (mc == null)
            {
                mc = editMeshCreater;
                if (mc == null) return;
            }

            if (verts == null)
            {
                verts = controll_vertexes;
            }

            if (rend == null)
            {
                if (0 <= editIndex && editIndex < meshsCreaters.Length)
                {
                    rend = rends?[editIndex];
                }
                else
                {
                    return;
                }
            }

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

            defaultMaterials = rend.sharedMaterials.ToArray();
            normalMaterials = rends[editIndex].sharedMaterials.Select(mat =>
            {
                var m = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.NormalMaterial));
                m.mainTexture = mat.mainTexture;
                return m;
            }).ToArray();
            CreateNormalMesh();

            if (cashes)
            {
                mc.AddCaches();
            }
            
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
                if (wireFrameMaterial == null)
                {
                    wireFrameMaterial = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.OverlayWireFrameMaterial));
                    wireFrameMaterial.SetFloat("_ZTest",4);
                    wireFrameMaterial.SetColor("_Color",wireFrameColor);
                }
                rend.sharedMaterials = Enumerable.Range(0,rend.sharedMaterials.Length).Select(_=> wireFrameMaterial).ToArray();
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

        void CreateNormalMesh()
        {
            if (editIndex < 0) return;
            if (normalAlpha > 0.1f)
            {
                rends[editIndex].sharedMaterials = normalMaterials;
            }
            else
            {
                rends[editIndex].sharedMaterials = defaultMaterials.ToArray();
            }
        }

        void DestroyNormalMesh()
        {
            if (editIndex < 0) return;
            if (defaultMaterials != null)
            {
                rends[editIndex].sharedMaterials = defaultMaterials.ToArray();
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
                var mat = new Material(AssetUtility.LoadAssetAtGuid<Material>(EnvironmentGUIDs.OverlayWireFrameMaterial));
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

        /// <summary>
        /// ボーンの参照先を変更する
        /// </summary>
        MeshCreater MergeBone(MeshCreater mc,string dir,string file)
        {
            // ここからボーンの参照
            mc.ChangeBones(targetHuman,avatar,true);
            return mc;
        }
        
        /// <summary>
        /// ボーンの参照先を，コンストレイントボーンに変更する
        /// </summary>
        MeshCreater CombineBone(MeshCreater mc,string dir,string file)
        {
            // ここからボーンの参照
            var t = targetHuman.GetBones();
            var o = avatar.GetBones();
            var p = targetHuman.transform.Find(avatar.name + "_bones") ??
                new GameObject(avatar.name + "_bones").transform;
            p.SetParent(targetHuman.transform);
            p.localPosition = Vector3.zero;
            p.localRotation = Quaternion.identity;
            p.localScale = Vector3.one;
            
            mc.ChangeBones(targetHuman,avatar, 
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
            return mc;
        }
        
        /// <summary>
        /// コンストレイントボーンを設定する
        /// </summary>
        void ConstraintBone()
        {
            var t = targetHuman.GetBones();
            var o = avatar.GetBones();
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
            var humanBones = avatar.GetBones();
            foreach (var bone in rend.bones)
            {
                if(!humanBones.Contains(bone)) bone.gameObject.SetActive(false);
            }
            ReloadMesh(false);
        }

        /// <summary>
        /// ヒエラルキー上で非アクティブなボーンを参照から外す
        /// </summary>
        /// <param name="rend"></param>
        MeshCreater DeleateDisableBone(MeshCreater mc)
        {
            mc.MergeDisableBones();
            return mc;
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
                    var i = AssetUtility.LoadAssetAtGuid<Texture>(guid);
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

            public MeshPenTool(Texture2D i, string n,
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
