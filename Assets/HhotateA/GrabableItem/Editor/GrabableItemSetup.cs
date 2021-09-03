/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.GrabableItem
{
    public class GrabableItemSetup : WindowBase
    {
        [MenuItem("Window/HhotateA/アバターアイテムセットアップ(GrabableItemSetup)",false,5)]

        public static void ShowWindow()
        {
            var wnd = GetWindow<GrabableItemSetup>();
            wnd.titleContent = new GUIContent("GrabableItemSetup");
        }
        private GameObject handBone;
        private GameObject worldBone;
        private GameObject target;

        private Triggers grabTrigger = Triggers.RightHand_Fist;
        private Triggers dropTrigger = Triggers.Menu;

        private bool constraintMode = true;
        private bool safeMode = false;
        private bool deleateObject = true;

        private string saveName = "GrabControll";

        private void OnEnable()
        {
            worldBone = new GameObject("WorldAnchor");
            worldBone.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnGUI()
        {
            TitleStyle("アバターアイテムセットアップ(GrabableItemSetup)");
            DetailStyle(
                "アバターに付属したアイテムを，手に持ったり，ワールドに置いたりするための簡単なセットアップツールです．",
                EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3

            EditorGUILayout.Space();
            AvatartField("Avatar",
                () =>
                {
                    if (avatarAnim.isHuman)
                    {
                        handBone = avatarAnim.GetHumanBones()[(int) HumanBodyBones.RightHand].gameObject;
                    }
                });
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                // EditorGUILayout.LabelField("手に持つアイテム");
                var newTarget = (GameObject) EditorGUILayout.ObjectField("Object", target, typeof(GameObject), true);
                if (newTarget != target)
                {
                    if (HasRootParent(newTarget.transform, avatar.transform))
                    {
                        target = newTarget;
                        saveName = target.name + "GrabControll";
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                // EditorGUILayout.LabelField("手のボーン：オブジェクトを持つトリガー");
                using (new EditorGUILayout.HorizontalScope())
                {
                    handBone = (GameObject) EditorGUILayout.ObjectField("HandBone", handBone, typeof(GameObject), true);
                    grabTrigger = (Triggers) EditorGUILayout.EnumPopup("", grabTrigger);
                }

                EditorGUILayout.Space();
                
                if (constraintMode)
                {
                    // EditorGUILayout.LabelField("：オブジェクトを置くトリガー");
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            worldBone = (GameObject) EditorGUILayout.ObjectField("WorldBone", worldBone,
                                typeof(GameObject),
                                true);
                        }

                        dropTrigger = (Triggers) EditorGUILayout.EnumPopup("", dropTrigger);
                    }
                }

                EditorGUILayout.Space();

                constraintMode = EditorGUILayout.Toggle("Use Constraint", constraintMode);

                EditorGUILayout.Space();
                
                safeMode = EditorGUILayout.Toggle("Safe Original Item", safeMode);

                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
            if (ShowOptions())
            {
                if (GUILayout.Button("Force Revert"))
                {
                    var am = new AvatarModifyTool(avatar);
                    am.RevertByKeyword(EnvironmentGUIDs.prefix);
                    OnFinishRevert();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(avatar == null ||
                                               target == null ||
                                               handBone == null))
            {
                if (GUILayout.Button("Setup"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", "Assets", target.name + "GrabControll",
                        "controller");
                    if (string.IsNullOrEmpty(path))
                    {
                        OnCancel();
                        return;
                    }
                    saveName = System.IO.Path.GetFileNameWithoutExtension(path);
                    path = FileUtil.GetProjectRelativePath(path);
                    try
                    {
                        if (constraintMode)
                        {
                            ConstraintSetup(path);
                        }
                        else
                        {
                            SimpleSetup(path);
                        }
                        OnFinishSetup();
                    }
                    catch (Exception e)
                    {
                        OnError(e);
                        throw;
                    }
                    OnFinishSetup();
                }
            }
            status.Display();
#else
            VRCErrorLabel();
#endif
            Signature();
        }

        void DeleateSettingObjects()
        {
            while (true)
            {
                var wp = avatar.transform.RecursiveFindChild(EnvironmentGUIDs.prefix + saveName + "_WorldPoint");
                if (wp)
                {
                    DestroyImmediate(wp.gameObject);
                    continue;
                }
                var wa = avatar.transform.RecursiveFindChild(EnvironmentGUIDs.prefix + saveName + "_WorldAnchor");
                if (wa)
                {
                    DestroyImmediate(wa.gameObject);
                    continue;
                }
                var ia = avatar.transform.RecursiveFindChild(EnvironmentGUIDs.prefix + saveName + "_ItemShip");
                if (ia)
                {
                    DestroyImmediate(ia.gameObject);
                    continue;
                }
                var ha = avatar.transform.RecursiveFindChild(EnvironmentGUIDs.prefix + saveName + "_HandAnchor");
                if (ha)
                {
                    DestroyImmediate(ha.gameObject);
                    continue;
                }
                var ra = avatar.transform.RecursiveFindChild(EnvironmentGUIDs.prefix + saveName + "_RootAnchor");
                if (ra)
                {
                    DestroyImmediate(ra.gameObject);
                    continue;
                }
                break;
            }
        }

        void ConstraintSetup(string path)
        {
            if (deleateObject)
            {
                DeleateSettingObjects();
            }
            if (grabTrigger == Triggers.None && dropTrigger == Triggers.None)
            {
                return;
            }
            else
            if(grabTrigger == Triggers.None)
            {
                DropSetup(path);
            }
            else
            if(dropTrigger == Triggers.None)
            {
                GrabSetup(path);
            }
            else
            {
                FullSetup(path);
            }
        }

        void GrabSetup(string path)
        {
            GameObject itemAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_ItemShip");
            itemAnchor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            itemAnchor.transform.SetParent(avatar.transform);
            
            GameObject handAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_HandAnchor");
            handAnchor.transform.SetPositionAndRotation(handBone.transform.position,handBone.transform.rotation);
            handAnchor.transform.SetParent(handBone.transform);
            
            GameObject rootAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_RootAnchor");
            rootAnchor.transform.SetPositionAndRotation(target.transform.position,target.transform.rotation);
            rootAnchor.transform.SetParent(target.transform.parent);
            
            var itemConst = itemAnchor.AddComponent<ParentConstraint>();
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = rootAnchor.transform,
                weight = 1f
            });
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = handAnchor.transform,
                weight = 0f
            });
            itemConst.weight = 1f;
            itemConst.translationAtRest = Vector3.zero;
            itemConst.rotationAtRest = Vector3.zero;
            itemConst.locked = true;
            itemConst.constraintActive = true;

            var c = new AnimatorControllerCreator(saveName);
            var resetAnim = new AnimationClipCreator("Reset",avatar.gameObject);
            resetAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            resetAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[0].weight",1f);
            resetAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[1].weight",0f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[0].weight",1f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[1].weight",0f);
            
            var grabAnim = new AnimationClipCreator("Grab",avatar.gameObject);
            grabAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            grabAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[0].weight",0f);
            grabAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[1].weight",1f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[0].weight",0f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[1].weight",1f);

            if (safeMode)
            {
                var clone = GameObject.Instantiate(target,itemAnchor.transform);
                clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                clone.SetActive(false);
                
                resetAnim.AddKeyframe_Gameobject(target,0f,true);
                resetAnim.AddKeyframe_Gameobject(target,1f/60f,true);
                resetAnim.AddKeyframe_Gameobject(clone,0f,false);
                resetAnim.AddKeyframe_Gameobject(clone,1f/60f,false);
                
                grabAnim.AddKeyframe_Gameobject(target,0f,false);
                grabAnim.AddKeyframe_Gameobject(target,1f/60f,false);
                grabAnim.AddKeyframe_Gameobject(clone,0f,true);
                grabAnim.AddKeyframe_Gameobject(clone,1f/60f,true);
            }
            else
            {
                var targetConst = target.GetComponent<ParentConstraint>();
                if(targetConst) DestroyImmediate(targetConst);
                targetConst = target.AddComponent<ParentConstraint>();
                targetConst.AddSource(new ConstraintSource()
                {
                    sourceTransform = itemAnchor.transform,
                    weight = 1f
                });
                targetConst.weight = 1f;
                targetConst.translationAtRest = Vector3.zero;
                targetConst.rotationAtRest = Vector3.zero;
                targetConst.locked = true;
                targetConst.constraintActive = true;
            }
            
            c.AddDefaultState("Idle",resetAnim.Create());
            c.AddState("Reset",resetAnim.Create());
            c.AddState("Grab", grabAnim.Create());

            string paramGrab = saveName + "_Grab";
            string paramDrop = saveName + "_Drop";
            c.AddParameter(paramGrab,false);
            c.AddParameter(paramDrop,false);
            c.AddParameter("GestureLeft",0);
            c.AddParameter("GestureRight",0);
            
            if (grabTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Grab",paramGrab,true,true,1f,0.2f);
                c.AddTransition("Grab","Reset",paramGrab,false,true,1f,0.2f);
            }
            else
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramGrab,
                    threshold = 1,
                },GetGrabTrigger(grabTrigger,true) },true,1f,0.2f);
                c.AddTransition("Grab","Reset",paramGrab,false, true,1f,0.2f);
                c.AddTransition("Grab","Reset",new AnimatorCondition[1]{ GetGrabTrigger(grabTrigger,false)},true,1f,0.2f);
            }
            c.AddTransition("Reset","Idle");
            
            c.CreateAsset(path);
            resetAnim.CreateAsset(path, true);
            grabAnim.CreateAsset(path, true);
            
