/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.TailMover
{
    public class TailMoverSetup : WindowBase
    {
        [MenuItem("Window/HhotateA/なでもふセットアップ(TailMoverSetup)",false,103)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TailMoverSetup>();
            wnd.minSize = new Vector2(600,200);
            wnd.titleContent = new GUIContent("TailMoverSetup");
        }

        private bool isHumanoidAnimation = false;
        
        private List<Transform> tailRoots = new List<Transform>();
        private List<Transform> tailIgnores = new List<Transform>();
        private Vector3 tailaxi = Vector3.zero;
        private bool expandRoots = true;
        private bool expandIgnores = false;
        private bool expandRotSetting = false;
        
        Texture2D tailIdleIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.tailIdleIcon));
        Texture2D tailControllIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.tailControllIcon));
        Texture2D ahogeIdleIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.ahogeIdleIcon));
        Texture2D ahogeControllIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.ahogeControllIcon));
        Texture2D kemomimiIdleIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.kemomimiIdleIcon));
        Texture2D kemomimiControllIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.kemomimiControllIcon));
        Texture2D armIdleIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.armIdleIcon));
        Texture2D armControllIcon => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(EnvironmentGUIDs.armControllIcon));
        Texture2D[] idleIcons;
        Texture2D[] controllIcons;

        private Presets? preset = null;
        enum Presets
        {
            General,
            Tail,
            Ahoge,
            KemoMimi,
            RightArm,
            LeftArm,
        }

        private TailControlls tailControll = TailControlls.Center;
        enum TailControlls
        {
            Center,
            Up,
            Down,
            Right,
            Left
        }

        private Vector2[] controllValues = new Vector2[5]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
        };

        private Vector3[] tailRots = Enumerable.Range(0,Enum.GetNames(typeof(TailControlls)).Length).Select(_=>Vector3.zero).ToArray();

        private Dictionary<Transform,Quaternion> defaultRots;
        private Dictionary<Transform,Quaternion> zeroRots;
        private Dictionary<Transform,float> curveWeightValue;
        
        private AnimationCurve curveWeight = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private bool enableTestRotX = true;
        private bool enableTestRotY = false;
        private float testRotX = 0f;
        private float testRotY = 0f;

        private float idleSpeed = 0.1f;
        private float idleInertia = 0.03f;

        private string dataname = "TailMover";

        private void OnEnable()
        {
            idleIcons = new Texture2D[6]
            {
                null,
                tailIdleIcon,
                ahogeIdleIcon,
                kemomimiIdleIcon,
                armIdleIcon,
                armIdleIcon
            };
            controllIcons = new Texture2D[6]
            {
                null,
                tailControllIcon,
                ahogeControllIcon,
                kemomimiControllIcon,
                armControllIcon,
                armControllIcon
            };
        }

        private void Update()
        {
            if (defaultRots==null) return;
            if (!expandRotSetting)
            {
                if(enableTestRotX) testRotX = Mathf.Sin(Time.fixedTime * idleSpeed/0.1f);
                if(enableTestRotY) testRotY = Mathf.Sin(Time.fixedTime * (idleSpeed+idleInertia)/0.1f);
                RotTail(testRotX,testRotY);
            }
        }

        private void OnGUI()
        {
            TitleStyle("なでもふセットアップ");
            DetailStyle("アバターの尻尾やケモ耳のアイドルモーションを設定したり，デスクトップモードで腕を動かす設定ができるツールです．",EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3

            EditorGUILayout.Space();
            AvatartField();
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (!avatar)
            {
                if (ShowOptions())
                {
                    if (GUILayout.Button("Force Revert"))
                    {
#if VRC_SDK_VRCSDK3
                        var mod = new AvatarModifyTool(avatar);
                        mod.RevertByKeyword(EnvironmentGUIDs.prefix);
                        OnFinishRevert();
#endif
                    }
                }
                return;
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (Presets val in Enum.GetValues(typeof(Presets)))
                {
                    using (new EditorGUI.DisabledScope(val == preset))
                    {
                        if (GUILayout.Button(new GUIContent(val.ToString(),idleIcons[(int)val]),GUILayout.MinWidth(50),GUILayout.Height(50)))
                        {
                            ApplyPreset(val);
                        }
                    }
                }
            }

            if (preset == null)
            {
                EditorGUILayout.LabelField("プリセットを選択してください．");
                return;
            }

            expandRoots = EditorGUILayout.Foldout(expandRoots,"RootBones");
            if (expandRoots)
            {
                for (int i = 0; i < tailRoots.Count; i++)
                {
                    if (tailRoots[i] == null)
                    {
                        tailRoots.RemoveAt(i);
                        break;
                    }
                    tailRoots[i] = EditorGUILayout.ObjectField("", tailRoots[i], typeof(Transform), true) as Transform;
                }
                var newRoot = EditorGUILayout.ObjectField("", null, typeof(Transform), true) as Transform;
                if (newRoot)
                {
                    if (avatar == null)
                    {
#if VRC_SDK_VRCSDK3
                        avatar = newRoot.GetComponentInParent<VRCAvatarDescriptor>();
#else
                        avatar = newRoot.GetComponentInParent<Animator>();
#endif
                    }
                    var rootparent = newRoot.parent;
                    while (rootparent!=null)
                    {
                        if (rootparent == avatar.transform)
                        {
                            tailRoots.Add(newRoot);
                        }
                        rootparent = rootparent.parent;
                    }
                }
            }
            
            expandIgnores = EditorGUILayout.Foldout(expandIgnores,"IgnoreBones");
            if (expandIgnores)
            {
                for (int i = 0; i < tailIgnores.Count; i++)
                {
                    if (tailIgnores[i] == null)
                    {
                        tailIgnores.RemoveAt(i);
                        break;
                    }
                    tailIgnores[i] = EditorGUILayout.ObjectField("", tailIgnores[i], typeof(Transform), true) as Transform;
                }
                var newIgnores = EditorGUILayout.ObjectField("", null, typeof(Transform), true) as Transform;
                if (newIgnores)
                {
                    var rootparent = newIgnores.parent;
                    while (rootparent!=null)
                    {
                        if (tailRoots.Contains(rootparent))
                        {
                            tailIgnores.Add(newIgnores);
                            break;
                        }
                        rootparent = rootparent.parent;
                    }
                }
            }

            tailaxi = EditorGUILayout.Vector3Field("TailAxi",tailaxi);
            if(tailRoots.Count==0)
            {
                EditorGUILayout.LabelField("ルートボーンを設定してください．");
                return;
            }

            if (GUILayout.Button("Create Animation"))
            {
                ResetTail();
                Setup();
            }
            if (defaultRots==null) return;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope())
            {
                expandRotSetting = EditorGUILayout.Foldout(expandRotSetting,"RotSetting");
                if (expandRotSetting)
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUIStyle style = new GUIStyle();
                            style.fontStyle = FontStyle.Bold;
                            int bw = (int) (position.width * 0.33333f) - 5;
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                if (GUILayout.Button("", GUILayout.Width(50), GUILayout.Width(bw))){}
                                EditorGUI.EndDisabledGroup();
                                
                                EditorGUI.BeginDisabledGroup(tailControll == TailControlls.Up);
                                if (GUILayout.Button("Up", GUILayout.Width(50), GUILayout.Width(bw)))
                                    tailControll = TailControlls.Up;
                                EditorGUI.EndDisabledGroup();
                                
                                EditorGUI.BeginDisabledGroup(true);
                                if (GUILayout.Button("", GUILayout.Width(50), GUILayout.Width(bw))){}
                                EditorGUI.EndDisabledGroup();
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginDisabledGroup(tailControll == TailControlls.Left);
                                if (GUILayout.Button("Left", GUILayout.Width(50), GUILayout.Width(bw)))
                                    tailControll = TailControlls.Left;
                                EditorGUI.EndDisabledGroup();
                                
                                EditorGUI.BeginDisabledGroup(tailControll == TailControlls.Center);
                                if (GUILayout.Button("Center", GUILayout.Width(50), GUILayout.Width(bw)))
                                    tailControll = TailControlls.Center;
                                EditorGUI.EndDisabledGroup();
                                
                                EditorGUI.BeginDisabledGroup(tailControll == TailControlls.Right);
                                if (GUILayout.Button("Right", GUILayout.Width(50), GUILayout.Width(bw)))
                                    tailControll = TailControlls.Right;
                                EditorGUI.EndDisabledGroup();
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUI.BeginDisabledGroup(true);
                                if (GUILayout.Button("", GUILayout.Width(50), GUILayout.Width(bw))){}
                                EditorGUI.EndDisabledGroup();
                                
                                EditorGUI.BeginDisabledGroup(tailControll == TailControlls.Down);
                                if (GUILayout.Button("Down", GUILayout.Width(50), GUILayout.Width(bw)))
                                    tailControll = TailControlls.Down;
                                EditorGUI.EndDisabledGroup();
                                
                                EditorGUI.BeginDisabledGroup(true);
                                if (GUILayout.Button("", GUILayout.Width(50), GUILayout.Width(bw))){}
                                EditorGUI.EndDisabledGroup();
                            }
                        }

                        curveWeight = EditorGUILayout.CurveField(curveWeight);
                        var x = EditorGUILayout.Slider("XAngle", tailRots[(int) tailControll].x, -90f, 90f);
                        var y = EditorGUILayout.Slider("YAngle", tailRots[(int) tailControll].y, -90f, 90f);
                        var z = EditorGUILayout.Slider("ZAngle", tailRots[(int) tailControll].z, -90f, 90f);
                        isHumanoidAnimation = EditorGUILayout.Toggle("IsHumanoid", isHumanoidAnimation);
                        if (check.changed)
                        {
                            tailRots[(int) tailControll] = new Vector3(x,y,z);
                            RotTail(tailRots[(int) tailControll]);
                        }
                    }
                }

                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        testRotX = EditorGUILayout.Slider("testRotX", testRotX, -1f, 1f);
                        if (check.changed)
                        {
                            enableTestRotX = false;
                        }
                    }
                    if (GUILayout.Button("Auto",GUILayout.Width(50)))
                    {
                        enableTestRotX = true;
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        testRotY = EditorGUILayout.Slider("testRotY", testRotY, -1f, 1f);
                        if (check.changed)
                        {
                            enableTestRotY = false;
                        }
                    }
                    if (GUILayout.Button("Auto",GUILayout.Width(50)))
                    {
                        enableTestRotY = true;
                    }
                }
                
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("   ",GUILayout.Width(50));
                    EditorGUILayout.LabelField("Idle Speed",GUILayout.Width(100));
                    idleSpeed = EditorGUILayout.FloatField( idleSpeed,GUILayout.Width(75));
                    EditorGUILayout.LabelField("   ",GUILayout.ExpandWidth(true),GUILayout.MinWidth(25));
                    EditorGUILayout.LabelField("idle Inertia",GUILayout.Width(100));
                    idleInertia = EditorGUILayout.FloatField( idleInertia,GUILayout.Width(75));
                    EditorGUILayout.LabelField("   ",GUILayout.Width(50));
                }

                EditorGUILayout.Space();
                
                if (ShowOptions())
                {
                    if (GUILayout.Button("Force Revert"))
                    {
                        var mod = new AvatarModifyTool(avatar);
                        mod.RevertByKeyword(EnvironmentGUIDs.prefix);
                        OnFinishRevert();
                    }
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (GUILayout.Button("Save RadialControll"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", "Assets",  preset.ToString()+"Controll",
                        "controller");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    if (String.IsNullOrWhiteSpace(dataname))
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        dataname = fileName;
                    }
                    try
                    {
                        SaveTailAnim(path);
                        OnFinishSetup();
                        DetectAnimatorError();
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                }

                EditorGUILayout.Space();
                
                // if (preset != Presets.LeftArm && preset != Presets.RightArm)
                {
                    if (GUILayout.Button("Save IdleMotion"))
                    {
                        var path = EditorUtility.SaveFilePanel("Save", "Assets", preset.ToString()+"Idle",
                            "controller");
                        if (string.IsNullOrEmpty(path))
                        {
                            OnCancel();
                            return;
                        }
                        if (String.IsNullOrWhiteSpace(dataname))
                        {
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                            dataname = fileName;
                        }
                        try
                        {
                            SaveTailIdle(path);
                            OnFinishSetup();
                            DetectAnimatorError();
                        }
                        catch (Exception e)
                        {
                            OnError(e);
                            throw;
                        }
                    }
                }
                EditorGUILayout.Space();
                status.Display();
            }
