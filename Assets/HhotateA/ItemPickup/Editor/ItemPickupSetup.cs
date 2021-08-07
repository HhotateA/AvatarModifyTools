using System;
using System.Collections.Generic;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEngine.Animations;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.ItemPickup
{
    public class ItemPickupSetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/アバターアイテムセットアップ(ItemPickupSetup)",false,5)]

        public static void ShowWindow()
        {
            var wnd = GetWindow<ItemPickupSetup>();
            wnd.titleContent = new GUIContent("ItemPickupSetup");
        }
#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor avatar;
#else
        private Animator avatar;
#endif
        private GameObject handBone;
        private GameObject worldBone;
        private GameObject target;

        private Triggers grabTrigger = Triggers.RightHand_Fist;
        private Triggers dropTrigger = Triggers.Menu;

        private bool constraintMode = true;
        private bool deleateObject = true;
        private bool writeDefault = false;
        private bool notRecommended = false;

        private void OnEnable()
        {
            worldBone = new GameObject("WorldAnchor");
            worldBone.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnGUI()
        {
#if VRC_SDK_VRCSDK3
            var newAvatar = (VRCAvatarDescriptor) EditorGUILayout.ObjectField("Avatar", avatar, typeof(VRCAvatarDescriptor), true);
#else
            var newAvatar = (Animator) EditorGUILayout.ObjectField("Avatar", avatar, typeof(Animator), true);
#endif
            if (newAvatar)
            {
                if (newAvatar != avatar)
                {
                    avatar = newAvatar;
                    var anim = newAvatar.GetComponent<Animator>();
                    if (anim)
                    {
                        if (anim.isHuman)
                        {
                            handBone = anim.GetHumanBones()[(int) HumanBodyBones.RightHand].gameObject;
                        }
                    }
                }
            }
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                var newTarget = (GameObject) EditorGUILayout.ObjectField("Object", target, typeof(GameObject), true);
                if (newTarget != target)
                {
                    if (HasRootParent(newTarget.transform, avatar.transform))
                    {
                        target = newTarget;
                    }
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    handBone = (GameObject) EditorGUILayout.ObjectField("HandBone", handBone, typeof(GameObject), true);
                    grabTrigger = (Triggers) EditorGUILayout.EnumPopup("", grabTrigger);
                }

                EditorGUILayout.Space();

                constraintMode = EditorGUILayout.Toggle("Use Constraint", constraintMode);

                EditorGUILayout.Space();

                if (constraintMode)
                {
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
            }

            EditorGUILayout.Space();
            notRecommended = EditorGUILayout.Foldout(notRecommended,"VRChat Not Recommended");
            if (notRecommended)
            {
                writeDefault = EditorGUILayout.Toggle("Write Default", writeDefault); 
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(avatar == null ||
                                               target == null ||
                                               handBone == null))
            {
                if (GUILayout.Button("Setup"))
                {
                    var path = EditorUtility.SaveFilePanel("Save", "Assets", target.name + "Controll",
                        "controller");
                    if (string.IsNullOrWhiteSpace(path)) return;
                    path = FileUtil.GetProjectRelativePath(path);
                    if (constraintMode)
                    {
                        ConstraintSetup(path);
                    }
                    else
                    {
                        SimpleSetup(path);
                    }
                }
            }
        }

        void ConstraintSetup(string path)
        {
            GameObject worldPoint = avatar.transform.FindInChildren("WorldPoint")?.gameObject;
            if (!worldPoint) worldPoint = new GameObject("WorldPoint");
            worldPoint.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            worldPoint.transform.SetParent(avatar.transform);
            
            GameObject worldAnchor = worldPoint.transform.FindInChildren("WorldAnchor")?.gameObject;
            if (!worldAnchor) worldAnchor = new GameObject("WorldAnchor");
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
            
            if (deleateObject)
            {
                var ia = worldAnchor.transform.FindInChildren("ItemShip_" + target.name);
                if(ia) DestroyImmediate(ia.gameObject);
                var ha = handBone.transform.FindInChildren("HandAnchor_" + target.name);
                if(ha) DestroyImmediate(ha.gameObject);
                var ra = target.transform.parent.FindInChildren("RootAnchor_" + target.name);
                if(ra) DestroyImmediate(ra.gameObject);
            }
            
            GameObject itemAnchor = new GameObject("ItemShip_" + target.name);
            itemAnchor.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            itemAnchor.transform.SetParent(worldAnchor.transform);
            
            GameObject handAnchor = new GameObject("HandAnchor_" + target.name);
            handAnchor.transform.SetPositionAndRotation(handBone.transform.position,handBone.transform.rotation);
            handAnchor.transform.SetParent(handBone.transform);
            
            GameObject rootAnchor = new GameObject("RootAnchor_" + target.name);
            rootAnchor.transform.SetPositionAndRotation(target.transform.position,target.transform.rotation);
            rootAnchor.transform.SetParent(target.transform.parent);

            var clone = GameObject.Instantiate(target,itemAnchor.transform);
            clone.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            clone.SetActive(false);
            
            var itemConst = itemAnchor.AddComponent<ParentConstraint>();
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = rootAnchor.transform,
                weight = 0f
            });
            itemConst.AddSource(new ConstraintSource()
            {
                sourceTransform = handAnchor.transform,
                weight = 1f
            });
            itemConst.weight = 1f;
            itemConst.translationAtRest = Vector3.zero;
            itemConst.rotationAtRest = Vector3.zero;
            itemConst.locked = true;
            itemConst.constraintActive = true;

            var c = new AnimatorControllerCreator("Pickup_"+target.name);
            var idleAnim = new AnimationClipCreator("Idle",avatar.gameObject);
            idleAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            idleAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[0].weight",1f);
            idleAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[1].weight",0f);
            idleAnim.AddKeyframe_Gameobject(target,0f,true);
            idleAnim.AddKeyframe_Gameobject(clone,0f,false);
            idleAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            idleAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[0].weight",1f);
            idleAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[1].weight",0f);
            idleAnim.AddKeyframe_Gameobject(target,1f/60f,true);
            idleAnim.AddKeyframe_Gameobject(clone,1f/60f,false);
            
            var grabAnim = new AnimationClipCreator("Grab",avatar.gameObject);
            grabAnim.AddKeyframe(0f,itemConst,"m_Enabled",1f);
            grabAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[0].weight",0f);
            grabAnim.AddKeyframe(0f,itemConst,"m_Sources.Array.data[1].weight",1f);
            grabAnim.AddKeyframe_Gameobject(target,0f,false);
            grabAnim.AddKeyframe_Gameobject(clone,0f,true);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",1f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[0].weight",0f);
            grabAnim.AddKeyframe(1f/60f,itemConst,"m_Sources.Array.data[1].weight",1f);
            grabAnim.AddKeyframe_Gameobject(target,1f/60f,false);
            grabAnim.AddKeyframe_Gameobject(clone,1f/60f,true);
            
            var dropAnim = new AnimationClipCreator("Drop",avatar.gameObject);
            dropAnim.AddKeyframe(0f,itemConst,"m_Enabled",0f);
            dropAnim.AddKeyframe_Gameobject(target,0f,false);
            dropAnim.AddKeyframe_Gameobject(clone,0f,true);
            dropAnim.AddKeyframe(1f/60f,itemConst,"m_Enabled",0f);
            dropAnim.AddKeyframe_Gameobject(target,1f/60f,false);
            dropAnim.AddKeyframe_Gameobject(clone,1f/60f,true);
            
            c.AddDefaultState("Idle",idleAnim.Create());
            c.AddState("Grab", grabAnim.Create());
            c.AddState("Drop", dropAnim.Create());

            string paramGrab = target.name + "_Grab";
            string paramDrop = target.name + "_Drop";
            c.AddParameter(paramGrab,false);
            c.AddParameter(paramDrop,false);
            c.AddParameter("GestureLeft",0);
            c.AddParameter("GestureRight",0);
            
            if (grabTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramGrab,
                    threshold = 1,
                }},true,1f,0.2f);
                c.AddTransition("Grab","Idle",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.IfNot,
                    parameter = paramGrab,
                    threshold = 0,
                }},true,1f,0.2f);
            }
            else
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[2]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramGrab,
                    threshold = 1,
                },GetGrabTrigger(grabTrigger,true) },true,1f,0.2f);
                c.AddTransition("Grab","Idle",paramGrab,false, true,1f,0.2f);
                c.AddTransition("Grab","Idle",new AnimatorCondition[1]{ GetGrabTrigger(grabTrigger,false)},true,1f,0.2f);
            }

            if (dropTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Drop",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramDrop,
                    threshold = 1,
                }},true,1f,0f);
                c.AddTransition("Drop","Grab",paramDrop,false);
                
                c.AddTransition("Grab","Drop",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.If,
                    parameter = paramDrop,
                    threshold = 1,
                }},true,1f,0f);
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
            idleAnim.CreateAsset(path, true);
            grabAnim.CreateAsset(path, true);
            dropAnim.CreateAsset(path, true);
            