#if VRC_SDK_VRCSDK3
            var m = new MenuCreater(saveName);
            m.AddToggle("Grab",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon),paramGrab);
            
            var p = new ParametersCreater(saveName);
            p.AddParam(paramGrab,false,false);
            p.AddParam(paramDrop,false,false);
            
            var mod = new AvatarModifyTool(avatar,path);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.Create();
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = m.CreateAsset(path, true);
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#endif
        }

        void DropSetup(string path)
        {

            GameObject worldPoint = avatar.transform.Find(EnvironmentGUIDs.prefix + saveName + "_WorldPoint")?.gameObject;
            if (!worldPoint) worldPoint = new GameObject(EnvironmentGUIDs.prefix + saveName + "_WorldPoint");
            worldPoint.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            worldPoint.transform.SetParent(avatar.transform);
            
            GameObject worldAnchor = worldPoint.transform.Find(EnvironmentGUIDs.prefix + saveName + "_WorldAnchor")?.gameObject;
            if (!worldAnchor) worldAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_WorldAnchor");
            worldAnchor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            worldAnchor.transform.SetParent(worldPoint.transform);
            var worldConstP = worldAnchor.GetComponent<PositionConstraint>();
            if (!worldConstP)
            {
                worldConstP = worldAnchor.AddComponent<PositionConstraint>();
                worldConstP.AddSource(new ConstraintSource()
                {
                    sourceTransform = worldPoint.transform,
                    weight = -1f
                });
                worldConstP.weight = 0.5f;
                worldConstP.translationAtRest = Vector3.zero;
                worldConstP.locked = true;
                worldConstP.constraintActive = true;
            }
            var worldConstR = worldAnchor.GetComponent<RotationConstraint>();
            if (!worldConstR)
            {
                worldConstR = worldAnchor.AddComponent<RotationConstraint>();
                worldConstR.AddSource(new ConstraintSource()
                {
                    sourceTransform = worldPoint.transform,
                    weight = -0.5f
                });
                worldConstR.weight = 1f;
                worldConstR.rotationAtRest = Vector3.zero;
                worldConstR.locked = true;
                worldConstR.constraintActive = true;
            }

            GameObject itemAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_ItemShip");
            itemAnchor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            itemAnchor.transform.SetParent(worldAnchor.transform);

            GameObject rootAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_RootAnchor");
            rootAnchor.transform.SetPositionAndRotation(target.transform.position,target.transform.rotation);
            rootAnchor.transform.SetParent(target.transform.parent);
            
            var itemConst = itemAnchor.AddComponent<ParentConstraint>();
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = rootAnchor.transform,
                weight = 1f
            });
            itemConst.weight = 1f;
            itemConst.translationAtRest = Vector3.zero;
            itemConst.rotationAtRest = Vector3.zero;
            itemConst.locked = true;
            itemConst.constraintActive = true;

            var c = new AnimatorControllerCreator(saveName);
            var resetAnim = new AnimationClipCreator("Reset",avatar.gameObject);
            resetAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            
            var dropAnim = new AnimationClipCreator("Drop",avatar.gameObject);
            dropAnim.AddKeyframe(0f,itemConst,"m_Enabled",0f);
            dropAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",0f);

            if (safeMode)
            {
                var clone = GameObject.Instantiate(target,itemAnchor.transform);
                clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                clone.SetActive(false);
                
                resetAnim.AddKeyframe_Gameobject(target,0f,true);
                resetAnim.AddKeyframe_Gameobject(target,1f/60f,true);
                resetAnim.AddKeyframe_Gameobject(clone,0f,false);
                resetAnim.AddKeyframe_Gameobject(clone,1f/60f,false);
                
                dropAnim.AddKeyframe_Gameobject(target,0f,false);
                dropAnim.AddKeyframe_Gameobject(target,1f/60f,false);
                dropAnim.AddKeyframe_Gameobject(clone,0f,true);
                dropAnim.AddKeyframe_Gameobject(clone,1f/60f,true);
            }
            else
            {
                var targetConst = target.GetComponent<ParentConstraint>();
                if(targetConst) DestroyImmediate(targetConst);
                targetConst = target.AddComponent<ParentConstraint>();
                targetConst.AddSource(new ConstraintSource()
                {
                    sourceTransform = itemAnchor.transform,
                    weight = 1f
                });
                targetConst.weight = 1f;
                targetConst.translationAtRest = Vector3.zero;
                targetConst.rotationAtRest = Vector3.zero;
                targetConst.locked = true;
                targetConst.constraintActive = true;
            }
            
            c.AddDefaultState("Idle",resetAnim.Create());
            c.AddState("Reset",resetAnim.Create());
            c.AddState("Drop", dropAnim.Create());

            string paramDrop = saveName + "_Drop";
            c.AddParameter(paramDrop,false);
            c.AddParameter("GestureLeft",0);
            c.AddParameter("GestureRight",0);
            
            if (dropTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Drop",paramDrop,true,true,1f,0f);
                c.AddTransition("Drop","Reset",paramDrop,false);
            }
            else
            {
                c.AddTransition("Idle","Drop",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramDrop,
                    threshold = 1,
                },GetGrabTrigger(dropTrigger,true) },true,1f,0f);

                c.AddTransition("Drop", "Reset", paramDrop, false);
                c.AddTransition("Drop","Reset",new AnimatorCondition[1]{ GetGrabTrigger(dropTrigger,false)},true,1f,0f);
            }
            c.AddTransition("Reset","Idle");
            
            c.CreateAsset(path);
            resetAnim.CreateAsset(path, true);
            dropAnim.CreateAsset(path, true);
            
