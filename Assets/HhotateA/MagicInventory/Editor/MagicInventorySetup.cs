using System;
using System.Collections.Generic;
using System.Linq;
using HhotateA.AvatarModifyTools.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;

namespace HhotateA.AvatarModifyTools.MagicInventory
{
    public class MagicInventorySetup : EditorWindow
    {
        [MenuItem("Window/HhotateA/MagicInventorySetup")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<MagicInventorySetup>();
            wnd.titleContent = new GUIContent("MagicInventory");
        }

        private Animator avatar;
        private List<MenuElement> menuElements = new List<MenuElement>();
        private List<ItemElement> itemElements = new List<ItemElement>();
        private SerializedProperty prop;
        ReorderableList menuReorderableList;

        private float duration = 1f;
        private string param = "_AnimationTime";
        
        Dictionary<Shader,Dictionary<Material, Material>> matlist = new Dictionary<Shader,Dictionary<Material, Material>>();
        Material GetAnimationMaterial(Material origin,Shader shader)
        {
            if (!matlist.ContainsKey(shader))
            {
                matlist.Add(shader,new Dictionary<Material,Material>());
            }
            if (!matlist[shader].ContainsKey(origin))
            {
                var mat = new Material(origin);
                mat.name = origin.name + "_" + shader.name;
                mat.shader = shader;
                matlist[shader].Add(origin,mat);
            }

            return matlist[shader][origin];
        }

        private void OnEnable()
        {
            menuReorderableList = new ReorderableList(menuElements, typeof(MenuElement))
            {
                elementHeight = 60,
                drawElementCallback = (r, i, a, f) =>
                {
                    var d = menuElements[i];
                    r.height /= 3;
                    d.name = EditorGUI.TextField(r,"", d.name);
                    r.y += r.height;
                    if (d.isToggle)
                    {
                        d.isToggle = EditorGUI.Toggle(r, "isToggle", d.isToggle);
                    }
                    else
                    {
                        var rh = r;
                        rh.width *= 0.5f;
                        d.isToggle = EditorGUI.Toggle(rh, "isToggle", d.isToggle);
                        rh.x += rh.width;
                        d.layer = (LayerGroup) EditorGUI.EnumPopup(rh, d.layer);
                    }
                    r.y += r.height;
                    d.isSaved = EditorGUI.Toggle(r, "isSaved", d.isSaved);
                    //d.animationParam = EditorGUI.TextField(r,"Param", d.animationParam);
                    //d.root = (GameObject) EditorGUI.ObjectField(r,"", d.root, typeof(GameObject), true);
                },
            };
        }
        private void OnGUI()
        {
            avatar = EditorGUILayout.ObjectField("", avatar, typeof(Animator), true) as Animator;
            menuReorderableList.DoLayoutList();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)
                    {
                        var index = menuReorderableList.index;
                        foreach (var item in menuElements[index].onItems)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    item.root = (GameObject) EditorGUILayout.ObjectField("", item.root,
                                        typeof(GameObject), true);
                                }

