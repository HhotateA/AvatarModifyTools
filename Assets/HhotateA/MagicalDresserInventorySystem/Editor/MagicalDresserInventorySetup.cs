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
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using Object = System.Object;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace HhotateA.AvatarModifyTools.MagicalDresserInventorySystem
{
    public class MagicalDresserInventorySetup : WindowBase
    {
        [OnOpenAssetAttribute]
        public static bool OpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID).GetType() == typeof(MagicalDresserInventorySaveData))
            {
                OpenSavedWindow(EditorUtility.InstanceIDToObject(instanceID) as MagicalDresserInventorySaveData);
            }
            return false;
        }
        
        [MenuItem("Window/HhotateA/マジックドレッサーインベントリ(MDInventorySystem)",false,106)]
        public static void ShowWindow()
        {
            OpenSavedWindow();
        }
        
        private MagicalDresserInventorySaveData data;
        private List<MenuElement> menuElements => data.menuElements;
        private SerializedProperty prop;
        ReorderableList menuReorderableList;
        
        MenuTemplateTreeView menuTreeView;

        private bool displayItemMode = true;
        private bool displaySyncTransition = false;
        
        // <renderer original material,<transition sample material, use material>>
        Dictionary<Material,Dictionary<Material, Material>> matlist = new Dictionary<Material,Dictionary<Material, Material>>();
        Dictionary<GameObject,bool> defaultActive = new Dictionary<GameObject, bool>();
        Dictionary<Renderer,bool> defaultRendEnable = new Dictionary<Renderer, bool>();

        Dictionary<Renderer, Material[]> defaultMaterials = new Dictionary<Renderer, Material[]>();
        
        Dictionary<BlendShapeReference,float> defaultBlendShapes = new Dictionary<BlendShapeReference, float>();
        struct BlendShapeReference
        {
            public SkinnedMeshRenderer rend;
            public int index;
        }

        private bool openMenuList = true;
        
        Vector2 scrollLeft = Vector2.zero;
        Vector2 scrollLeftTree = Vector2.zero;
        Vector2 scrollRight = Vector2.zero;
        
        Material GetAnimationMaterial(Material origin,Material animMat)
        {
            if (!matlist.ContainsKey(animMat))
            {
                matlist.Add(animMat,new Dictionary<Material,Material>());
            }
            if (!matlist[animMat].ContainsKey(origin))
            {
                if (animMat.GetTypeByMaterial() != FeedType.None)
                {
                    var mat = animMat.GetTypeByMaterial().GetMaterialByType();
                    mat = Instantiate(mat);
                    if(origin.mainTexture) mat.mainTexture = origin.mainTexture;
                    matlist[animMat].Add(origin,mat);
                }
                else
                {
                    var mat = new Material(animMat);
                    mat.name = origin.name + "_" + animMat.name;
                    mat.mainTexture = origin.mainTexture;
                    matlist[animMat].Add(origin,mat);
                }
            }

            return matlist[animMat][origin];
        }

        public static void OpenSavedWindow(MagicalDresserInventorySaveData saveddata = null)
        {
            var wnd = GetWindow<MagicalDresserInventorySetup>();
            wnd.titleContent = new GUIContent("マジックドレッサーインベントリ(MDInventorySystem)");
            wnd.minSize = new Vector2(850, 500);
            wnd.maxSize = new Vector2(850,2000);

            if (saveddata == null)
            {
                saveddata = CreateInstance<MagicalDresserInventorySaveData>();
                saveddata.icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.itemboxIcon);
            }
            // wnd.data = Instantiate(saveddata);
            wnd.data = saveddata;
            wnd.LoadReorderableList();
            
            var root = wnd.data.GetRoot();
            if (root)
            {
#if VRC_SDK_VRCSDK3
                wnd.avatar = root.GetComponent<VRCAvatarDescriptor>();
#endif
                if (wnd.avatar)
                {
                    wnd.data.ApplyRoot(wnd.avatar.gameObject);
                }
            }
            wnd.LoadReorderableList();
        }

        void LoadReorderableList()
        {
            menuReorderableList = new ReorderableList(menuElements, typeof(MenuElement), true, true, false,false)
            {
                drawHeaderCallback = r =>
                {
                    EditorGUI.LabelField(r,"Menu Elements");
                },
                elementHeight = 60+6,
                drawElementCallback = (r, i, a, f) => DrawMenuElement(r,i),
                onAddCallback = l => AddMenu(),
                onSelectCallback = l =>
                {
                    SetObjectActiveForScene(menuElements[l.index]);
                    scrollRight = Vector2.zero;
                }
            };
            menuTreeView = new MenuTemplateTreeView(new TreeViewState ());
            menuTreeView.Setup(data);
            menuTreeView.OnSelect += (m) =>
            {
                menuReorderableList.index = data.menuElements.FindIndex(e=>e==m);
                SetObjectActiveForScene(m);
                scrollRight = Vector2.zero;
            };
        }

        void DrawMenuElement(Rect r, int i)
        {
            if (0 > i || menuElements.Count <= i)
            {
                EditorGUI.LabelField(r,"Null" + i);
                return;
            }
            var d = menuElements[i];
            if(d == null) return;
            {
                var rh = r;
                rh.width = rh.height;
                d.icon = (Texture2D) EditorGUI.ObjectField (rh,d.icon, typeof(Texture2D), false);
                r.width -= rh.width;
                r.x += rh.width+10;
            }
            r.height -= 6;
            r.height /= 3;
            r.width *= 0.95f;
            
            d.name = EditorGUI.TextField(r,"", d.name);

            r.y += 2;
            r.y += r.height;
            
            if (d.isToggle)
            {
                var rh = r;
                rh.width = 20;
                d.SetLayer( EditorGUI.Toggle(rh, "", d.isToggle));
                rh.x += rh.width;
                rh.width = 120;
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.EnumPopup(rh, (ToggleGroup)0);
                }
                rh.x += rh.width+10;
                rh.width = 20;
                d.isRandom = EditorGUI.Toggle(rh, "", d.isRandom);
                rh.x += rh.width;
                rh.width = 100;
                EditorGUI.LabelField(rh, "Is Random");
            }
            else
            {
                var rh = r;
                rh.width = 20;
                d.SetLayer( EditorGUI.Toggle(rh, "", d.isToggle));
                rh.x += rh.width;
                rh.width = 120;
                d.SetLayer( (LayerGroup) EditorGUI.EnumPopup(rh, d.layer));

                rh.x += rh.width+10;
                rh.width = 20;
                data.layerSettingses[(int) d.layer].isRandom = EditorGUI.Toggle(rh, "", data.layerSettingses[(int) d.layer].isRandom);
                rh.x += rh.width;
                rh.width = 60;
                using (new EditorGUI.DisabledScope(d.isTaboo))
                {
                    EditorGUI.LabelField(rh, d.isTaboo ? "is Taboo" : "Is Random");
                }

                //if (data.layerSettingses[(int) d.layer].isRandom)
                {
                    rh.x += rh.width+5;
                    rh.width = 20;
                    d.isTaboo = EditorGUI.Toggle(rh, "", d.isTaboo);
                }
            }

            r.y += 2;
            r.y += r.height;
            
            if (d.isToggle)
            {
                var rh = r;
                rh.width = 20;
                d.isSaved = EditorGUI.Toggle(rh, "", d.isSaved);
                rh.x += rh.width;
                rh.width = 100;
                EditorGUI.LabelField(rh, "Is Saved");
                rh.x += rh.width + 30;
                rh.width = 20;
                d.isDefault = EditorGUI.Toggle(rh, "", d.isDefault);
                rh.x += rh.width;
                rh.width = 100;
                EditorGUI.LabelField(rh, "Is Default");
            }
            else
            {
                var rh = r;
                rh.width = 20;
                data.layerSettingses[(int)d.layer].isSaved = 
                    EditorGUI.Toggle(rh, "", data.layerSettingses[(int)d.layer].isSaved);
                rh.x += rh.width;
                rh.width = 100;
                EditorGUI.LabelField(rh, "Is Saved");
                rh.x += rh.width + 30;
                rh.width = 20;
                if (data.layerSettingses[(int) d.layer].GetDefaultElement(menuElements) == d)
                {
                    rh.width = 20;
                    if (!EditorGUI.Toggle(rh, "",true))
                    {
                        data.layerSettingses[(int) d.layer].SetDefaultElement(null);
                    }
                    rh.x += rh.width;
                    rh.width = 100;
                    EditorGUI.LabelField(rh, "Is Default");
                }
                else
                {
                    rh.width = 20;
                    if (EditorGUI.Toggle(rh, "",false))
                    {
                        data.layerSettingses[(int) d.layer].SetDefaultElement(d);
                    }
                    rh.x += rh.width;
                    rh.width = 100;
                    EditorGUI.LabelField(rh, "Is Default");
                }
            }
        }

        void DrawFolderElement(Rect r, MenuTemplate d)
        {
            var rh = r;
            rh.width = rh.height;
            d.icon = (Texture2D) EditorGUI.ObjectField (rh,d.icon, typeof(Texture2D), false);
            r.width -= rh.width;
            r.x += rh.width+10;
            r.height -= 6;
            r.height /= 3;
            r.width *= 0.95f;
            
            d.name = EditorGUI.TextField(r,"", d.name);
        }
        
        private void OnGUI()
        {
            TitleStyle("マジックドレッサーインベントリ");
            DetailStyle("アバターのメニューから，服やアイテムの入れ替えを行える設定ツールです．",EnvironmentGUIDs.readme);
#if VRC_SDK_VRCSDK3

            EditorGUILayout.Space();
            AvatartField("",()=>
            {
                RevertObjectActiveForScene();
                data.ApplyRoot(avatar.gameObject);
                data.SaveGUID(avatar.gameObject);
                LoadReorderableList();
            });
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (!avatar)
            {
                GUILayout.Label("シーン上のアバターをドラッグ＆ドロップ");
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                        GUILayout.Width(60), GUILayout.Height(60));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        data.saveName = EditorGUILayout.TextField(data.saveName, GUILayout.Height(20));
                    }
                }

                return;
            }
            
            var index = menuReorderableList.index;
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    data.icon = (Texture2D) EditorGUILayout.ObjectField(data.icon, typeof(Texture2D), false,
                        GUILayout.Width(60), GUILayout.Height(60));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        data.saveName = EditorGUILayout.TextField(data.saveName, GUILayout.Height(20));
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUILayout.VerticalScope())
                            {
                                data.materialOverride = EditorGUILayout.ToggleLeft("Override Default Value Animation", data.materialOverride);
                                data.idleOverride = EditorGUILayout.ToggleLeft("Override Animation On Idle State", data.idleOverride);
                            }
                            using (new EditorGUILayout.VerticalScope())
                            {
                                data.createAnimWhenNotChangedActive = EditorGUILayout.ToggleLeft("Create Anim when Not Changed Active", data.createAnimWhenNotChangedActive);
                                data.useMenuTemplate = EditorGUILayout.ToggleLeft("Use Menu Template", data.useMenuTemplate);
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(350)))
                    {
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            if (data.useMenuTemplate)
                            {
                                using (new EditorGUILayout.VerticalScope(GUI.skin.box,GUILayout.Width(330)))
                                {
                                    var menuelementRect = EditorGUILayout.GetControlRect(false, 60);

                                    if (menuReorderableList.index >= 0)
                                    {
                                        DrawMenuElement(menuelementRect, menuReorderableList.index);
                                    }
                                    else
                                    {
                                        var selectFolders = menuTreeView.GetSelectTemplates();
                                        if (selectFolders.Count > 0)
                                        {
                                            DrawFolderElement(menuelementRect, selectFolders[0]);
                                        }
                                    }
                                }
                                
                                scrollLeftTree = EditorGUILayout.BeginScrollView(scrollLeftTree, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                                var treeRect = EditorGUILayout.GetControlRect(false, openMenuList ? (position.height-300)/2 : position.height-300);
                                treeRect.width = 375;
                                menuTreeView.OnGUI(treeRect);
                                EditorGUILayout.EndScrollView();
                                
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    if(GUILayout.Button("Reset",GUILayout.Width(75)))
                                    {
                                        ResetTemplate();
                                    }
                                    EditorGUILayout.LabelField("",GUILayout.Width(100));
                                    if(GUILayout.Button("New Folder",GUILayout.Width(75)))
                                    {
                                        AddFolder();
                                    }
                                    EditorGUILayout.LabelField("  ",GUILayout.Width(10));

                                    using (new EditorGUI.DisabledScope(menuTreeView.GetSelectTemplates().Count == 0 && 
                                                                       !(0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)))
                                    {
                                        if(GUILayout.Button("-",GUILayout.Width(30)))
                                        {
                                            if (0 <= menuReorderableList.index &&
                                                  menuReorderableList.index < menuElements.Count)
                                            {
                                                menuElements.RemoveAt(menuReorderableList.index);
                                            }
                                            else
                                            {
                                                var selectFolders = menuTreeView.GetSelectTemplates();
                                                // foreach (var selectFolder in selectFolders)
                                                {
                                                    var selectFolder = selectFolders[0];
                                                    if (String.IsNullOrWhiteSpace(selectFolder.menuGUID))
                                                    {
                                                        MenuTemplate.FIndTemplateElement(data.menuTemplate, selectFolder.GetGuid(),
                                                            (e, p) =>
                                                            {
                                                                if (p == null)
                                                                {
                                                                    if (data.menuTemplate.Contains(e))
                                                                    {
                                                                        data.menuTemplate.Remove(e);
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    p.childs.Remove(e);
                                                                }
                                                            });
                                                    }
                                                    else
                                                    {
                                                        var menu = menuElements.FirstOrDefault(e =>
                                                            e.guid == selectFolder.menuGUID);
                                                        if (menu != null)
                                                        {
                                                            menuElements.Remove(menu);
                                                        }
                                                    }
                                                }
                                            }
                                            menuTreeView.ReloadTemplates();
                                        }
                                    }
                                    EditorGUILayout.LabelField("  ",GUILayout.Width(2));
                                    if(GUILayout.Button("+",GUILayout.Width(30)))
                                    {
                                        AddMenu();
                                    }
                                }
                                
                                openMenuList = EditorGUILayout.ToggleLeft("Extend Menu List",openMenuList);
                                
                                if (openMenuList)
                                {
                                    scrollLeft = EditorGUILayout.BeginScrollView(scrollLeft, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView,GUILayout.Width(375),GUILayout.ExpandHeight(false));
                                    menuReorderableList.DoLayoutList();
                                    EditorGUILayout.EndScrollView();
                                }
                                else
                                {
                                }
                                EditorGUILayout.Space();
                                EditorGUILayout.LabelField(" ",GUILayout.ExpandHeight(true));
                            }
                            else
                            {
                                scrollLeft = EditorGUILayout.BeginScrollView(scrollLeft, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView,GUILayout.Width(375),GUILayout.ExpandHeight(false));
                                menuReorderableList.DoLayoutList();
                                EditorGUILayout.EndScrollView();
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField("  ",GUILayout.Width(275));
                                    using (new EditorGUI.DisabledScope(!(0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)))
                                    {
                                        if(GUILayout.Button("-",GUILayout.Width(30)))
                                        {
                                            if(0 <= menuReorderableList.index && menuReorderableList.index < menuElements.Count)
                                            {
                                                menuElements.RemoveAt(menuReorderableList.index);
                                            }
                                        }
                                    }
                                    EditorGUILayout.LabelField("  ",GUILayout.Width(5));
                                    if(GUILayout.Button("+",GUILayout.Width(30)))
                                    {
                                        AddMenu();
                                    }
                                }
                                EditorGUILayout.Space();
                            }
                            if (check.changed)
                            {
                                if (0 <= index && index < menuElements.Count)
                                {
                                    SetObjectActiveForScene(menuElements[menuReorderableList.index]);
                                }

                                menuTreeView.ReloadTemplates();
                            }
                        }
                    }
                    
                    EditorGUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(400)))
                    {
                        var itemListStyle = new GUIStyle();
                        var itemListStyleNormal = itemListStyle.normal;
                        itemListStyleNormal.background = Texture2D.grayTexture;
                        itemListStyle.normal = itemListStyleNormal;
                        using (new EditorGUILayout.VerticalScope(itemListStyle))
                        {
                            if (0 <= index && index < menuElements.Count)
                            {
                                EditorGUILayout.LabelField(menuElements[index].name,GUILayout.Width(210));

                                using (var r = new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField("");
                                    var rect = r.rect;
                                    rect.height += 5;
                                    var tabstyleActive = new GUIStyle(GUI.skin.button);
                                    tabstyleActive.stretchWidth = true;
                                    var ns = tabstyleActive.normal;
                                    ns.background = Texture2D.blackTexture;
                                    ns.textColor = Color.gray;
                                    tabstyleActive.normal = ns;
                                    var tabstyleDisable = new GUIStyle(GUI.skin.box);
                                    tabstyleDisable.stretchWidth = true;
                                    rect.width /= 2;
                                    if (GUI.Button(rect, "ON",
                                        displayItemMode ? tabstyleDisable : tabstyleActive))
                                    {
                                        displayItemMode = true;
                                        SetObjectActiveForScene(menuElements[index]);
                                    }

                                    rect.x += rect.width;
                                    if (GUI.Button(rect, "OFF",
                                        displayItemMode ? tabstyleActive : tabstyleDisable))
                                    {
                                        displayItemMode = false;
                                        SetObjectActiveForScene(menuElements[index]);
                                    }
                                }

                                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                                {
                                    ItemLabelDisplay();
                                    
                                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                                
                                    ItemElementDisplay(menuElements[index]);
                                    
                                    scrollRight = EditorGUILayout.BeginScrollView(scrollRight, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUI.skin.scrollView);
                                    if (displayItemMode)
                                    {
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            foreach (var item in menuElements[index].UnSafeActiveItems())
                                            {
                                                ItemElementDisplay(item, false, true, false, false, false, menuElements[index],
                                                    (o) =>
                                                    {
                                                        data.RepairReference(item.path, o, avatar.gameObject);
                                                    }, () =>
                                                    {
                                                        menuElements[index].activeItems.Remove(item);
                                                    });
                                            }
                                            
                                            foreach (var item in menuElements[index].SafeActiveItems())
                                            {
                                                ItemElementDisplay(item, true, true, true, true, true, menuElements[index],
                                                    (o) =>
                                                    {
                                                        // item.obj = o;
                                                    }, () =>
                                                    {
                                                        menuElements[index].activeItems.Remove(item);
                                                    });
                                            }

                                            if (!menuElements[index].isToggle)
                                            {
                                                foreach (var item in ComputeLayerAnotherItems(menuElements[index]))
                                                {
                                                    ItemElementDisplay(item, true, false, true, true, true, menuElements[index],
                                                        (o) =>
                                                        {
                                                            // item.obj = o;
                                                        }, () =>
                                                        {
                                                            menuElements[index].activeItems.Remove(item);
                                                        }, () =>
                                                        {
                                                            menuElements[index].activeItems.Add(item);
                                                        });
                                                }
                                            }

                                            if (check.changed)
                                            {
                                                SetObjectActiveForScene(menuElements[index]);
                                                // SyncItemActive(menuElements[index].activeItems, menuElements[index].inactiveItems, true);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            foreach (var item in menuElements[index].UnSafeInactiveItems())
                                            {
                                                ItemElementDisplay(item, false, true, false, false, false, menuElements[index],
                                                    (o) =>
                                                    {
                                                        data.RepairReference(item.path, o, avatar.gameObject);
                                                    }, () =>
                                                    {
                                                        menuElements[index].inactiveItems.Remove(item);
                                                    });
                                            }
                                            
                                            foreach (var item in menuElements[index].SafeInactiveItems())
                                            {
                                                ItemElementDisplay(item, true, true, true, true, true, menuElements[index],
                                                    (o) =>
                                                    {
                                                        // item.obj = o;
                                                    }, () =>
                                                    {
                                                        menuElements[index].inactiveItems.Remove(item);
                                                    });
                                            }

                                            foreach (var item in ComputeLayerInactiveItems(menuElements[index]))
                                            {
                                                using (var add = new EditorGUI.ChangeCheckScope())
                                                {
                                                    ItemElementDisplay(item, false, false, true, true, false, menuElements[index],
                                                        (o) =>
                                                        {
                                                            // item.obj = o;
                                                        }, () =>
                                                        {
                                                            menuElements[index].inactiveItems.Remove(item);
                                                        }, () =>
                                                        {
                                                            menuElements[index].inactiveItems.Add(item);
                                                        });
                                                }
                                            }
                                            
                                            if (check.changed)
                                            {
                                                SetObjectActiveForScene(menuElements[index]);
                                                // SyncItemActive(menuElements[index].inactiveItems, menuElements[index].activeItems, true);
                                            }
                                        }
                                    }
                                    EditorGUILayout.EndScrollView();

                                    var newItem = (GameObject) EditorGUILayout.ObjectField("", null,
                                        typeof(GameObject), true, GUILayout.Width(370));
                                    if (newItem)
                                    {
                                        if (menuElements[index].activeItems.All(e => e.obj != newItem))
                                        {
                                            menuElements[index].activeItems.Add(new ItemElement(newItem, avatar.gameObject,
                                                IsMenuElementDefault(menuElements[index])
                                                    ? newItem.gameObject.activeSelf
                                                    : !newItem.gameObject.activeSelf));
                                        }

                                        SetObjectActiveForScene(menuElements[index]);
                                    }
                                    
                                    //sync element
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        displaySyncTransition = EditorGUILayout.Foldout(displaySyncTransition, "Sync Elements");
                                        EditorGUILayout.LabelField(" ",GUILayout.Width(40),GUILayout.ExpandWidth(true));
                                        EditorGUILayout.LabelField("Delay",GUILayout.Width(50));
                                        EditorGUILayout.LabelField("",GUILayout.Width(10));
                                        EditorGUILayout.LabelField("On",GUILayout.Width(25));
                                        EditorGUILayout.LabelField("Off",GUILayout.Width(25));
                                    }
                                    using (new EditorGUILayout.VerticalScope())
                                    {
                                        if (displaySyncTransition)
                                        {
                                            foreach (var menuElement in menuElements)
                                            {
                                                if (menuElements[index] != menuElement)
                                                {
                                                    using (new EditorGUILayout.HorizontalScope())
                                                    {
                                                        EditorGUILayout.LabelField(" ",GUILayout.Width(50),GUILayout.ExpandWidth(true));
                                                        EditorGUILayout.LabelField(menuElement.name,GUILayout.Width(100));

                                                        var syncElement = displayItemMode ? 
                                                            menuElements[index].activeSyncElements.FirstOrDefault(e => e.guid == menuElement.guid):
                                                            menuElements[index].inactiveSyncElements.FirstOrDefault(e => e.guid == menuElement.guid);
                                                        //var syncOffElements = displayItemMode ? menuElements[index].activeSyncOffElements : menuElements[index].inactiveSyncOffElements;
                                                        if (syncElement == null)
                                                        {
                                                            using (var check = new EditorGUI.ChangeCheckScope())
                                                            {
                                                                syncElement = new SyncElement(menuElement.guid);
                                                                if (EditorGUILayout.Toggle("", syncElement.delay>=0, GUILayout.Width(25)))
                                                                {
                                                                    syncElement.delay = 0f;
                                                                }
                                                                else
                                                                {
                                                                    syncElement.delay = -1f;
                                                                }

                                                                using (new EditorGUI.DisabledScope(true))
                                                                {
                                                                    syncElement.delay = EditorGUILayout.FloatField("", syncElement.delay, GUILayout.Width(50));
                                                                }

                                                                EditorGUILayout.LabelField("",GUILayout.Width(15));
                                                                syncElement.syncOn = EditorGUILayout.Toggle("", syncElement.syncOn, GUILayout.Width(25));
                                                                syncElement.syncOff = EditorGUILayout.Toggle("", syncElement.syncOff, GUILayout.Width(25));
                                                                if (check.changed)
                                                                {
                                                                    if (displayItemMode)
                                                                    {
                                                                        menuElements[index].activeSyncElements.Add(syncElement);
                                                                    }
                                                                    else
                                                                    {
                                                                        menuElements[index].inactiveSyncElements.Add(syncElement);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (EditorGUILayout.Toggle("", syncElement.delay>=0, GUILayout.Width(25)))
                                                            {
                                                                syncElement.delay = 0f;
                                                            }
                                                            else
                                                            {
                                                                syncElement.delay = -1f;
                                                            }

                                                            using (new EditorGUI.DisabledScope(true))
                                                            {
                                                                syncElement.delay = EditorGUILayout.FloatField("", syncElement.delay, GUILayout.Width(50));
                                                            }
                                                            
                                                            EditorGUILayout.LabelField("",GUILayout.Width(15));
                                                            if (EditorGUILayout.Toggle("", syncElement.syncOn, GUILayout.Width(25)))
                                                            {
                                                                if (!syncElement.syncOn)
                                                                {
                                                                    // 重複防止
                                                                    syncElement.syncOn = true;
                                                                    syncElement.syncOff = false;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                syncElement.syncOn = false;
                                                            }
                                                            if(EditorGUILayout.Toggle("", syncElement.syncOff, GUILayout.Width(25)))
                                                            {
                                                                if (!syncElement.syncOff)
                                                                {
                                                                    // 重複防止
                                                                    syncElement.syncOff = true;
                                                                    syncElement.syncOn = false;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                syncElement.syncOff = false;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                EditorGUILayout.LabelField("null");
                            }
                        }

                        EditorGUILayout.Space();
                        if (ShowOptions())
                        {
                            if (GUILayout.Button("Force Revert"))
                            {
                                RevertObjectActiveForScene();
                                var mod = new AvatarModifyTool(avatar);
                                mod.RevertByKeyword(EnvironmentGUIDs.prefix);
                                OnFinishRevert();
                            }
                        }

                        EditorGUILayout.Space();
                        EditorGUILayout.Space();

                        using (new EditorGUI.DisabledScope(avatar == null))
                        {
                            if (GUILayout.Button("Setup"))
                            {
                                RevertObjectActiveForScene();
                                var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.GetAssetName(), "mdinventry.asset");
                                if (string.IsNullOrEmpty(path))
                                {
                                    OnCancel();
                                    return;
                                }
                                if (String.IsNullOrWhiteSpace(data.saveName))
                                {
                                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                                    data.saveName = fileName;
                                }
                                try
                                {
                                    data = Instantiate(data);
                                    LoadReorderableList();
                                    // data.ApplyPath(avatar.gameObject);
                                    path = FileUtil.GetProjectRelativePath(path);
                                    AssetDatabase.CreateAsset(data, path);
                                    Setup(path);
                                    OnFinishSetup();
                                    
                                    var conflict = FindConflict();
                                    if (conflict.Count > 0)
                                    {
                                        //status.Warning("Detect Conflict Layer : " + conflict[0]);
                                        status.Warning("Complete Setup (Detect Conflict Layers)");
                                        foreach (var c in conflict)
                                        {
                                            Debug.LogWarning("Detect Conflict Layer : " + c);
                                        }
                                    }

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
                        if (GUILayout.Button("Export Animation"))
                        {
                            RevertObjectActiveForScene();
                            var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.GetAssetName(), "anim");
                            if (string.IsNullOrEmpty(path))
                            {
                                OnCancel();
                                return;
                            }
                            data.saveName = Path.GetFileNameWithoutExtension(path);
                            try
                            {
                                // data = Instantiate(data);
                                // LoadReorderableList();
                                // data.ApplyPath(avatar.gameObject);
                                // AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                                SaveAnim(path);
                                status.Success("Finish Export");
                            }
                            catch (Exception e)
                            {
                                OnError(e);
                                throw;
                            }
                        }
                        EditorGUILayout.Space();

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("Save Settings"))
                            {
                                var path = EditorUtility.SaveFilePanel("Save", data.GetAssetDir(), data.GetAssetName(),"mdinventry.asset");
                                if (string.IsNullOrEmpty(path))
                                {
                                    OnCancel();
                                    return;
                                }
                                data = Instantiate(data);
                                LoadReorderableList();
                                AssetDatabase.CreateAsset(data, FileUtil.GetProjectRelativePath(path));
                                OnSave();
                            }
                            if (GUILayout.Button("Load Settings"))
                            {
                                var path = EditorUtility.OpenFilePanel("Load", data.GetAssetDir(), "mdinventry.asset");
                                if (string.IsNullOrEmpty(path))
                                {
                                    OnCancel();
                                    return;
                                }
                                var d = AssetDatabase.LoadAssetAtPath<MagicalDresserInventorySaveData>(FileUtil.GetProjectRelativePath(path));
                                if (d == null)
                                {
                                    status.Warning("Load Failure");
                                    return;
                                }
                                else
                                {
                                    data = d;
                                    LoadReorderableList();
                                }

                                // avatarの設定
                                if (avatar)
                                {
                                    data.ApplyRoot(avatar.gameObject);
                                }
                                else
                                {
                                    var root = d.GetRoot();
                                    if (root)
                                    {
#if VRC_SDK_VRCSDK3
                                        avatar = root.GetComponent<VRCAvatarDescriptor>();
#endif
                                        if (avatar)
                                        {
                                            data.ApplyRoot(avatar.gameObject);
                                        }
                                    }
                                }
                                OnLoad();
                            }
                        }

                        status.Display();
                        Signature();
                    }
                }
            }
#else
            VRCErrorLabel();
#endif
        }

        bool GetLayerDefaultActive(LayerGroup layer,GameObject obj)
        {
            var item = data.layerSettingses[(int) layer].GetDefaultElement(menuElements).SafeActiveItems()
                .FirstOrDefault(e => e.obj == obj);
            if (item != null)
            {
                return item.active;
            }
            return true;
        }

        void ItemLabelDisplay()
        {
            GUIStyle label = new GUIStyle(GUI.skin.label);
            label.fontSize = 10;
            label.wordWrap = true;
            label.alignment = TextAnchor.LowerLeft;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("IsActive", label,GUILayout.Width(55));
                EditorGUILayout.LabelField("Object",  label,GUILayout.Width(150));
                EditorGUILayout.LabelField("Delay",  label,GUILayout.Width(50));
                EditorGUILayout.LabelField("Duration",  label,GUILayout.Width(50));
                EditorGUILayout.LabelField("Type",  label,GUILayout.Width(100));
            }
        }

        void ItemElementDisplay(MenuElement parentmenu)
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    parentmenu.extendOverrides = EditorGUILayout.Foldout(parentmenu.extendOverrides, "Override Transitions");
                }

                if (parentmenu.extendOverrides)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var item = parentmenu.overrideActivateTransition;
                        EditorGUILayout.LabelField(" ", GUILayout.Width(30));

                        parentmenu.isOverrideActivateTransition = EditorGUILayout.Toggle("", parentmenu.isOverrideActivateTransition, GUILayout.Width(30));

                        EditorGUILayout.LabelField("Activate Transition", GUILayout.Width(150));

                        item.delay = EditorGUILayout.FloatField("", item.delay, GUILayout.Width(50));
                        item.duration = EditorGUILayout.FloatField("", item.duration, GUILayout.Width(50));
                        item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type, GUILayout.Width(100));
                    }
                    if (parentmenu.overrideActivateTransition.type == FeedType.Shader)
                    {
                        ShaderOptionDisplay(parentmenu.overrideActivateTransition);
                    }
                    
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var item = parentmenu.overrideInactivateTransition;
                        EditorGUILayout.LabelField("", GUILayout.Width(30));

                        parentmenu.isOverrideInactivateTransition = EditorGUILayout.Toggle("", parentmenu.isOverrideInactivateTransition, GUILayout.Width(30));

                        EditorGUILayout.LabelField("Inactivate Transition", GUILayout.Width(150));

                        item.delay = EditorGUILayout.FloatField("", item.delay, GUILayout.Width(50));
                        item.duration = EditorGUILayout.FloatField("", item.duration, GUILayout.Width(50));
                        item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type, GUILayout.Width(100));
                    }
                    if (parentmenu.overrideInactivateTransition.type == FeedType.Shader)
                    {
                        ShaderOptionDisplay(parentmenu.overrideInactivateTransition);
                    }
                }
            }
        }

        void ItemElementDisplay(ItemElement item,bool activeEdit = true,bool objEdit = true,bool timeEdit = true,bool typeEdit = true, bool optionEdit = true,
            MenuElement parentmenu = null,Action<GameObject> onSetObject = null,Action onRemoveObject = null,Action onChange = null)
        {
            bool overrideMode = false;
            if (parentmenu != null)
            {
                if (item.active && parentmenu.isOverrideActivateTransition)
                {
                    overrideMode = true;
                }
                if (!item.active && parentmenu.isOverrideInactivateTransition)
                {
                    overrideMode = true;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                //EditorGUILayout.LabelField("",GUILayout.Width(5));
                var r = GUILayoutUtility.GetRect(new GUIContent(""), GUI.skin.toggle,GUILayout.Width(25));
                
                using (new EditorGUI.DisabledScope(!optionEdit))
                {
                    item.extendOption = EditorGUI.Foldout(r, item.extendOption, "");
                }

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    using (new EditorGUI.DisabledScope(!activeEdit))
                    {
                        item.active = EditorGUILayout.Toggle("", item.active, GUILayout.Width(30));
                    }

                    using (new EditorGUI.DisabledScope(!objEdit))
                    {
                        var o = (GameObject) EditorGUILayout.ObjectField("", item.obj,
                            typeof(GameObject), true, GUILayout.Width(110));
                        if (item.obj == null)
                        {
                            if (o != null)
                            {
                                // item.obj = o;
                                onSetObject?.Invoke(o);
                            }
                            if (GUILayout.Button("×", GUILayout.Width(40)))
                            {
                                // item.obj = null;
                                onRemoveObject?.Invoke();
                            }
                            EditorGUILayout.LabelField(" ",GUILayout.Width(10));
                            EditorGUILayout.LabelField("Missing :",GUILayout.Width(50));
                            EditorGUILayout.TextField(item.path,GUILayout.Width(140));
                            return;
                        }
                        else
                        {
                            if (o == null)
                            {
                                // 消されたときのみ上書き
                                // item.obj = null;
                                onRemoveObject?.Invoke();
                            }
                            if (GUILayout.Button(objEdit ? "×" : "Sync", GUILayout.Width(40)))
                            {
                                // item.obj = null;
                                onRemoveObject?.Invoke();
                            }
                        }
                    }

                    if (overrideMode)
                    {
                        if (item.active)
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.FloatField("", parentmenu.overrideActivateTransition.delay, GUILayout.Width(50));
                                EditorGUILayout.FloatField("", parentmenu.overrideActivateTransition.duration, GUILayout.Width(50));
                                
                                EditorGUILayout.EnumPopup("", parentmenu.overrideActivateTransition.type, GUILayout.Width(100));
                            }
                        }
                        else
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.FloatField("", parentmenu.overrideInactivateTransition.delay, GUILayout.Width(50));
                                EditorGUILayout.FloatField("", parentmenu.overrideInactivateTransition.duration, GUILayout.Width(50));
                                
                                EditorGUILayout.EnumPopup("", parentmenu.overrideInactivateTransition.type, GUILayout.Width(100));
                            }
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(!timeEdit))
                        {
                            item.delay = EditorGUILayout.FloatField("", item.delay, GUILayout.Width(50));
                            item.duration =
                                EditorGUILayout.FloatField("", item.duration, GUILayout.Width(50));
                        }

                        using (new EditorGUI.DisabledScope(!typeEdit))
                        {
                            item.type = (FeedType) EditorGUILayout.EnumPopup("", item.type, GUILayout.Width(100));
                        }
                    }
                    
                    
                    if (change.changed)
                    {
                        onChange?.Invoke();
                    }
                }
            }


            if (overrideMode)
            {
                if (item.active)
                {
                    if (item.extendOption && optionEdit)
                    {
                        foreach (var rendOption in item.rendOptions)
                        {
                            RendererOptionDisplay(item, rendOption);
                        }
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    }
                }
                else
                {
                    if (item.extendOption && optionEdit)
                    {
                        foreach (var rendOption in item.rendOptions)
                        {
                            RendererOptionDisplay(item, rendOption);
                        }
                        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                    }
                }
            }
            else
            {
                if (item.type == FeedType.Shader)
                {
                    ShaderOptionDisplay(item);
                }

                if (item.extendOption && optionEdit)
                {
                    foreach (var rendOption in item.rendOptions)
                    {
                        RendererOptionDisplay(item, rendOption);
                    }
                    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
                }    
            }
        }

        void ShaderOptionDisplay(ItemElement item)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", GUILayout.Width(100));
                item.animationMaterial = (Material) EditorGUILayout.ObjectField("", item.animationMaterial,
                    typeof(Material), false, GUILayout.Width(100));
                EditorGUILayout.LabelField("", GUILayout.Width(5));
                item.animationParam = EditorGUILayout.TextField( item.animationParam, GUILayout.Width(100));
                EditorGUILayout.LabelField("", GUILayout.Width(5));
                item.animationParamOff = EditorGUILayout.FloatField(item.animationParamOff,GUILayout.Width(30));
                EditorGUILayout.LabelField("=>", GUILayout.Width(20));
                item.animationParamOn = EditorGUILayout.FloatField(item.animationParamOn,GUILayout.Width(30));
            }
        }

        void RendererOptionDisplay(ItemElement item, RendererOption rendOption)
        {
            if(rendOption == null) return;
            if(rendOption.rend == null) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("", GUILayout.Width(25));
                if (item.active)
                {
                    rendOption.RendEnable = EditorGUILayout.Toggle("", rendOption.RendEnable, GUILayout.Width(30));
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Toggle("", false, GUILayout.Width(30));
                    }
                }

                rendOption.ExtendOption = EditorGUILayout.Foldout(rendOption.ExtendOption, rendOption.rend.name);
            }

            if (rendOption.ExtendOption)
            {
                for (int i = 0; i < rendOption.changeMaterialsOptions.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("",GUILayout.Width(60));
                        var toggle = EditorGUILayout.Toggle("", rendOption.changeMaterialsOptions[i].change, GUILayout.Width(25));
                        if (toggle != rendOption.changeMaterialsOptions[i].change)
                        {
                            // 変更があった場合レイヤー内に伝播
                            if (menuElements[menuReorderableList.index].isToggle)
                            {
                                ToggleMaterialOption(menuElements[menuReorderableList.index], rendOption.rend, i, toggle);
                            }
                            else
                            {
                                ToggleMaterialOption(menuElements[menuReorderableList.index].layer, rendOption.rend, i, toggle);
                            }
                        }
                        
                        EditorGUILayout.LabelField(rendOption.rend.sharedMaterials[i].name,  GUILayout.Width(95));
                        if (!rendOption.changeMaterialsOptions[i].change)
                        {
                            EditorGUILayout.LabelField("",  GUILayout.Width(75));
                        }
                        else
                        {
                            if (EditorGUILayout.Toggle("", rendOption.changeMaterialsOptions[i].delay < 0, GUILayout.Width(25)))
                            {
                                if (rendOption.changeMaterialsOptions[i].delay >= 0)
                                {
                                    rendOption.changeMaterialsOptions[i].delay = -1f;
                                }
                            }
                            else
                            {
                                if (rendOption.changeMaterialsOptions[i].delay < 0)
                                {
                                    rendOption.changeMaterialsOptions[i].delay = 0f;
                                }
                            }

                            using (new EditorGUI.DisabledScope(rendOption.changeMaterialsOptions[i].delay < 0))
                            {
                                rendOption.changeMaterialsOptions[i].delay = EditorGUILayout.FloatField("",
                                    rendOption.changeMaterialsOptions[i].delay, GUILayout.Width(50));
                            }
                        }
                        
                        if (toggle)
                        {
                            rendOption.changeMaterialsOptions[i].material = (Material) EditorGUILayout.ObjectField("",
                                rendOption.changeMaterialsOptions[i].material, typeof(Material), false,GUILayout.Width(150));
                        }
                        else
                        {
                            rendOption.changeMaterialsOptions[i].material = rendOption.rend.sharedMaterials[i];
                            using (new EditorGUI.DisabledScope(true))
                            {
                                /*EditorGUILayout.ObjectField("",
                                    new Material(Shader.Find("Unlit/Color")){name = "No Change"}, typeof(Object), false,GUILayout.Width(150));*/
                                EditorGUILayout.LabelField("NoChange",GUILayout.Width(150));
                            }
                        }
                    }
                }
                for (int i = 0; i < rendOption.changeBlendShapeOptions.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("",GUILayout.Width(60));
                        var toggle = EditorGUILayout.Toggle("", rendOption.changeBlendShapeOptions[i].change, GUILayout.Width(25));
                        if (toggle != rendOption.changeBlendShapeOptions[i].change)
                        {
                            // 変更があった場合レイヤー内に伝播
                            if (menuElements[menuReorderableList.index].isToggle)
                            {
                                ToggleBlendshapeOption(menuElements[menuReorderableList.index], rendOption.rend as SkinnedMeshRenderer, i, toggle);
                            }
                            else
                            {
                                ToggleBlendshapeOption(menuElements[menuReorderableList.index].layer, rendOption.rend as SkinnedMeshRenderer, i, toggle);
                            }
                        }
                        rendOption.changeBlendShapeOptions[i].change = toggle;
                        
                        EditorGUILayout.LabelField(rendOption.rend.GetMesh().GetBlendShapeName(i),  GUILayout.Width(125));
                        if (!rendOption.changeBlendShapeOptions[i].change)
                        {
                            EditorGUILayout.LabelField("",  GUILayout.Width(95));
                        }
                        else
                        {
                            if (EditorGUILayout.Toggle("", rendOption.changeBlendShapeOptions[i].delay < 0, GUILayout.Width(25)))
                            {
                                if (rendOption.changeBlendShapeOptions[i].delay >= 0)
                                {
                                    rendOption.changeBlendShapeOptions[i].delay = -1f;
                                }
                            }
                            else
                            {
                                if (rendOption.changeBlendShapeOptions[i].delay < 0)
                                {
                                    rendOption.changeBlendShapeOptions[i].delay = 0f;
                                }
                            }
                            using (new EditorGUI.DisabledScope(rendOption.changeBlendShapeOptions[i].delay<0))
                            {
                                rendOption.changeBlendShapeOptions[i].delay = EditorGUILayout.FloatField("", rendOption.changeBlendShapeOptions[i].delay, GUILayout.Width(50));
                                rendOption.changeBlendShapeOptions[i].duration = EditorGUILayout.FloatField("", rendOption.changeBlendShapeOptions[i].duration, GUILayout.Width(50));
                            }
                        }

                        if (toggle)
                        {
                            rendOption.changeBlendShapeOptions[i].weight =
                                GUILayout.HorizontalSlider(rendOption.changeBlendShapeOptions[i].weight, 0f, 100f,GUILayout.Width(40));
                            rendOption.changeBlendShapeOptions[i].weight =
                                EditorGUILayout.FloatField(rendOption.changeBlendShapeOptions[i].weight,GUILayout.Width(30));
                        }
                        else
                        {
                            rendOption.changeBlendShapeOptions[i].weight = GetDefaultBlendshape(rendOption.rend as SkinnedMeshRenderer, i);
                            using (new EditorGUI.DisabledScope(true))
                            {
                                var noChange =
                                    GUILayout.HorizontalSlider
                                        (-1f, 0f, 100f,GUILayout.Width(40));
                                EditorGUILayout.LabelField("NoChange",GUILayout.Width(30));
                            }
                        }
                    }
                }
            }
        }

        void SaveAnim(string path)
        {
            path = FileUtil.GetProjectRelativePath(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string fileDir = System.IO.Path.GetDirectoryName (path);
            for (int i = 0; i < menuElements.Count; i++)
            {
                var menuElement = menuElements[i];
                var activateAnim = new AnimationClipCreator(menuElement.name+"_Activate",avatar.gameObject);
                var inactivateAnim = new AnimationClipCreator(menuElement.name+"_Inactivate",avatar.gameObject);
                foreach (var item in menuElement.SafeActiveItems())
                {
                    if (item.active)
                    {
                        SaveElementActive(item,activateAnim);
                    }
                    else
                    {
                        SaveElementInactive(item,activateAnim);
                    }
                }
                foreach (var item in menuElement.SafeInactiveItems())
                {
                    if (item.active)
                    {
                        SaveElementActive(item,inactivateAnim);
                    }
                    else
                    {
                        SaveElementInactive(item,inactivateAnim);
                    }
                }
                activateAnim.CreateAsset(Path.Combine(fileDir,fileName+"_Activate.anim"));
                inactivateAnim.CreateAsset(Path.Combine(fileDir,fileName+"_InactivateAnim.anim"));
            }
            SaveMaterials(fileDir,false);
        }

        void Setup(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            string fileDir = Path.GetDirectoryName (path);
#if VRC_SDK_VRCSDK3
            var p = new ParametersCreater(fileName);
            var c = new AnimatorControllerCreator(fileName,false);
            // var m = new MenuCreater(fileName);
            
            var idleAnim = new AnimationClipCreator("Idle", avatar.gameObject).CreateAsset(path, true);

            var toggleMenuElements = menuElements.Where(e => e.isToggle).ToList();
            for(int i = 0; i <toggleMenuElements.Count ; i++)
            {
                var menuElement = toggleMenuElements[i];
                if (menuElement.isToggle)
                {
                    var param = data.saveName + "_Toggle" + i.ToString();
                    p.AddParam(param,menuElement.isDefault,menuElement.isSaved);
                    c.CreateLayer(param);
                    c.AddParameter(param,menuElement.isDefault);
                    if (menuElement.isRandom)
                    {
                        c.AddDefaultState("Initialize",null);
                        c.AddState("Default",null);
                        c.AddTransition("Initialize","Default");
                        c.ParameterDriver("Initialize",param,0,1,0.5f);
                    }
                    else
                    {
                        c.AddDefaultState("Default",null);
                    }
                    var activeAnim = new AnimationClipCreator(menuElement.name+"_Active",avatar.gameObject);
                    var activateAnim = new AnimationClipCreator(menuElement.name+"_Activate",avatar.gameObject);
                    var inactiveAnim = new AnimationClipCreator(menuElement.name+"_Inactive",avatar.gameObject);
                    var inactivateAnim = new AnimationClipCreator(menuElement.name+"_Inactivate",avatar.gameObject);
                    
                    var activeItems = menuElement.SafeActiveItems();
                    activeItems.AddRange(ComputeLayerAnotherItems(menuElement));
                    
                    var inactiveItems = menuElement.SafeInactiveItems();
                    inactiveItems.AddRange(ComputeLayerInactiveItems(menuElement));

                    // rend option (material,blend shapeの変更適応)
                    foreach (var activeItem in activeItems)
                    {
                        var inactiveItem = inactiveItems.FirstOrDefault(e => activeItem.obj == e.obj);
                        if (inactiveItem != null)
                        {
                            RendererOptionTransition(inactiveItem,activeItem,activateAnim);
                            RendererOptionTransition(activeItem,inactiveItem,inactivateAnim);
                        }
                    }
                    
                    // On時のTransitionとIdle
                    foreach (var item in activeItems)
                    {
                        if (item.active)
                        {
                            SaveElementActive(item,activateAnim,activeAnim, menuElement);
                        }
                        else
                        {
                            SaveElementInactive(item,activateAnim,activeAnim, menuElement);
                        }
                        
                    }
                    // Off時のTransitionとIdle
                    foreach (var item in inactiveItems)
                    {
                        if (item.active)
                        {
                            SaveElementActive(item,inactivateAnim,inactiveAnim, menuElement);
                        }
                        else
                        {
                            SaveElementInactive(item,inactivateAnim,inactiveAnim, menuElement);
                        }
                    }
                    
                    c.AddState("Active", activeAnim.CreateAsset(path,true));
                    c.AddState("Activate", activateAnim.CreateAsset(path,true));
                    c.AddState("Inactive", inactiveAnim.CreateAsset(path,true));
                    c.AddState("Inactivate", inactivateAnim.CreateAsset(path,true));
                    c.AddState("ActiveIdle", data.idleOverride ? activeAnim.Create() : idleAnim);
                    c.AddState("InactiveIdle", data.idleOverride ? inactiveAnim.Create() : idleAnim);
                    c.AddTransition("Default","Active",param,true);
                    c.AddTransition("Default","Inactive",param,false);
                    c.AddTransition("Activate","Active");
                    c.AddTransition("Active","ActiveIdle");
                    c.AddTransition("ActiveIdle","Inactivate",param,false);
                    c.AddTransition("Inactivate","Inactive");
                    c.AddTransition("Inactive","InactiveIdle");
                    c.AddTransition("InactiveIdle","Activate",param,true);
                    //m.AddToggle(menuElement.name,menuElement.icon,param);
                    menuElement.param = param;
                    menuElement.value = 1;
                }
            }

            foreach (LayerGroup layer in Enum.GetValues(typeof(LayerGroup)))
            {
                var layerMenuElements = menuElements.Where(e => !e.isToggle && e.layer == layer).ToList();
                if (layerMenuElements.Count == 0) continue;
                var param = data.saveName + "_" + layer.ToString();
                p.AddParam(param,0,data.layerSettingses[(int) layer].isSaved);
                c.CreateLayer(param);
                
                layerMenuElements = layerMenuElements.OrderBy(e => e.isTaboo ? 100 : 0).ToList();
                
                var d = data.layerSettingses[(int) layer].GetDefaultElement(data.menuElements);
                if (d==null)
                {
                    var menu = new MenuElement()
                    {
                        name = "Default",
                        activeItems = ComputeDefaultItems(layer),
                        isToggle = false,
                        isTaboo = true,
                        layer = layer,
                    };
                    layerMenuElements.Insert(0,menu);
                }
                else
                {
                    layerMenuElements.Remove(d);
                    layerMenuElements.Insert(0,d);
                }
                
                if (data.layerSettingses[(int) layer].isRandom)
                {
                    // ランダム設定の反映
                    c.AddDefaultState("Initialize",null);
                    c.AddState("Default",null);
                    c.AddTransition("Initialize","Default");
                    c.ParameterDriver("Initialize",param,
                        layerMenuElements[0].isTaboo ? 1 : 0,
                        layerMenuElements.Count(e=>!e.isTaboo) - 1 + (layerMenuElements[0].isTaboo ? 1 : 0) );
                }
                else
                {
                    c.AddDefaultState("Default",null);
                }
                
                for (int i = 0; i < layerMenuElements.Count; i++)
                {
                    var menuElement = layerMenuElements[i];
                    var itemElements = menuElement.SafeActiveItems();
                    itemElements.AddRange(ComputeLayerAnotherItems(menuElement));
                    var activeAnim = new AnimationClipCreator(layer.ToString() + i.ToString() + "_Active", avatar.gameObject);
                    foreach (var item in itemElements)
                    {
                        activeAnim.AddKeyframe_Gameobject(item.obj,0f,item.active);
                        activeAnim.AddKeyframe_Gameobject(item.obj,1f/60f,item.active);
                
                        // option処理
                        foreach (var rendOption in item.rendOptions)
                        {
                            if (rendOption.rend == null) continue;
                            if (rendOption.rend is SkinnedMeshRenderer)
                            {
                                activeAnim.AddKeyframe(0f, rendOption.rend as SkinnedMeshRenderer, "m_Enabled", rendOption.RendEnable ? 1 : 0);
                                activeAnim.AddKeyframe(1f/60f, rendOption.rend as SkinnedMeshRenderer, "m_Enabled", rendOption.RendEnable ? 1 : 0);
                            }
                            else
                            if(rendOption.rend is MeshRenderer)
                            {
                                activeAnim.AddKeyframe(0f, rendOption.rend as MeshRenderer, "m_Enabled", rendOption.RendEnable ? 1 : 0);
                                activeAnim.AddKeyframe(1f/60f, rendOption.rend as MeshRenderer, "m_Enabled", rendOption.RendEnable ? 1 : 0);
                            }
                            
                            
                            for (int j = 0; j < rendOption.changeMaterialsOptions.Count; j++)
                            {
                                if (rendOption.changeMaterialsOptions[j].change)
                                {
                                    activeAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOptions[j].material,0f,j);
                                    activeAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOptions[j].material,1f/60f,j);
                                }
                                else
                                if(data.materialOverride)
                                {
                                    // デフォルトのマテリアルで上書き（同期エラー対策）
                                    activeAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[j],0f,j);
                                    activeAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[j],1f/60f,j);
                                }
                                /*activeAnim.AddKeyframe_MaterialParam(0f, rendOption.rend, "_AnimationTime", 1f);
                                activeAnim.AddKeyframe_MaterialParam(1f/60f, rendOption.rend, "_AnimationTime", 1f);*/
                            }
                            for (int j = 0; j < rendOption.changeBlendShapeOptions.Count; j++)
                            {
                                if (rendOption.changeBlendShapeOptions[j].change)
                                {
                                    var rs = rendOption.rend as SkinnedMeshRenderer;
                                    activeAnim.AddKeyframe(0f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(j) , rendOption.changeBlendShapeOptions[j].weight);
                                    activeAnim.AddKeyframe(1f/60f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(j) , rendOption.changeBlendShapeOptions[j].weight);
                                }
                            }
                        }
                    }
                    c.AddState(i.ToString() + "_Active", activeAnim.CreateAsset(path,true));
                    c.AddState(i.ToString() + "_Idle",  data.idleOverride ? activeAnim.Create() : idleAnim);

                    if (menuElement.isTaboo ||
                        (data.layerSettingses[(int) layer].isRandom && menuElement.activeSyncElements.Count != 0))
                    {
                        c.AddState(i.ToString() + "_Initialize",  activeAnim.Create());
                        c.AddTransition("Default",i.ToString() + "_Initialize",param,i);
                        if (menuElement.isTaboo)
                        {
                            // タブー設定に当たったらデフォルトに送る
                            c.ParameterDriver(i.ToString() + "_Initialize",param,0);
                            c.AddTransition(i.ToString() + "_Initialize",0.ToString() + "_Active");
                        }
                        else
                        {
                            c.AddTransition(i.ToString() + "_Initialize",i.ToString() + "_Active");
                        }
                    }
                    else
                    {
                        c.AddTransition("Default",i.ToString() + "_Active",param,i);
                    }
                    c.AddTransition(i.ToString() + "_Active",i.ToString() + "_Idle");
                    
                    //m.AddToggle(menuElement.name,menuElement.icon,param,i);
                    menuElement.param = param;
                    menuElement.value = i;
                }
                
                for (int i = 0; i < layerMenuElements.Count; i++)
                {
                    for (int j = 0; j < layerMenuElements.Count; j++)
                    {
                        if(i==j) continue;
                        var transitionAnim = new AnimationClipCreator( layer.ToString() + j.ToString() + "to" + i.ToString() + "_Transition", avatar.gameObject);
                        var fromItems = layerMenuElements[j].SafeActiveItems();
                        fromItems.AddRange(ComputeLayerAnotherItems(layerMenuElements[j]));
                        var toItems = layerMenuElements[i].SafeActiveItems();
                        toItems.AddRange(ComputeLayerAnotherItems(layerMenuElements[i]));
                        foreach (var item in ComputeDefaultItems(layer))
                        { 
                            var fromItem = fromItems.FirstOrDefault(e => e.obj == item.obj);
                            var toItem = toItems.FirstOrDefault(e => e.obj == item.obj);
                            // レイヤー内で参照されているアイテムすべてについてとらんじちよん
                            if (fromItem == null && toItem == null)
                            {
                                
                            }
                            else if (fromItem == null)
                            {
                                SaveElementTransition(toItem,transitionAnim, false);
                            }
                            else if (toItem == null)
                            {
                                SaveElementTransition(fromItem,transitionAnim,true);
                            }
                            else
                            {
                                // rend option (material,blend shapeの変更適応)
                                RendererOptionTransition(fromItem,toItem,transitionAnim);
                                // 次のステートでも表示状態の変更がなければ状態変更のアニメをつけない処理
                                if (data.createAnimWhenNotChangedActive || fromItem.active != toItem.active)
                                {
                                    // transition animation
                                    SaveElementTransition(toItem,transitionAnim, false);
                                }
                            }
                        }
                        c.AddState(j.ToString() + "to" + i.ToString() + "_Transition", transitionAnim.CreateAsset(path,true));
                        c.AddTransition(j.ToString() + "_Idle",j.ToString() + "to" + i.ToString() + "_Transition",param,i);
                        c.AddTransition(j.ToString() + "to" + i.ToString() + "_Transition",i.ToString() + "_Active");
                    }
                }
            }

            // パラメーターシンク
            foreach (var menuElement in menuElements)
            {
                foreach (var syncElement in menuElement.activeSyncElements)
                {
                    if (!syncElement.syncOn && !syncElement.syncOff) continue;
                    var syncParam = menuElements.FirstOrDefault(e => e.guid == syncElement.guid);
                    if (syncParam == null) continue;

                    c.SetEditLayer(c.GetEditLayer(menuElement.param));
                    if (syncElement.delay < 0)
                    {
                        if (menuElement.isToggle)
                        {
                            c.ParameterDriver( "Active" , syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                        else
                        {
                            c.ParameterDriver( menuElement.value.ToString() + "_Active", syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                    }
                    else
                    {
                        if (menuElement.isToggle)
                        {
                            c.ParameterDriver( "Activate" , syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                        else
                        {
                            var states = c.GetStates(".*" + "to" + menuElement.value.ToString() + "_Transition").Distinct().ToArray();
                            foreach (var state in states)
                            {
                                c.ParameterDriver( state, syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                            }
                        }
                    }

                    // ランダムで代入時も同様に処理
                    if (data.layerSettingses[(int) menuElement.layer].isRandom)
                    {
                        if (menuElement.isToggle)
                        {
                            c.ParameterDriver( menuElement.value.ToString() + "_Initialize", syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                        else
                        {
                            c.ParameterDriver( menuElement.value.ToString() + "_Initialize", syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                    }
                }

                if (menuElement.isToggle)
                {
                    foreach (var syncElement in menuElement.inactiveSyncElements)
                    {
                        if (!syncElement.syncOn && !syncElement.syncOff) continue;
                        var syncParam = menuElements.FirstOrDefault(e => e.guid == syncElement.guid);
                        if (syncParam == null) continue;
                        
                        if (syncElement.delay < 0)
                        {
                            c.SetEditLayer(c.GetEditLayer(menuElement.param));
                            c.ParameterDriver( "Inactive" , syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                        else
                        {
                            c.SetEditLayer(c.GetEditLayer(menuElement.param));
                            c.ParameterDriver( "Inactivate" , syncParam.param, syncElement.syncOn ? syncParam.value : 0f);
                        }
                    }
                }
            }

            MenuCreater pm = new MenuCreater("ParentMenu");
            MenuCreater m = new MenuCreater(fileName);
            if (data.useMenuTemplate)
            {
                // テンプレートの整合性チェック
                data.ReloadTemplates();
                foreach (var menu in data.menuTemplate)
                {
                    if (String.IsNullOrWhiteSpace(menu.menuGUID))
                    {
                        var a = CreateTemplateMenu(menu, path);
                        m.AddSubMenu(a.CreateAsset(path, true),menu.name,menu.icon);
                    }
                    else
                    {
                        var a = data.menuElements.FirstOrDefault(e => e.guid == menu.menuGUID);
                        if (a != null)
                        {
                            if (a.isToggle)
                            {
                                m.AddToggle(menu.name,menu.icon,a.param);
                            }
                            else
                            {
                                m.AddToggle(menu.name,menu.icon,a.param,a.value);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var menuElement in menuElements)
                {
                    if (menuElement.isToggle)
                    {
                        m.AddToggle(menuElement.name,menuElement.icon,menuElement.param);
                    }
                    else
                    {
                        m.AddToggle(menuElement.name,menuElement.icon,menuElement.param,menuElement.value);
                    }
                }
            }
            pm.AddSubMenu(m.CreateAsset(path, true),data.saveName,data.icon);
            

            //p.LoadParams(c,true);
            var mod = new AvatarModifyTool(avatar,fileDir);
            var assets = CreateInstance<AvatarModifyData>();
            {
                assets.fx_controller = c.CreateAsset(path, true);
                assets.parameter = p.CreateAsset(path, true);
                assets.menu = pm.CreateAsset(path,true);
            }
            AssetDatabase.AddObjectToAsset(assets,path);
            data.assets = assets;
            ApplySettings(mod).ModifyAvatar(assets,EnvironmentGUIDs.prefix);
#endif
            SaveMaterials(path,true);
        }
        
#if VRC_SDK_VRCSDK3
        MenuCreater CreateTemplateMenu(MenuTemplate menu,string path)
        {
            var m = new MenuCreater(menu.name);
            foreach (var element in menu.childs)
            {
                if (String.IsNullOrWhiteSpace(element.menuGUID))
                {
                    var a = CreateTemplateMenu(element, path);
                    m.AddSubMenu(a.CreateAsset(path, true),element.name,element.icon);
                }
                else
                {
                    var a = data.menuElements.FirstOrDefault(e => e.guid == element.menuGUID);
                    if (a != null)
                    {
                        if (a.isToggle)
                        {
                            m.AddToggle(element.name,element.icon,a.param);
                        }
                        else
                        {
                            m.AddToggle(element.name,element.icon,a.param,a.value);
                        }
                    }
                }
            }
            return m;
        }
#endif

        void SaveElementTransition(ItemElement element,
            AnimationClipCreator transitionAnim, bool invert = false,
            MenuElement parentmenu = null)
        {
            if (invert)
            {
                if (element.active)
                {
                    SaveElementInactive(element,transitionAnim,null,parentmenu);
                }
                else
                {
                    SaveElementActive(element,transitionAnim,null,parentmenu);
                }
            }
            else
            {
                if (element.active)
                {
                    SaveElementActive(element,transitionAnim,null,parentmenu);
                }
                else
                {
                    SaveElementInactive(element,transitionAnim,null,parentmenu);
                }
            }
        }

        void SaveElementActive(ItemElement element,
            AnimationClipCreator transitionAnim = null, AnimationClipCreator setAnim = null,
            MenuElement parentmenu = null)
        {
            if (setAnim != null)
            {
                ActiveAnimation(setAnim,element.obj,true);
                
                // option処理
                foreach (var rendOption in element.rendOptions)
                {
                    if(rendOption.rend == null) continue;
                    setAnim.AddKeyframe(0f, rendOption.rend, "m_Enabled", rendOption.RendEnable ? 1 : 0);
                    setAnim.AddKeyframe(1f/60f, rendOption.rend, "m_Enabled", rendOption.RendEnable ? 1 : 0);
                    
                    for (int i = 0; i < rendOption.changeMaterialsOptions.Count; i++)
                    {
                        if (rendOption.changeMaterialsOptions[i].change)
                        {
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOptions[i].material,0f,i);
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.changeMaterialsOptions[i].material,1f/60f,i);
                        }
                        else
                        if(data.materialOverride)
                        {
                            // デフォルトのマテリアルで上書き（同期エラー対策）
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],0f,i);
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],1f/60f,i);
                        }
                        /*setAnim.AddKeyframe_MaterialParam(0f, rendOption.rend, "_AnimationTime", 1f);
                        setAnim.AddKeyframe_MaterialParam(1f/60f, rendOption.rend, "_AnimationTime", 1f);*/
                    }
                    for (int i = 0; i < rendOption.changeBlendShapeOptions.Count; i++)
                    {
                        if (rendOption.changeBlendShapeOptions[i].change)
                        {
                            var rs = rendOption.rend as SkinnedMeshRenderer;
                            setAnim.AddKeyframe(0f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(i) , rendOption.changeBlendShapeOptions[i].weight);
                            setAnim.AddKeyframe(1f/60f, rs, "blendShape."+rs.GetMesh().GetBlendShapeName(i) , rendOption.changeBlendShapeOptions[i].weight);
                        }
                    }
                }

                if (data.materialOverride)
                {
                    setAnim.AddKeyframe_Scale(0f,element.obj.transform,element.obj.transform.localScale);
                    setAnim.AddKeyframe_Scale(1f/60f,element.obj.transform,element.obj.transform.localScale);
                }
            }

            if (transitionAnim != null)
            {
                // 上書き設定
                if (parentmenu != null)
                {
                    if (parentmenu.isOverrideActivateTransition)
                    {
                        if (parentmenu.overrideActivateTransition.type == FeedType.None)
                        {
                            ActiveAnimation(transitionAnim,element.obj,true,parentmenu.overrideActivateTransition.delay);
                        }
                        else
                        if(parentmenu.overrideActivateTransition.type == FeedType.Scale)
                        {
                            ScaleAnimation(transitionAnim, element.obj, parentmenu.overrideActivateTransition.delay, parentmenu.overrideActivateTransition.duration, true);
                        }
                        else
                        if(parentmenu.overrideActivateTransition.type == FeedType.Shader)
                        {
                            ShaderAnimation(transitionAnim, element.obj, parentmenu.overrideActivateTransition.delay, 
                                parentmenu.overrideActivateTransition.duration, 
                                parentmenu.overrideActivateTransition.animationMaterial, 
                                parentmenu.overrideActivateTransition.animationParam,
                                parentmenu.overrideActivateTransition.animationParamOff,
                                parentmenu.overrideActivateTransition.animationParamOn);
                        }
                        else
                        {
                            ShaderAnimation(transitionAnim, element.obj, parentmenu.overrideActivateTransition.delay, parentmenu.overrideActivateTransition.duration,
                                parentmenu.overrideActivateTransition.type.GetMaterialByType(), "_AnimationTime",
                                0f,1f);
                            ChangeMaterialDefault(transitionAnim,element.obj,parentmenu.overrideActivateTransition.delay+parentmenu.overrideActivateTransition.duration+1f/60f);
                        }

                        return;
                    }
                }
                
                if (element.type == FeedType.None)
                {
                    ActiveAnimation(transitionAnim,element.obj,true,element.delay);
                }
                else
                if(element.type == FeedType.Scale)
                {
                    ScaleAnimation(transitionAnim, element.obj, element.delay, element.duration, true);
                }
                else
                if(element.type == FeedType.Shader)
                {
                    ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration, element.animationMaterial, element.animationParam,element.animationParamOff,element.animationParamOn);
                }
                else
                {
                    ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                        element.type.GetMaterialByType(), "_AnimationTime",
                        0f,1f);
                    ChangeMaterialDefault(transitionAnim,element.obj,element.delay+element.duration+1f/60f);
                }
            }
        }
        void SaveElementInactive(ItemElement element,
            AnimationClipCreator transitionAnim = null, AnimationClipCreator setAnim = null,
            MenuElement parentmenu = null)
        {
            if (setAnim != null)
            {
                ActiveAnimation(setAnim,element.obj,false);
                
                // option処理
                foreach (var rendOption in element.rendOptions)
                {
                    if(rendOption.rend == null) continue;
                    for (int i = 0; i < rendOption.changeMaterialsOptions.Count; i++)
                    {
                        if (rendOption.changeMaterialsOptions[i].change)
                        {
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],0f,i);
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],1f/60f,i);
                        }
                        if(data.materialOverride)
                        {
                            // デフォルトのマテリアルで上書き（同期エラー対策）
                            /*setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],0f,i);
                            setAnim.AddKeyframe_Material(rendOption.rend,rendOption.rend.sharedMaterials[i],1f/60f,i);*/
                        }
                        /*setAnim.AddKeyframe_MaterialParam(0f, rendOption.rend, "_AnimationTime", 0f);
                        setAnim.AddKeyframe_MaterialParam(1f/60f, rendOption.rend, "_AnimationTime", 0f);*/
                    }
                    for (int i = 0; i < rendOption.changeBlendShapeOptions.Count; i++)
                    {
                        if (rendOption.changeBlendShapeOptions[i].change)
                        {
                            var rs = rendOption.rend as SkinnedMeshRenderer;
                            setAnim.AddKeyframe(0f, rs, "blendShape."+rendOption.rend.GetMesh().GetBlendShapeName(i) , rs.GetBlendShapeWeight(i));
                            setAnim.AddKeyframe(1f/60f, rs, "blendShape."+rendOption.rend.GetMesh().GetBlendShapeName(i) , rs.GetBlendShapeWeight(i));
                        }
                    }
                }
            }
            if(transitionAnim != null)
            {
                // 上書き設定
                if (parentmenu != null)
                {
                    if (parentmenu.isOverrideInactivateTransition)
                    {
                        if (parentmenu.overrideInactivateTransition.type == FeedType.None)
                        {
                            ActiveAnimation(transitionAnim, element.obj,false, parentmenu.overrideInactivateTransition.delay);
                        }
                        else
                        if(parentmenu.overrideInactivateTransition.type == FeedType.Scale)
                        {
                            ScaleAnimation(transitionAnim, element.obj, parentmenu.overrideInactivateTransition.delay, parentmenu.overrideInactivateTransition.duration, false);
                        }
                        else
                        if(parentmenu.overrideInactivateTransition.type == FeedType.Shader)
                        {
                            ShaderAnimation(transitionAnim, element.obj, parentmenu.overrideInactivateTransition.delay,
                                parentmenu.overrideInactivateTransition.duration,
                                parentmenu.overrideInactivateTransition.animationMaterial,
                                parentmenu.overrideInactivateTransition.animationParam,
                                parentmenu.overrideInactivateTransition.animationParamOn,
                                parentmenu.overrideInactivateTransition.animationParamOff);
                        }
                        else
                        {
                            ShaderAnimation(transitionAnim, element.obj, parentmenu.overrideInactivateTransition.delay, parentmenu.overrideInactivateTransition.duration,
                                parentmenu.overrideInactivateTransition.type.GetMaterialByType(), "_AnimationTime",
                                1f,0f);
                            ChangeMaterialDefault(transitionAnim,element.obj,parentmenu.overrideInactivateTransition.delay+parentmenu.overrideInactivateTransition.duration+1f/60f);
                        }

                        return;
                    }
                }
                
                if (element.type == FeedType.None)
                {
                    ActiveAnimation(transitionAnim, element.obj,false, element.delay);
                }
                else
                if(element.type == FeedType.Scale)
                {
                    ScaleAnimation(transitionAnim, element.obj, element.delay, element.duration, false);
                }
                else
                if(element.type == FeedType.Shader)
                {
                    ShaderAnimation(transitionAnim, element.obj,element.delay,element.duration, element.animationMaterial, element.animationParam,element.animationParamOn, element.animationParamOff);
                }
                else
                {
                    ShaderAnimation(transitionAnim, element.obj, element.delay, element.duration,
                        element.type.GetMaterialByType(), "_AnimationTime",
                        1f,0f);
                    ActiveAnimation(transitionAnim,element.obj,false,element.delay+element.duration+1f/60f);
                    ChangeMaterialDefault(transitionAnim,element.obj,element.delay+element.duration+2f/60f);
                }
            }
        }

        void RendererOptionTransition(ItemElement fromElement,ItemElement toElement, AnimationClipCreator transitionAnim)
        {
            foreach (var rendOpt in toElement.rendOptions)
            {
                if(rendOpt.rend == null) continue;
                var rend = rendOpt.rend;
                var to = toElement.rendOptions.FirstOrDefault(r => r.rend == rend);
                if(to==null) continue;
                var from = fromElement.rendOptions.FirstOrDefault(r => r.rend == rend);
                if(from==null) continue;
                if (from.changeMaterialsOptions.Count == to.changeMaterialsOptions.Count)
                {
                    for (int i = 0; i < to.changeMaterialsOptions.Count; i++)
                    {
                        if (to.changeMaterialsOptions[i].change)
                        {
                            if(to.changeMaterialsOptions[i].material == null) continue;
                            if(from.changeMaterialsOptions[i].material == to.changeMaterialsOptions[i].material) continue;
                            if (to.changeMaterialsOptions[i].delay < 0)
                            {
                        
                            }
                            else
                            {
                                if(to.changeMaterialsOptions[i].delay < 1f/60f)transitionAnim.AddKeyframe_Material(rend,GetDefaultMaterial(rend,i),0f,i);
                                transitionAnim.AddKeyframe_Material(rend,to.changeMaterialsOptions[i].material,to.changeMaterialsOptions[i].delay,i);
                            }
                        }
                    }
                }

                if (from.changeBlendShapeOptions.Count == to.changeBlendShapeOptions.Count)
                {
                    for (int i = 0; i < from.changeBlendShapeOptions.Count; i++)
                    {
                        if (to.changeBlendShapeOptions[i].change)
                        {
                            if (0f > to.changeBlendShapeOptions[i].weight &&
                                to.changeBlendShapeOptions[i].weight > 100f) continue;
                            if (Mathf.Abs(from.changeBlendShapeOptions[i].weight -
                                          to.changeBlendShapeOptions[i].weight) < 1f) continue;
                            if (to.changeBlendShapeOptions[i].delay < 0)
                            {

                            }
                            else
                            {
                                //transitionAnim.AddKeyframe_Material(rend.rend,rend.changeBlendShapeOptions[i].weight);
                                transitionAnim.AddKeyframe(to.changeBlendShapeOptions[i].delay,
                                    rend as SkinnedMeshRenderer,
                                    "blendShape." + rend.GetMesh().GetBlendShapeName(i),
                                    from.changeBlendShapeOptions[i].change
                                        ? from.changeBlendShapeOptions[i].weight
                                        : (rend as SkinnedMeshRenderer).GetBlendShapeWeight(i));
                                transitionAnim.AddKeyframe(
                                    to.changeBlendShapeOptions[i].delay + to.changeBlendShapeOptions[i].duration,
                                    rend as SkinnedMeshRenderer,
                                    "blendShape." + rend.GetMesh().GetBlendShapeName(i),
                                    to.changeBlendShapeOptions[i].weight);
                            }
                        }
                    }
                }
            }
        }

        AnimationClipCreator ScaleAnimation(AnimationClipCreator anim,GameObject obj, float delay = 0f, float duration = 1f,
            bool activate = true,bool bounce = false)
        {
            anim.AddKeyframe_Gameobject(obj,0f,true);
            var defaultScale = obj.transform.localScale;
            if (activate)
            {
                anim.AddKeyframe_Scale( 0f,obj.transform,Vector3.zero);
                anim.AddKeyframe_Scale( delay,obj.transform,Vector3.zero);
                if (bounce)
                {
                    anim.AddKeyframe_Scale(delay+duration*0.6f,obj.transform, defaultScale*1.2f);
                    anim.AddKeyframe_Scale(delay+duration*0.8f,obj.transform, defaultScale*0.9f);
                }
                anim.AddKeyframe_Scale(delay+duration,obj.transform,defaultScale);
            }
            else
            {
                anim.AddKeyframe_Scale(delay,obj.transform,defaultScale);
                if (bounce)
                {
                    anim.AddKeyframe_Scale(delay+duration*0.2f,obj.transform, defaultScale*1.2f);
                    anim.AddKeyframe_Scale(delay+duration*0.4f,obj.transform, defaultScale*0.9f);
                }
                anim.AddKeyframe_Scale( delay+duration,obj.transform,Vector3.zero);
                
                anim.AddKeyframe_Gameobject(obj,delay+duration,false);
                anim.AddKeyframe_Scale(delay+duration+1f/60f,obj.transform,defaultScale);
            }
            return anim;
        }
        
        AnimationClipCreator ShaderAnimation(AnimationClipCreator anim,GameObject obj,float delay = 0f , float duration = 1f,
            Material mat = null,string param = "",float from = 0f, float to = 1f)
        {
            anim.AddKeyframe_Gameobject(obj,delay,true);
            ChangeMaterialShader(anim,obj,mat,delay);
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                anim.AddKeyframe_MaterialParam(delay, rend, param, from);
                anim.AddKeyframe_MaterialParam(delay+duration, rend, param, to);
            }
            
            return anim;
        }

        AnimationClipCreator ActiveAnimation(AnimationClipCreator anim, GameObject obj,bool value,float time = 0f)
        {
            if (time < 1f / 60f)
            {
                anim.AddKeyframe_Gameobject(obj,0f,value);
            }
            else
            {
                anim.AddKeyframe_Gameobject(obj,0f,!value);
            }
            anim.AddKeyframe_Gameobject(obj,time,value);
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

        void ChangeMaterialShader(AnimationClipCreator anim,GameObject obj, Material shader,float time = 0f)
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

        // 生成したマテリアルを保存する
        void SaveMaterials(string path,bool subAsset = false)
        {
            foreach (var mats in matlist)
            {
                foreach (var mat in mats.Value)
                {
                    if (subAsset)
                    {
                        AssetDatabase.AddObjectToAsset(mat.Value,path);
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(mat.Value, AssetDatabase.GenerateUniqueAssetPath(
                            Path.Combine(path,mat.Value.name + ".mat")));
                    }
                }
            }
            matlist = new Dictionary<Material, Dictionary<Material, Material>>();
        }

        // メニューで設定されている状態に，シーンのアクティブ，マテリアル，BlendShapeを反映する
        void SetObjectActiveForScene(MenuElement menu,bool active = true, bool material = true, bool blendShape = true)
        {
            RevertObjectActiveForScene(active,material,blendShape);
            if (menu == null) return;

            var activeItems = menu.SafeActiveItems();
            activeItems.AddRange(ComputeLayerAnotherItems(menu));
                    
            var inactiveItems = menu.SafeInactiveItems();
            inactiveItems.AddRange(ComputeLayerInactiveItems(menu));
            
            var items = displayItemMode ? activeItems : inactiveItems;
            
            foreach (var item in items)
            {
                if (active)
                {
                    GetDefaultActive(item.obj);
                    item.obj.SetActive(item.active);
                }
                foreach (var option in item.rendOptions)
                {
                    if (option.rend)
                    {
                        GetDefaultRendEnable(option.rend);
                        option.rend.enabled = option.RendEnable;
                        
                        if (material)
                        {
                            for (int i = 0; i < option.changeMaterialsOptions.Count; i++)
                            {
                                GetDefaultMaterial(option.rend, i);
                                if (option.changeMaterialsOptions[i].change)
                                {
                                    var mats = option.rend.sharedMaterials.ToArray();
                                    mats[i] = option.changeMaterialsOptions[i].material;
                                    option.rend.sharedMaterials = mats.ToArray();
                                }
                            }
                        }
                    
                        if (blendShape)
                        {
                            for (int i = 0; i < option.changeBlendShapeOptions.Count; i++)
                            {
                                GetDefaultBlendshape(option.rend as SkinnedMeshRenderer, i);
                                if (option.changeBlendShapeOptions[i].change)
                                {
                                    (option.rend as SkinnedMeshRenderer)?.SetBlendShapeWeight(i,option.changeBlendShapeOptions[i].weight);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // シーンのアクティブ，マテリアル，BlendShapeをリセットする
        void RevertObjectActiveForScene(bool active = true, bool material = true, bool blendShape = true)
        {
            if (active)
            {
                foreach (var oa in defaultActive)
                {
                    oa.Key.SetActive(oa.Value);
                }
            }

            if (material)
            {
                foreach (var dm in defaultMaterials)
                {
                    dm.Key.sharedMaterials = dm.Value.ToArray();
                }
            }

            if (blendShape)
            {
                foreach (var db in defaultBlendShapes)
                {
                    db.Key.rend.SetBlendShapeWeight(db.Key.index,db.Value);
                }
            }
        }

        // デフォルトのアクティブ状態(シーン)を記録&取得する
        bool GetDefaultActive(GameObject obj)
        {
            if (!obj) return false;
                
            if (!defaultActive.ContainsKey(obj))
            {
                defaultActive.Add(obj,obj.activeSelf);
            }

            return defaultActive[obj];
        }

        // デフォルトのマテリアル状態(シーン)を記録&取得する
        bool GetDefaultRendEnable(Renderer rend)
        {
            if (rend == null) return false;
            
            if (!defaultRendEnable.ContainsKey(rend))
            {
                defaultRendEnable.Add(rend,rend.enabled);
            }

            return defaultRendEnable[rend];
        }

        // デフォルトのマテリアル状態(シーン)を記録&取得する
        Material GetDefaultMaterial(Renderer rend, int index)
        {
            if (rend == null) return null;
            
            if (!defaultMaterials.ContainsKey(rend))
            {
                defaultMaterials.Add(rend,rend.sharedMaterials.ToArray());
            }

            return defaultMaterials[rend][index];
        }
        
        // デフォルトのBlendShape状態(シーン)を記録&取得する
        float GetDefaultBlendshape(SkinnedMeshRenderer rend, int index)
        {
            if (rend == null) return -1f;
            var key = new BlendShapeReference()
            {
                rend = rend,
                index = index,
            };
            
            if (!defaultBlendShapes.ContainsKey(key))
            {
                defaultBlendShapes.Add(key,rend.GetBlendShapeWeight(index));
            }

            return defaultBlendShapes[key];
        }

        // RendererOptionが編集済みかどうか
        bool IsModifyRendererOption(ItemElement item)
        {
            return item.rendOptions.Any(ro =>
                ro.changeMaterialsOptions.Any(e => e.change) ||
                ro.changeBlendShapeOptions.Any(e => e.change));


        }

        // アイテムのアクティブを2メニュー間で合わせる
        void SyncItemActive(List<ItemElement> srcs, List<ItemElement> dsts, bool invert = false, bool checkOptions = true)
        {
            foreach (var dst in dsts)
            {
                var src = srcs.FirstOrDefault(e => e.obj == dst.obj);
                if (src != null)
                {
                    if (checkOptions)
                    {
                        if(!RendOptionEqual(src,dst)) continue;
                        // if(IsModifyRendererOption(dst) || IsModifyRendererOption(src)) continue;
                    }
                    dst.active = invert ? !src.active : src.active;
                }
            }
        }

        bool RendOptionEqual(ItemElement srcs, ItemElement dsts)
        {
            foreach (var src in srcs.rendOptions)
            {
                var dst = dsts.rendOptions.FirstOrDefault(d => d.rend == src.rend);
                if (src != null && dst != null)
                {
                    for (int i = 0; i < src.changeMaterialsOptions.Count &&  i < dst.changeMaterialsOptions.Count; i++)
                    {
                        if (src.changeMaterialsOptions[i].change && dst.changeMaterialsOptions[i].change)
                        {
                            if (src.changeMaterialsOptions[i].material != dst.changeMaterialsOptions[i].material)
                            {
                                return false;
                            }
                        }
                    }
                    for (int i = 0; i < src.changeBlendShapeOptions.Count &&  i < dst.changeBlendShapeOptions.Count; i++)
                    {
                        if (src.changeBlendShapeOptions[i].change && dst.changeBlendShapeOptions[i].change)
                        {
                            if (Mathf.Abs(src.changeBlendShapeOptions[i].weight - dst.changeBlendShapeOptions[i].weight)<1f)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        List<ItemElement> ComputeLayerInactiveItems(MenuElement menu)
        {
            var activeItems = menu.SafeActiveItems();
            if (!menu.isToggle)
            {
                activeItems.AddRange(ComputeLayerAnotherItems(menu));
            }
            
            return activeItems.Where(e=>menu.SafeInactiveItems().All(f=>f.obj!=e.obj)).Select(e=>e.Clone(true)).ToList();
        }
        
        // レイヤー内の未設定アイテムを走査し返す
        List<ItemElement> ComputeLayerAnotherItems(MenuElement menu)
        {
            if (!menu.isToggle)
            {
                var items = GetActiveItems(menu.layer).
                    Where(e => menu.SafeActiveItems().All(f => f.obj != e.obj)).
                    Select(e => e.Clone(true)).ToList();
                if (IsMenuElementDefault(menu))
                {
                    // デフォルトエレメントならアクティブをシーンの状態に合わせる
                    foreach (var item in items)
                    {
                        item.active = GetDefaultActive(item.obj);
                        foreach (var rendOption in item.rendOptions)
                        {
                            if(rendOption.rend == null) continue;
                            rendOption.rend.enabled = GetDefaultRendEnable(rendOption.rend);
                            foreach (var another in items.SelectMany(e=>e.rendOptions))
                            {
                                if (rendOption.rend == another.rend)
                                {
                                    // Material設定の上書き
                                    for (int i = 0; i < rendOption.changeMaterialsOptions.Count; i++)
                                    {
                                        if (rendOption.changeMaterialsOptions[i] != null) break;
                                        if (another.changeMaterialsOptions[i] != null)
                                        {
                                            rendOption.changeMaterialsOptions[i] =
                                                new MaterialOption(rendOption.rend.sharedMaterials[i])
                                                {
                                                    change = true
                                                };
                                        }
                                    }
                                    // BlendShapel設定の上書き
                                    for (int i = 0; i < rendOption.changeBlendShapeOptions.Count; i++)
                                    {
                                        if (rendOption.changeBlendShapeOptions[i].change) break;
                                        if (another.changeBlendShapeOptions[i].change)
                                        {
                                            rendOption.changeBlendShapeOptions[i] =
                                                new BlendShapeOption((rendOption.rend as SkinnedMeshRenderer)?.GetBlendShapeWeight(i) ?? 0f)
                                                {
                                                    change = true
                                                };
                                        }
                                    }
                                    
                                }
                            }
                        }
                    }
                }

                var inactiveItems = GetInactiveItems(menu.layer);
                foreach (var item in items)
                {
                    var inactiveItem = inactiveItems.FirstOrDefault(e => e.obj == item.obj && e.active == item.active);
                    if (inactiveItem != null)
                    {
                        item.delay = inactiveItem.delay;
                        item.duration = inactiveItem.duration;
                        item.type = inactiveItem.type;
                        item.animationMaterial = inactiveItem.animationMaterial;
                        item.animationParam = inactiveItem.path;
                    }
                }
                return items;
            }

            return new List<ItemElement>();
        }

        // そのメニューがデフォルト値か返す
        bool IsMenuElementDefault(MenuElement menu)
        {
            if (menu.isToggle)
            {
                return menu.isDefault;
            }
            else
            {
                return data.layerSettingses[(int) menu.layer].GetDefaultElement(menuElements)
                       == menu;
            }
        }
        
        // レイヤーのデフォルトステートがない場合，シーン状態をデフォルトステートとして記録する
        List<ItemElement> ComputeDefaultItems(LayerGroup layer)
        {
            var items = GetActiveItems(layer);
            foreach (var item in items)
            {
                item.active = item.obj.gameObject.activeSelf;
            }
            return items;
        }

        // レイヤー内の全メニューを走査し，未設定のActiveアイテムを集める
        List<ItemElement> GetActiveItems(LayerGroup layer)
        {
            var items = new List<ItemElement>();
            foreach (var menuElement in menuElements)
            {
                if (!menuElement.isToggle && menuElement.layer == layer)
                {
                    foreach (var item in menuElement.SafeActiveItems())
                    {
                        items.Add(item.Clone());
                    }
                }
            }

            return DistinctList(items);
        }
        
        // レイヤー内の全メニューを走査し，未設定のInactiveアイテムを集める
        List<ItemElement> GetInactiveItems(LayerGroup layer)
        {
            var items = new List<ItemElement>();
            foreach (var menuElement in menuElements)
            {
                if (!menuElement.isToggle && menuElement.layer == layer)
                {
                    foreach (var item in menuElement.SafeInactiveItems())
                    {
                        items.Add(item.Clone());
                    }
                }
            }

            return items;
        }

        // ItemElementの重複削除
        List<ItemElement> DistinctList(List<ItemElement> origin)
        {
            var items = new List<ItemElement>();
            foreach (var item in origin)
            {
                if (items.All(e => e.obj != item.obj))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        // レイヤー内のRendererOptionの有効無効を同期
        void ToggleMaterialOption(LayerGroup layer,Renderer rend,int index,bool val)
        {
            foreach (var r in data.menuElements.
                Where(m=>m.layer == layer).
                SelectMany(m=>m.SafeActiveItems()).
                SelectMany(i=>i.rendOptions))
            {
                if (r.rend == rend)
                {
                    if (!r.changeMaterialsOptions[index].change)
                    {
                        r.changeMaterialsOptions[index].material = rend.sharedMaterials[index];
                    }
                    r.changeMaterialsOptions[index].change = val;
                }
            }
        }
        
        // メニュー内のRendererOptionの有効無効を同期
        void ToggleMaterialOption(MenuElement menu,Renderer rend,int index,bool val)
        {
            foreach (var ie in menu.SafeActiveItems())
            {
                foreach (var ro in ie.rendOptions)
                {
                    if (ro.rend == null) continue;
                    if (ro.rend == rend)
                    {
                        if (!ro.changeMaterialsOptions[index].change)
                        {
                            ro.changeMaterialsOptions[index].material = rend.sharedMaterials[index];
                        }
                        ro.changeMaterialsOptions[index].change = val;
                    }
                }
            }
            foreach (var ie in menu.SafeInactiveItems())
            {
                foreach (var ro in ie.rendOptions)
                {
                    if (ro.rend == rend)
                    {
                        if (ro.rend == null) continue;
                        if (!ro.changeMaterialsOptions[index].change)
                        {
                            ro.changeMaterialsOptions[index].material = rend.sharedMaterials[index];
                        }
                        ro.changeMaterialsOptions[index].change = val;
                    }
                }
            }
        }

        // レイヤー内のRendererOptionの有効無効を同期
        void ToggleBlendshapeOption(LayerGroup layer,SkinnedMeshRenderer rend,int index,bool val)
        {
            foreach (var r in data.menuElements.
                Where(m=>m.layer == layer).
                SelectMany(m=>m.SafeActiveItems()).
                SelectMany(i=>i.rendOptions))
            {
                if (r.rend == null) continue;
                if (r.rend == rend)
                {
                    if (!r.changeBlendShapeOptions[index].change)
                    {
                        r.changeBlendShapeOptions[index] = new BlendShapeOption(GetDefaultBlendshape(rend,index));
                    }
                    r.changeBlendShapeOptions[index].change = val;
                }
            }
        }
        
        // メニュー内のRendererOptionの有効無効を同期
        void ToggleBlendshapeOption(MenuElement menu,SkinnedMeshRenderer rend,int index,bool val)
        {
            foreach (var ie in menu.SafeActiveItems())
            {
                foreach (var ro in ie.rendOptions)
                {
                    if (ro.rend == null) continue;
                    if (ro.rend == rend)
                    {
                        if (!ro.changeBlendShapeOptions[index].change)
                        {
                            ro.changeBlendShapeOptions[index] = new BlendShapeOption(GetDefaultBlendshape(rend,index));
                        }
                        ro.changeBlendShapeOptions[index].change = val;
                    }
                }
            }
            foreach (var ie in menu.SafeInactiveItems())
            {
                foreach (var ro in ie.rendOptions)
                {
                    if (ro.rend == null) continue;
                    if (ro.rend == rend)
                    {
                        if (!ro.changeBlendShapeOptions[index].change)
                        {
                            ro.changeBlendShapeOptions[index] = new BlendShapeOption(GetDefaultBlendshape(rend,index));
                        }
                        ro.changeBlendShapeOptions[index].change = val;
                    }
                }
            }
        }

        void AddMenu()
        {
            menuElements.Add(new MenuElement()
            {
                name = "Menu" + menuElements.Count,
                icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.itemIcon)
            });
        }
        
        void AddFolder()
        {
            data.menuTemplate.Add(new MenuTemplate()
            {
                name = "Folder",
                icon = AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentGUIDs.itemboxIcon),
                autoCreate = false
            });
        }
        
        void ResetTemplate()
        {
            data.menuTemplate = new List<MenuTemplate>();
        }

        List<string> FindConflict()
        {
#if VRC_SDK_VRCSDK3
            AvatarModifyTool mod = new AvatarModifyTool(avatar);
            ApplySettings(mod);
            var items = new List<GameObject>();
            foreach (var menu in data.menuElements)
            {
                foreach (var item in menu.activeItems)
                {
                    items.Add(item.obj);
                }
            }

            items = items.Distinct().ToList();
            var conflictLayers = mod.HasActivateKeyframeLayers(items.ToArray()).Where(l =>
                !l.StartsWith(EnvironmentGUIDs.prefix + mod.GetSafeParam(data.saveName))).Where(l=>
                !l.StartsWith(EnvironmentGUIDs.prefix + data.saveName)).ToList();

            conflictLayers = conflictLayers.Distinct().ToList();
            return conflictLayers;
#else
            return new List<string>();
#endif
        }
        
        private void OnDestroy()
        {
            RevertObjectActiveForScene();
            SaveMaterials("Assets/Export");
        }
    }
}