#if VRC_SDK_VRCSDK3
            var m = new MenuCreater(saveName);
            m.AddToggle("Drop",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.dropIcon),paramDrop);
            
            var p = new ParametersCreater(saveName);
            p.AddParam(paramDrop,false,false);
            
            var mod = new AvatarModifyTool(avatar,path);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.Create();
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = m.CreateAsset(path, true);
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#endif
        }

        void FullSetup(string path)
        {
            GameObject worldPoint = avatar.transform.Find(EnvironmentGUIDs.prefix + saveName + "_WorldPoint")?.gameObject;
            if (!worldPoint) worldPoint = new GameObject(EnvironmentGUIDs.prefix + saveName + "_WorldPoint");
            worldPoint.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            worldPoint.transform.SetParent(avatar.transform);
            
            GameObject worldAnchor = worldPoint.transform.Find(EnvironmentGUIDs.prefix + saveName + "_WorldAnchor")?.gameObject;
            if (!worldAnchor) worldAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_WorldAnchor");
            worldAnchor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            worldAnchor.transform.SetParent(worldPoint.transform);
            var worldConstP = worldAnchor.GetComponent<PositionConstraint>();
            if (!worldConstP)
            {
                worldConstP = worldAnchor.AddComponent<PositionConstraint>();
                worldConstP.AddSource(new ConstraintSource()
                {
                    sourceTransform = worldPoint.transform,
                    weight = -1f
                });
                worldConstP.weight = 0.5f;
                worldConstP.translationAtRest = Vector3.zero;
                worldConstP.locked = true;
                worldConstP.constraintActive = true;
            }
            var worldConstR = worldAnchor.GetComponent<RotationConstraint>();
            if (!worldConstR)
            {
                worldConstR = worldAnchor.AddComponent<RotationConstraint>();
                worldConstR.AddSource(new ConstraintSource()
                {
                    sourceTransform = worldPoint.transform,
                    weight = -0.5f
                });
                worldConstR.weight = 1f;
                worldConstR.rotationAtRest = Vector3.zero;
                worldConstR.locked = true;
                worldConstR.constraintActive = true;
            }
            
            GameObject itemAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_ItemShip");
            itemAnchor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            itemAnchor.transform.SetParent(worldAnchor.transform);
            
            GameObject handAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_HandAnchor");
            handAnchor.transform.SetPositionAndRotation(handBone.transform.position,handBone.transform.rotation);
            handAnchor.transform.SetParent(handBone.transform);
            
            GameObject rootAnchor = new GameObject(EnvironmentGUIDs.prefix + saveName + "_RootAnchor");
            rootAnchor.transform.SetPositionAndRotation(target.transform.position,target.transform.rotation);
            rootAnchor.transform.SetParent(target.transform.parent);
            
            var itemConst = itemAnchor.AddComponent<ParentConstraint>();
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = rootAnchor.transform,
                weight = 1f
            });
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = handAnchor.transform,
                weight = 0f
            });
            itemConst.weight = 1f;
            itemConst.translationAtRest = Vector3.zero;
            itemConst.rotationAtRest = Vector3.zero;
            itemConst.locked = true;
            itemConst.constraintActive = true;

            var c = new AnimatorControllerCreator(saveName);
            var resetAnim = new AnimationClipCreator("Reset",avatar.gameObject);
            resetAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            resetAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[0].weight",1f);
            resetAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[1].weight",0f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[0].weight",1f);
            resetAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[1].weight",0f);
            
            var grabAnim = new AnimationClipCreator("Grab",avatar.gameObject);
            grabAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            grabAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[0].weight",0f);
            grabAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[1].weight",1f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[0].weight",0f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[1].weight",1f);
            
            var dropAnim = new AnimationClipCreator("Drop",avatar.gameObject);
            dropAnim.AddKeyframe(0f,itemConst,"m_Enabled",0f);
            dropAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",0f);

            if (safeMode)
            {
                var clone = GameObject.Instantiate(target,itemAnchor.transform);
                clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                clone.SetActive(false);
                
                resetAnim.AddKeyframe_Gameobject(target,0f,true);
                resetAnim.AddKeyframe_Gameobject(target,1f/60f,true);
                resetAnim.AddKeyframe_Gameobject(clone,0f,false);
                resetAnim.AddKeyframe_Gameobject(clone,1f/60f,false);
                
                grabAnim.AddKeyframe_Gameobject(target,0f,false);
                grabAnim.AddKeyframe_Gameobject(target,1f/60f,false);
                grabAnim.AddKeyframe_Gameobject(clone,0f,true);
                grabAnim.AddKeyframe_Gameobject(clone,1f/60f,true);
                
                dropAnim.AddKeyframe_Gameobject(target,0f,false);
                dropAnim.AddKeyframe_Gameobject(target,1f/60f,false);
                dropAnim.AddKeyframe_Gameobject(clone,0f,true);
                dropAnim.AddKeyframe_Gameobject(clone,1f/60f,true);
            }
            else
            {
                var targetConst = target.GetComponent<ParentConstraint>();
                if(targetConst) DestroyImmediate(targetConst);
                targetConst = target.AddComponent<ParentConstraint>();
                targetConst.AddSource(new ConstraintSource()
                {
                    sourceTransform = itemAnchor.transform,
                    weight = 1f
                });
                targetConst.weight = 1f;
                targetConst.translationAtRest = Vector3.zero;
                targetConst.rotationAtRest = Vector3.zero;
                targetConst.locked = true;
                targetConst.constraintActive = true;
            }
            
            c.AddDefaultState("Idle",resetAnim.Create());
            c.AddState("Reset",resetAnim.Create());
            c.AddState("Grab", grabAnim.Create());
            c.AddState("Drop", dropAnim.Create());

            string paramGrab = saveName + "_Grab";
            string paramDrop = saveName + "_Drop";
            c.AddParameter(paramGrab,false);
            c.AddParameter(paramDrop,false);
            c.AddParameter("GestureLeft",0);
            c.AddParameter("GestureRight",0);
            
            c.AddTransition("Reset","Idle");
            
            if (grabTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Grab",paramGrab,true,true,1f,0.2f);
                c.AddTransition("Grab","Reset",paramGrab,false,true,1f,0.2f);
            }
            else
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramGrab,
                    threshold = 1,
                },GetGrabTrigger(grabTrigger,true) },true,1f,0.2f);
                c.AddTransition("Grab","Reset",paramGrab,false, true,1f,0.2f);
                c.AddTransition("Grab","Reset",new AnimatorCondition[1]{ GetGrabTrigger(grabTrigger,false)},true,1f,0.2f);
            }

            if (dropTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Drop",paramDrop,true,true,1f,0f);
                c.AddTransition("Drop","Grab",paramDrop,false);
                
                c.AddTransition("Grab","Drop",paramDrop,true,true,1f,0f);
            }
            else
            {
                c.AddTransition("Idle","Drop",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramDrop,
                    threshold = 1,
                },GetGrabTrigger(dropTrigger,true) },true,1f,0f);

                c.AddTransition("Drop", "Grab", paramDrop, false);
                c.AddTransition("Drop","Grab",new AnimatorCondition[1]{ GetGrabTrigger(dropTrigger,false)},true,1f,0f);
                
                c.AddTransition("Grab","Drop",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramDrop,
                    threshold = 1,
                },GetGrabTrigger(dropTrigger,true) },true,1f,0f);
            }
            
            c.CreateAsset(path);
            resetAnim.CreateAsset(path, true);
            grabAnim.CreateAsset(path, true);
            dropAnim.CreateAsset(path, true);
            