#if VRC_SDK_VRCSDK3
            var m = new MenuCreater("Pickup_"+target.name);
            m.AddToggle(target.name+"_Grab",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon),paramGrab);
            m.AddToggle(target.name+"_Drop",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.dropIcon),paramDrop);
            
            var mp = new MenuCreater("ParentMenu");
            mp.AddSubMenu(m.CreateAsset(path, true),target.name,AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon));
            
            var p = new ParametersCreater("Pickup_"+target.name);
            p.AddParam(paramGrab,false,false);
            p.AddParam(paramDrop,false,false);
            
            var am = new AvatarModifyTool(avatar,path);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.Create();
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = mp.CreateAsset(path, true);
            }
            if (writeDefault)
            {
                am.WriteDefaultOverride = true;
            }
            am.ModifyAvatar(assets,false);
#endif
        }

        void SimpleSetup(string path)
        {
            if (deleateObject)
            {
                var go = handBone.transform.FindInChildren(target.name + "(Clone)");
                if(go) DestroyImmediate(go);
            }
            var grabObj = GameObject.Instantiate(target,handBone.transform,false);

            var c = new AnimatorControllerCreator("Pickup_"+target.name);
            var idleAnim = new AnimationClipCreator("Idle",avatar.gameObject);
            idleAnim.AddKeyframe_Gameobject(target,0f,true);
            idleAnim.AddKeyframe_Gameobject(grabObj,0f,false);
            idleAnim.AddKeyframe_Gameobject(target,1f/60f,true);
            idleAnim.AddKeyframe_Gameobject(grabObj,1f/60f,false);
            var grabAnim = new AnimationClipCreator("Grab",avatar.gameObject);
            grabAnim.AddKeyframe_Gameobject(target,0f,false);
            grabAnim.AddKeyframe_Gameobject(grabObj,0f,true);
            grabAnim.AddKeyframe_Gameobject(target,1f/60f,false);
            grabAnim.AddKeyframe_Gameobject(grabObj,1f/60f,true);
            
            c.AddDefaultState("Idle",idleAnim.Create());
            c.AddState("Grab", grabAnim.Create());
            
            string paramGrab = target.name + "_Grab";
            c.AddParameter("LeftHand_Idle",0);
            c.AddParameter("GestureRight",0);
            
            if (grabTrigger == Triggers.Menu)
            {
                c.AddTransition("Idle","Grab",new AnimatorCondition[1]{ new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.Equals,
                    parameter = paramGrab,
                    threshold = 1,
                }},true,1f,0f);
                c.AddTransition("Grab","Idle",new AnimatorCondition[1]{ new AnimatorCondition()
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
                
                c.AddTransition("Grab","Idle",new AnimatorCondition[1]{new AnimatorCondition()
                {
                    mode = AnimatorConditionMode.NotEqual,
                    parameter = paramGrab,
                    threshold = 1,
                }},true,1f,0.2f);
                c.AddTransition("Grab","Idle",new AnimatorCondition[1]{ GetGrabTrigger(grabTrigger,false)},true,1f,0.2f);
                
                c.CreateAsset(path);
                idleAnim.CreateAsset(path, true);
                grabAnim.CreateAsset(path, true);
            
#if VRC_SDK_VRCSDK3
                var m = new MenuCreater("Pickup_"+target.name);
                m.AddToggle(target.name+"_Grab",AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.grabIcon),paramGrab);
            
                var p = new ParametersCreater("Pickup_"+target.name);
                p.AddParam(paramGrab,false,false);
            
                var am = new AvatarModifyTool(avatar,path);
                var assets = CreateInstance<AvatarModifyData>();
                {
                    assets.fx_controller = c.Create();
                    assets.parameter = p.CreateAsset(path, true);
                    assets.menu = m.CreateAsset(path, true);
                }
                am.ModifyAvatar(assets,false);
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