#else
            VRCErrorLabel();
#endif
            Signature();
        }

        void Setup()
        {
            defaultRots = new Dictionary<Transform, Quaternion>();
            zeroRots = new Dictionary<Transform, Quaternion>();
            curveWeightValue = new Dictionary<Transform, float>();
            for (int i = 0; i < tailRoots.Count; i++)
            {
                SetupTail(tailRoots[i]);
            }
        }

        void ApplyPreset(Presets val)
        {
            preset = val;
            if (preset == Presets.General)
            {

            }
            else if (preset == Presets.Tail)
            {
                tailRoots = new List<Transform>();
                tailIgnores = new List<Transform>();
                tailaxi = new Vector3(0f,0f,-1f);
                tailRots = new Vector3[5]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(15f, 0f, 0f),
                    new Vector3(-30f, 0f, 0f),
                    new Vector3(0f, -20f, 0f),
                    new Vector3(0f, 20f, 0f),
                };
                curveWeight = AnimationCurve.Linear(0, 0, 1, 1);
                isHumanoidAnimation = false;
            }
            else if(preset == Presets.KemoMimi)
            {
                tailRoots = new List<Transform>();
                tailIgnores = new List<Transform>();
                tailaxi = Vector3.zero;
                tailRots = new Vector3[5]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-60f, 0f, 0f),
                    new Vector3(60f, 0f, 0f),
                    new Vector3(0f, 0f, -35f),
                    new Vector3(0f, 0f, 35f),
                };
                curveWeight = AnimationCurve.Linear(0, 0, 1, 1);
                isHumanoidAnimation = false;
            }
            else if(preset == Presets.Ahoge)
            {
                tailRoots = new List<Transform>();
                tailIgnores = new List<Transform>();
                tailaxi = new Vector3(0f,1f,0f);
                tailRots = new Vector3[5]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-40f, 0f, 0f),
                    new Vector3(40f, 0f, 0f),
                    new Vector3(0f, 0f, -30f),
                    new Vector3(0f, 0f, 30f),
                };
                curveWeight = AnimationCurve.Linear(0, 0, 1, 1);
                isHumanoidAnimation = false;
            }
            else if(preset == Presets.RightArm)
            {
                tailRoots = new List<Transform>();
                tailIgnores = new List<Transform>();
                var bones = avatar.GetComponent<Animator>().GetHumanBones();
                SetFromToBone(bones[(int)HumanBodyBones.RightUpperArm],bones[(int)HumanBodyBones.RightHand]);
                tailaxi = new Vector3(0f,0f,1f);
                tailRots = new Vector3[5]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-20f,0f, 0f),
                    new Vector3(20f, 0f, 0f),
                    new Vector3(0f, 20f, 0f),
                    new Vector3(0f, -20f, 0f),
                };
                curveWeight = AnimationCurve.Linear(0, 1, 1, 0);
                isHumanoidAnimation = true;
            }
            else if(preset == Presets.LeftArm)
            {
                tailRoots = new List<Transform>();
                tailIgnores = new List<Transform>();
                var bones = avatar.GetComponent<Animator>().GetHumanBones();
                SetFromToBone(bones[(int)HumanBodyBones.LeftUpperArm],bones[(int)HumanBodyBones.LeftHand]);
                tailaxi = new Vector3(0f,0f,1f);
                tailRots = new Vector3[5]
                {
                    new Vector3(0f, 0f, 0f),
                    new Vector3(-20f,0f, 0f),
                    new Vector3(20f, 0f, 0f),
                    new Vector3(0f, 20f, 0f),
                    new Vector3(0f, -20f, 0f),
                };
                curveWeight = AnimationCurve.Linear(0, 1, 1, 0);
                isHumanoidAnimation = true;
            }
        }

        int GetDescendantsCount(Transform p)
        {
            int m = 0;
            foreach (Transform c in p)
            {
                var v = GetDescendantsCount(c) + 1;
                m = Mathf.Max(m, v);
            }

            return m;
        }

        void SetupTail(Transform p,Transform root = null, int index = 0, int descendantsCount = 0)
        {
            if (root == null)
            {
                root = p;
                descendantsCount = GetDescendantsCount(p);
            }
            if(!defaultRots.ContainsKey(p)) defaultRots.Add(p, p.localRotation);
            foreach (Transform c in p)
            {
                var lookatobj  = p.position;
                if (Vector3.Distance(tailaxi, Vector3.zero) > 0.001f)
                {
                    lookatobj += tailaxi;
                }
                
                var r = Quaternion.FromToRotation(
                    Vector3.Normalize(c.position - p.position),
                    Vector3.Normalize(lookatobj - p.position));
                p.rotation = r * p.rotation;
                if(tailIgnores.Contains(c)) continue;
                SetupTail(c,root,index+1,descendantsCount);
            }

            if (!zeroRots.ContainsKey(p))
            {
                zeroRots.Add(p, p.localRotation);
                p.localRotation = zeroRots[p];
            }
            if(!curveWeightValue.ContainsKey(p)) curveWeightValue.Add(p,(float)index/(float)descendantsCount);
        }

        Vector3 GetTailRotValue(float[] weights)
        {
            Vector3 sum = Vector3.zero;
            float n = 0;
            for (int i = 0; i < Enum.GetNames(typeof(TailControlls)).Length; i++)
            {
                sum += tailRots[i] * weights[i];
                n += weights[i];
            }

            return sum / n;
        }

        void RotTail(float x,float y)
        {
            float[] ws = new float[]
            {
                1f,
                Mathf.Clamp01(y),
                Mathf.Clamp01(-y),
                Mathf.Clamp01(x),
                Mathf.Clamp01(-x),
            };
            Vector3 w = GetTailRotValue(ws);
            RotTail(w.x,w.y,w.z);
        }

        void RotTail(Vector3 value)
        {
            if (zeroRots == null) return;
            foreach (var rot in zeroRots)
            {
                //rot.Key.rotation = rot.Value;
                rot.Key.localRotation = rot.Value;
                rot.Key.rotation = Quaternion.Euler(value) * rot.Key.rotation;
            }
        }
        void RotTail(float xvalue,float yvalue,float zvalue)
        {
            RotTail(new Vector3(xvalue, yvalue, zvalue));
        }

        void ResetTail()
        {
            if(defaultRots==null) return;
            foreach (var rot in defaultRots)
            {
                rot.Key.localRotation = rot.Value;
            }
        }

        private void OnDestroy()
        {
            ResetTail();
        }

        private void SaveTailAnim(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);

            var param = dataname + "_Controll";
            var paramX = dataname + "_Controll_X";
            var paramY = dataname + "_Controll_Y";
            
            var controller = new AnimatorControllerCreator(param,param);
            
            var idle = new AnimationClipCreator("Idle",avatar.gameObject);
            
            var tree = new BlendTree();
            tree.name = "blend";
            tree.blendParameter = paramX;
            tree.blendParameterY = paramY;
            tree.blendType = BlendTreeType.SimpleDirectional2D;
            
            controller.AddDefaultState("Idle",idle.Create());
            controller.AddState("Reset",idle.Create());
            controller.AddState("Blend",tree);
            controller.AddTransition("Idle","Blend", param,true,false,0f,0.25f);
            controller.AddTransition("Blend","Reset",param,false,false,0f,0.25f);
            controller.AddTransition("Reset","Idle");
            if (preset == Presets.RightArm)
            {
#if VRC_SDK_VRCSDK3
                controller.SetAnimationTracking("Blend", AnimatorControllerCreator.VRCTrackingMask.RightHand,true);
                controller.SetAnimationTracking("Reset", AnimatorControllerCreator.VRCTrackingMask.RightHand,false);
#endif
                controller.LayerMask(AvatarMaskBodyPart.RightArm, true, false);
            }
            else if(preset == Presets.LeftArm)
            {
#if VRC_SDK_VRCSDK3
                controller.SetAnimationTracking("Blend", AnimatorControllerCreator.VRCTrackingMask.LeftHand,true);
                controller.SetAnimationTracking("Reset", AnimatorControllerCreator.VRCTrackingMask.LeftHand,false);
#endif
                controller.LayerMask(AvatarMaskBodyPart.LeftArm, true, false);
            }

            if (preset == Presets.RightArm)
            {
                // controller.SetWriteDefault("Idle",true);
                controller.LayerMask(AvatarMaskBodyPart.RightArm, true, false);
                controller.LayerTransformMask(avatar.gameObject,false);
            }
            else if(preset == Presets.LeftArm)
            {
               //  controller.SetWriteDefault("Idle",true);
                controller.LayerMask(AvatarMaskBodyPart.LeftArm, true, false);
                controller.LayerTransformMask(avatar.gameObject,false);
            }
            else if(!isHumanoidAnimation)
            {
                controller.LayerMask(AvatarMaskBodyPart.Body,false,false);
                controller.LayerTransformMask(avatar.gameObject,
                    zeroRots.Select(r => r.Key.gameObject).ToList(), true);
            }
            else
            {
                controller.SetWriteDefault("Idle",true);
                controller.LayerTransformMask(avatar.gameObject,false);
            }
            
            controller.AddParameter(tree.blendParameter,AnimatorControllerParameterType.Float);
            controller.AddParameter(tree.blendParameterY,AnimatorControllerParameterType.Float);
            var c = controller.CreateAsset(path);
            
            foreach (TailControlls controll in Enum.GetValues(typeof(TailControlls)))
            {
                RotTail(tailRots[(int)controll]);
                var anim = new AnimationClipCreator(controll.ToString(),avatar.gameObject);
                RecordAnimation(anim);
                tree.AddChild(anim.CreateAsset(path,true),controllValues[(int)controll]);
            }
            idle.CreateAsset(path, true);