#if VRC_SDK_VRCSDK3
            var m = new MenuCreater(saveName);
            m.AddToggle("Grab",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon),paramGrab);
            m.AddToggle("Drop",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.dropIcon),paramDrop);
            
            var mp = new MenuCreater("ParentMenu");
            mp.AddSubMenu(m.CreateAsset(path, true),target.name + "_GrabControll",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon));
            
            var p = new ParametersCreater(saveName);
            p.AddParam(paramGrab,false,false);
            p.AddParam(paramDrop,false,false);
            
            var mod = new AvatarModifyTool(avatar,path);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.Create();
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = mp.CreateAsset(path, true);
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#endif
        }

        void SimpleSetup(string path)
        {
            if (grabTrigger == Triggers.None)
            {
                return;
            }
            
            if (deleateObject)
            {
                var go = handBone.transform.FindInChildren(EnvironmentGUIDs.prefix + saveName);
                if(go) DestroyImmediate(go);
            }
            var grabObj = GameObject.Instantiate(target,handBone.transform,false);
            grabObj.name = EnvironmentGUIDs.prefix + saveName;
            
            string paramGrab = saveName + "_Grab";

            var c = new AnimatorControllerCreator(paramGrab);
            var resetAnim = new AnimationClipCreator("Reset",avatar.gameObject);
            resetAnim.AddKeyframe_Gameobject(target,0f,true);
            resetAnim.AddKeyframe_Gameobject(grabObj,0f,false);
            resetAnim.AddKeyframe_Gameobject(target,1f/60f,true);
            resetAnim.AddKeyframe_Gameobject(grabObj,1f/60f,false);
            var grabAnim = new AnimationClipCreator("Grab",avatar.gameObject);
            grabAnim.AddKeyframe_Gameobject(target,0f,false);
            grabAnim.AddKeyframe_Gameobject(grabObj,0f,true);
            grabAnim.AddKeyframe_Gameobject(target,1f/60f,false);
            grabAnim.AddKeyframe_Gameobject(grabObj,1f/60f,true);
            
            c.AddDefaultState("Idle");
            c.AddState("Reset",resetAnim.Create());
            c.AddState("Grab", grabAnim.Create());
            
            c.AddParameter("LeftHand_Idle",0);
            c.AddParameter("GestureRight",0);
            
            c.AddTransition("Reset","Idle");
            
            if (grabTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = paramGrab,
                    threshold = 1,
                }},true,1f,0f);
                c.AddTransition("Grab","Reset",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.NotEqual,
                    parameter = paramGrab,
                    threshold = 1,
                }},true,1f,0f);
            }
            else
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = paramGrab,
                    threshold = 1,
                },GetGrabTrigger(grabTrigger,true) },true,1f,0.2f);
                
                c.AddTransition("Grab","Reset",new AnimatorCondition[1]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.NotEqual,
                    parameter = paramGrab,
                    threshold = 1,
                }},true,1f,0.2f);
                c.AddTransition("Grab","Reset",new AnimatorCondition[1]{ GetGrabTrigger(grabTrigger,false)},true,1f,0.2f);
                
                c.CreateAsset(path);
                resetAnim.CreateAsset(path, true);
                grabAnim.CreateAsset(path, true);
            