                                item.duration = EditorGUILayout.FloatField("", item.duration);
                                item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type);

                            }

                        }
                        var newItem = (GameObject) EditorGUILayout.ObjectField("", null,
                            typeof(GameObject), true);
                        if (newItem)
                        {
                            var item = itemElements.FirstOrDefault(l => l.root == newItem);
                            if (item == null)
                            {
                                item = new ItemElement(newItem);
                                itemElements.Add(item);
                            }
                            menuElements[index].onItems.Add(item);
                            menuElements[index].onItems = menuElements[index].onItems.Distinct().ToList();
                        }
                    }
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    if (0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)
                    {
                        var index = menuReorderableList.index;
                        foreach (var item in menuElements[index].offItems)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUI.DisabledScope(true))
                                {
                                    item.root = (GameObject) EditorGUILayout.ObjectField("", item.root,
                                        typeof(GameObject), true);
                                }

                                item.duration = EditorGUILayout.FloatField("", item.duration);
                                item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type);

                            }

                        }
                        var newItem = (GameObject) EditorGUILayout.ObjectField("", null,
                            typeof(GameObject), true);
                        if (newItem)
                        {
                            var item = itemElements.FirstOrDefault(l => l.root == newItem);
                            if (item == null)
                            {
                                item = new ItemElement(newItem);
                                itemElements.Add(item);
                            }
                            menuElements[index].offItems.Add(item);
                            menuElements[index].offItems = menuElements[index].offItems.Distinct().ToList();
                        }
                    }
                }
            }

            if (GUILayout.Button(""))
            {
                AnimatorControllerCreator acc = new AnimatorControllerCreator("TestAnimator");
                
            }
        }

        void SaveAnim(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension (path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
            var p = new ParametersCreater(fileName);
            var c = new AnimatorControllerCreator(fileName,false);
            var m = new MenuCreater(fileName);
            if (menuElements.Any(e => e.layer == LayerGroup.A))
            {
                c.CreateLayer(fileName + "_A");
                var layerMenus = menuElements.Where(e => e.layer == LayerGroup.A).ToList();
                c.AddDefaultState("Idle");
                int i = 1;
                foreach (var layerMenu in layerMenus)
                {
                    c.AnyStateToDefault(layerMenu.name, null, fileName + "_A", i);
                    layerMenu.layerValue = i;
                    i++;
                }
            }

            foreach (var menuElement in menuElements)
            {
                if (menuElement.isToggle)
                {
                    m.AddToggle(menuElement.name,menuElement.icon,fileName + "_Toggle",menuElement.layerValue);
                }
                else if(menuElement.layer == LayerGroup.A)
                {
                    m.AddToggle(menuElement.name,menuElement.icon,fileName + "_A",menuElement.layerValue);
                }
                
            }

        }

        void SaveElement(AnimatorControllerCreator controll,ItemElement element,string dir)
        {
            var path = AssetDatabase.GetAssetPath(controll.Create());
            string param = element.root.gameObject.name + element.root.gameObject.GetInstanceID().ToString();
            if (element.type == FeedType.None)
            {
                ItemLayer(controll,param,
                    OnAnimation(element.root).CreateAsset(path,true),
                    OffAnimation(element.root).CreateAsset(path,true),
                    null, null );
            }
            else
            if(element.type == FeedType.Scale)
            {
                ItemLayer(controll,param,
                    OnAnimation(element.root).CreateAsset(path,true),
                    OffAnimation(element.root).CreateAsset(path,true),
                    ScaleInAnimation(element.root,1f,true).CreateAsset(path,true),
                    ScaleOutAnimation(element.root,1f,true).CreateAsset(path,true) );
            }
            else
            if(element.type == FeedType.Shader)
            {
                ItemLayer(controll,param,
                    OnAnimation(element.root).CreateAsset(path,true),
                    OffAnimation(element.root).CreateAsset(path,true),
                    ShaderInAnimation(element.root,element.animationShader,1f,element.animationParam).CreateAsset(path,true),
                    ShaderOutAnimation(element.root,element.animationShader,1f,element.animationParam).CreateAsset(path,true) );
            }
        }

        AnimationClipCreator ScaleInAnimation(GameObject obj, float duration,bool bounce = false)
        {
            var anim = new AnimationClipCreator(obj.name+"_In_Scale",avatar.gameObject,true);
            anim.AddKeyframe_Gameobject(obj,0f,true);
            var defaultScale = obj.transform.localScale;
            anim.AddKeyframe_Scale(0f,obj.transform,Vector3.zero);
            if (bounce)
            {
                anim.AddKeyframe_Scale(duration*0.5f,obj.transform, defaultScale*0.1f);
                anim.AddKeyframe_Scale(duration*0.9f,obj.transform, defaultScale*1.3f);
            }
            anim.AddKeyframe_Scale(duration,obj.transform,defaultScale);
            return anim;
        }
        
        AnimationClipCreator ScaleOutAnimation(GameObject obj, float duration,bool bounce = false)
        {
            var anim = new AnimationClipCreator(obj.name+"_Out_Scale",avatar.gameObject,false);
            anim.AddKeyframe_Gameobject(obj,0f,true);
            var defaultScale = obj.transform.localScale;
            anim.AddKeyframe_Scale(0f,obj.transform,defaultScale);
            if (bounce)
            {
                anim.AddKeyframe_Scale(duration*0.1f,obj.transform, defaultScale*1.3f);
                anim.AddKeyframe_Scale(duration*0.5f,obj.transform, defaultScale*0.1f);
            }
            anim.AddKeyframe_Scale(duration,obj.transform,Vector3.zero);
            return anim;
        }

        AnimationClipCreator ShaderInAnimation(GameObject obj, Shader shader, float duration, string param,bool emittion = false, float from = 0f, float to = 1f)
        {
            var anim = new AnimationClipCreator(obj.name+"_In_"+shader.name,avatar.gameObject);
            ChangeMaterialShader(anim,obj,shader,0f);
            anim.AddKeyframe_Gameobject(obj,0f,true);
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                anim.AddKeyframe_MaterialParam(0f,rend,param,0f);
                anim.AddKeyframe_MaterialParam(duration,rend,param,1f);
            }
            
            ChangeMaterialDefault(anim,obj,duration+1/60f);
            if (emittion)
            {
                foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
                {
                    anim.AddKeyframe_MaterialParam(duration,rend,"_EmissionColor",Vector4.one);
                    anim.AddKeyframe_MaterialParam(duration*1.25f,rend,param,rend.sharedMaterial.GetColor("_EmissionColor"));
                }
            }

            return anim;
        }

        AnimationClipCreator ShaderOutAnimation(GameObject obj, Shader shader, float duration, string param, bool emittion = true, float from = 1f, float to = 0f)
        {
            var anim = new AnimationClipCreator(obj.name+"_Out_"+shader.name,avatar.gameObject);
            float time = 0f;
            anim.AddKeyframe_Gameobject(obj,time,true);
            
            if (emittion)
            {
                ChangeMaterialDefault(anim,obj,time);
                foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
                {
                    anim.AddKeyframe_MaterialParam(time,rend,param,rend.sharedMaterial.GetColor("_EmissionColor"));
                    time += duration * 0.25f;
                    anim.AddKeyframe_MaterialParam(time,rend,"_EmissionColor",Vector4.one);
                }
            }
            
            ChangeMaterialShader(anim,obj,shader,time);
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                anim.AddKeyframe_MaterialParam(time,rend,param,from);
                anim.AddKeyframe_MaterialParam(time+duration,rend,param,to);
            }

            return anim;
        }

        AnimationClipCreator OnAnimation(GameObject obj)
        {
            var anim = new AnimationClipCreator(obj.name+"_On",avatar.gameObject);
            ChangeMaterialDefault(anim,obj,0f);
            anim.AddKeyframe_Gameobject(obj,0f,true);
            anim.AddKeyframe_Transform(0f,obj.transform,false,false,true);
            return anim;
        }
        
        AnimationClipCreator OffAnimation(GameObject obj)
        {
            var anim = new AnimationClipCreator(obj.name+"_Off",avatar.gameObject);
            ChangeMaterialDefault(anim,obj,0f);
            anim.AddKeyframe_Gameobject(obj,0f,false);
            anim.AddKeyframe_Transform(0f,obj.transform,false,false,true);
            return anim;
        }

        void ChangeMaterialDefault(AnimationClipCreator anim,GameObject obj,float time = 0f)
        {
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    anim.AddKeyframe_Material(rend,rend.sharedMaterials[i],time,i);
                }
            }
        }

        void ChangeMaterialShader(AnimationClipCreator anim,GameObject obj, Shader shader,float time = 0f)
        {
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                for (int i = 0; i < rend.sharedMaterials.Length; i++)
                {
                    anim.AddKeyframe_Material(rend,
                        GetAnimationMaterial(rend.sharedMaterials[i],shader),time,i);
                }
            }
        }

        void SaveMaterials(string path)
        {
            foreach (var mats in matlist)
            {
                foreach (var mat in mats.Value)
                {
                    AssetDatabase.CreateAsset(mat.Value,path+mat.Value.name+".mat");
                }
            }
            matlist = new Dictionary<Shader, Dictionary<Material, Material>>();
        }

        void ItemLayer(AnimatorControllerCreator controller, 
            string param,
            AnimationClip onAnim, AnimationClip offAnim,
            AnimationClip inAnim, AnimationClip outAnim)
        {
            controller.CreateLayer(param);
            controller.AddParameter(param,AnimatorControllerParameterType.Bool);
            controller.AddDefaultState("Idle");
            controller.AddState("On", onAnim);
            controller.AddState("Off", offAnim);
            controller.AddTransition("Idle","On",param,true);
            controller.AddTransition("Idle","Off",param,false);
            controller.AddState("OnIdle");
            controller.AddState("OffIdle");
            controller.AddTransition("On","OnIdle");
            controller.AddTransition("Off","OffIdle");
            controller.AddState("In", inAnim);
            controller.AddState("Out", outAnim);
            controller.AddTransition("OffIdle","In",param,true);
            controller.AddTransition("In","On",true,1f,0f);
            controller.AddTransition("OnIdle","Out",param,false);
            controller.AddTransition("Out","Off",true,1f,0f);
        }

        private void OnDestroy()
        {
            SaveMaterials("Assets/aa");
        }
    }

    [Serializable]
    public class MenuElement
    {
        public string name;
        public Texture2D icon;
        public List<ItemElement> onItems = new List<ItemElement>();
        public List<ItemElement> offItems = new List<ItemElement>();
        public bool isToggle = true;
        public LayerGroup layer = LayerGroup.A;
        public int layerValue = 0;
        public bool isSaved = true;
    }

    [Serializable]
    public class ItemElement
    {
        public GameObject root;
        public FeedType type;
        public float delay = 0f;
        public float duration = 1f;
        public Shader animationShader;
        public string animationParam = "_AnimationTime";
        public string param;

        public ItemElement(GameObject obj)
        {
            root = obj;
            param = obj.name + "_" + obj.GetInstanceID();
        }

        public void SetRoot(GameObject obj)
        {
            root = obj;
            param = root.name + root.GetInstanceID().ToString();
        }
    }

    public enum FeedType
    {
        None,
        Scale,
        Shader,
        Feed,
        Draw,
    }

    public enum LayerGroup
    {
        A,
        B,
        C
    }
}