#if VRC_SDK_VRCSDK3
            var menu = new MenuCreater(param);
            menu.AddAxis(param,controllIcons[(int) preset], param,paramX,paramY,
                "↑","→","↓","←",null,null,null,null);
            var p = new ParametersCreater(param);
            p.LoadParams(controller,false);
            
            var mod = new AvatarModifyTool(avatar,dir);
            AvatarModifyData assets = CreateInstance<AvatarModifyData>();
            {
                if (isHumanoidAnimation)
                {
                    assets.locomotion_controller = c;
                }
                else
                {
                    assets.fx_controller = c;
                }
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = menu.CreateAsset(path,true);
            }
            ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#else
#endif
            AssetDatabase.Refresh();
        }

        void SaveTailIdle(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            var dir = Path.GetDirectoryName(path);

            string param = dataname + "_Idle";
            
            var move = new AnimationClipCreator("idle",avatar.gameObject,false,true,true);
            RotTail(
                enableTestRotX ? -1f : testRotX,
                enableTestRotY ? -1f : testRotY);
            RecordAnimation(move,0f*idleSpeed + 0f*idleInertia, -1f);
            RotTail(
                enableTestRotX ? 0f : testRotX,
                enableTestRotY ? 0f : testRotY);
            RecordAnimation(move,1f*idleSpeed + 0f*idleInertia, 1f);
            RotTail(
                enableTestRotX ? 1f : testRotX,
                enableTestRotY ? 1f : testRotY);
            RecordAnimation(move,2f*idleSpeed + 0f*idleInertia, -1f);
            RecordAnimation(move,2f*idleSpeed + 1f*idleInertia, -1f);
            RotTail(
                enableTestRotX ? 0f : testRotX,
                enableTestRotY ? 0f : testRotY);
            RecordAnimation(move,3f*idleSpeed + 1f*idleInertia, 1f);
            RotTail(
                enableTestRotX ? -1f : testRotX,
                enableTestRotY ? -1f : testRotY);
            RecordAnimation(move,4f*idleSpeed + 1f*idleInertia, -1f);
            RecordAnimation(move,4f*idleSpeed + 2f*idleInertia, -1f);
            
            var idle = new AnimationClipCreator("Idle",avatar.gameObject);
            
            var controller = new AnimatorControllerCreator(param,param);
            controller.AddDefaultState("Idle",idle.Create());
            controller.AddState("Reset",idle.Create());
            controller.AddState("Move", move.Create());
            controller.SetStateSpeed("Move",param);
            controller.AddTransition("Idle","Move",param,0.001f,true,false,0f,0.25f);
            controller.AddTransition("Move","Reset",param,0.001f,false,false,0f,0.25f);
            controller.AddTransition("Reset","Idle");
            
            if (preset == Presets.RightArm)
            {
                // controller.SetWriteDefault("Idle",true);
                controller.LayerMask(AvatarMaskBodyPart.RightArm, true, false);
                controller.LayerTransformMask(avatar.gameObject,false);
            }
            else if(preset == Presets.LeftArm)
            {
                // controller.SetWriteDefault("Idle",true);
                controller.LayerMask(AvatarMaskBodyPart.LeftArm, true, false);
                controller.LayerTransformMask(avatar.gameObject,false);
            }
            else if(!isHumanoidAnimation)
            {
                controller.LayerMask(AvatarMaskBodyPart.Body,false,false);
                controller.LayerTransformMask(avatar.gameObject,
                    zeroRots.Select(r => r.Key.gameObject).ToList(), true);
            }
            else
            {
                controller.SetWriteDefault("Idle",true);
                controller.LayerTransformMask(avatar.gameObject,false);
            }

            var c = controller.CreateAsset(path);
            move.CreateAsset(path, true);
            idle.CreateAsset(path, true);
#if VRC_SDK_VRCSDK3
            var menu = new MenuCreater(dataname+"_Idle");
            menu.AddRadial(dataname+"_Idle",idleIcons[(int) preset],param);
            var p = new ParametersCreater(dataname+"_Idle");
            p.LoadParams(controller,true);
            
            var mod = new AvatarModifyTool(avatar,dir);
            AvatarModifyData assets = CreateInstance<AvatarModifyData>();
            {
                if (isHumanoidAnimation)
                {
                    assets.locomotion_controller = c;
                }
                else
                {
                    assets.fx_controller = c;
                }
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = menu.CreateAsset(path,true);
            }
            ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#endif
            AssetDatabase.Refresh();
        }

        void SetFromToBone(Transform from,Transform to)
        {
            if (from == null || to == null) return;
            tailIgnores.Add(to);
            var c = to;
            var p = c.parent;
            while (p != null)
            {
                foreach (Transform pc in p)
                {
                    if (pc != c)
                    {
                        tailIgnores.Add(pc);
                    }
                }
                //tailRoots.Add(p);
                c = p;
                p = p.parent;
                if (p == from) break;
            }
            tailRoots.Add(p);
        }

        void RecordAnimation(AnimationClipCreator anim,float time = 0f,float weight = 1f)
        {
            foreach (var rot in defaultRots)
            {
                if (isHumanoidAnimation)
                {
#if VRC_SDK_VRCSDK3
                    anim.AddKeyframe_Humanoid(avatarAnim,rot.Key,0f, weight);
#else
                    anim.AddKeyframe_Humanoid(avatar,rot.Key,0f, weight);
#endif
                }
                else
                {
                    anim.AddKeyframe_Transform(time,rot.Key,true,false,false, weight);
                }
            }
        }
    }
}