#if VRC_SDK_VRCSDK3
                var m = new MenuCreater(saveName);
                m.AddToggle(target.name + "_GrabControll",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon),paramGrab);
            
                var p = new ParametersCreater(paramGrab);
                p.AddParam(paramGrab,false,false);
            
                var mod = new AvatarModifyTool(avatar,path);
                var assets = CreateInstance<AvatarModifyData>();
                {
                    assets.fx_controller = c.Create();
                    assets.parameter = p.CreateAsset(path, true);
                    assets.menu = m.CreateAsset(path, true);
                }
                AssetDatabase.AddObjectToAsset(assets,path);
                ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#endif
            }
        }

        bool IsInsideHead(GameObject obj)
        {
            var anim = avatar.GetComponent<Animator>();
            var head = anim?.GetHumanBones()?[(int) HumanBodyBones.Head];
            return HasRootParent(obj.transform, head);
        }

        bool HasRootParent(Transform obj,Transform root)
        {
            if (obj.parent == null) return false;
            if (obj.parent == root) return true;
            return HasRootParent(obj.parent,root);
        }

        private void OnDestroy()
        {
            DestroyImmediate(worldBone);
        }
        
        string GetRelativePath(Transform o)
        {
            return AssetUtility.GetRelativePath(avatar.transform, o.transform);
        }

        AnimatorCondition GetGrabTrigger(Triggers trigger,bool equal = true)
        {
            var ac = new AnimatorCondition();
            ac.mode = equal ? AnimatorConditionMode.Equals : AnimatorConditionMode.NotEqual;
            if (trigger == Triggers.RightHand_Idle)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 0;
            }
            else
            if (trigger == Triggers.RightHand_Fist)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 1;
            }
            else
            if (trigger == Triggers.RightHand_Open)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 2;
            }
            else
            if (trigger == Triggers.RightHand_Point)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 3;
            }
            else
            if (trigger == Triggers.RightHand_Peace)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 4;
            }
            else
            if (trigger == Triggers.RightHand_RocknRoll)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 5;
            }
            else
            if (trigger == Triggers.RightHand_Gun)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 6;
            }
            else
            if (trigger == Triggers.RightHand_ThumbsUp)
            {
                ac.parameter = "GestureRight";
                ac.threshold = 7;
            }
            else
            if (trigger == Triggers.LeftHand_Idle)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 0;
            }
            else
            if (trigger == Triggers.LeftHand_Fist)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 1;
            }
            else
            if (trigger == Triggers.LeftHand_Open)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 2;
            }
            else
            if (trigger == Triggers.LeftHand_Point)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 3;
            }
            else
            if (trigger == Triggers.LeftHand_Peace)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 4;
            }
            else
            if (trigger == Triggers.LeftHand_RocknRoll)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 5;
            }
            else
            if (trigger == Triggers.LeftHand_Gun)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 6;
            }
            else
            if (trigger == Triggers.LeftHand_ThumbsUp)
            {
                ac.parameter = "GestureLeft";
                ac.threshold = 7;
            }

            return ac;
        }

        enum Triggers
        {
            None,
            RightHand_Idle,
            RightHand_Fist,
            RightHand_Open,
            RightHand_Point,
            RightHand_Peace,
            RightHand_RocknRoll,
            RightHand_Gun,
            RightHand_ThumbsUp,
            LeftHand_Idle,
            LeftHand_Fist,
            LeftHand_Open,
            LeftHand_Point,
            LeftHand_Peace,
            LeftHand_RocknRoll,
            LeftHand_Gun,
            LeftHand_ThumbsUp,
            Menu,
        }
    }
}