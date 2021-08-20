/*
AvatarModifyTools
https://github.com/HhotateA/AvatarModifyTools

Copyright (c) 2021 @HhotateA_xR

This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using System;
using VRC.SDK3.Avatars.ScriptableObjects;
#endif

namespace HhotateA.AvatarModifyTools.Core
{
    public class MenuCreater
    {
#if VRC_SDK_VRCSDK3
        private VRCExpressionsMenu asset => menus[0];
        private List<VRCExpressionsMenu> menus;
        private int index;
        
        public bool autoNextPage = false;

        public MenuCreater(string name,bool autoNextPage = true)
        {
            menus = new List<VRCExpressionsMenu>()
            {
                ScriptableObject.CreateInstance<VRCExpressionsMenu>()
            };
            asset.name = name;
            this.autoNextPage = autoNextPage;
            index = 0;
        }

        public VRCExpressionsMenu CreateNewMenu(VRCExpressionsMenu menu = null)
        {
            index++;
            if(menu == null) menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
            menu.name = asset.name + "_" + index;
            menus.Add(menu);
            return menu;
        }

        VRCExpressionsMenu MenuSplite(
            VRCExpressionsMenu baseMenu, 
            VRCExpressionsMenu subMenu,
            int length = 8)
        {
            var controllers = new List<VRCExpressionsMenu.Control>();
            controllers.AddRange(baseMenu.controls);
            controllers.AddRange(subMenu.controls);
            baseMenu.controls = new List<VRCExpressionsMenu.Control>();
            subMenu.controls = new List<VRCExpressionsMenu.Control>();
            for (int i = 0; i < controllers.Count; i++)
            {
                if(i<length-1) baseMenu.controls.Add(controllers[i]);
                else subMenu.controls.Add(controllers[i]);
            }
            return AddNextPage(baseMenu,subMenu);
        }

        VRCExpressionsMenu MenuSplite()
        {
            if (index < 1) return null;
            return MenuSplite(menus[index - 1], menus[index]);
        }

        public void AddControll(VRCExpressionsMenu.Control controll,int i = -1)
        {
            if (i == -1) i = index;
            menus[i].controls.Add(controll);
            if (autoNextPage && menus[i].controls.Count > 8)
            {
                CreateNewMenu();
                MenuSplite();
            }
        }

        VRCExpressionsMenu AddNextPage( VRCExpressionsMenu basemenu, VRCExpressionsMenu submenu)
        {
            basemenu.controls.Add(NextPageControl(submenu));
            return basemenu;
        }

        public void AddSubMenu(VRCExpressionsMenu subMenu = null, string name = "", Texture2D icon = null)
        {
            if (subMenu == null)
            {
                var menu = ScriptableObject.CreateInstance<VRCExpressionsMenu>();
                AddControll(SubMenuControl(name,icon,menu));
                CreateNewMenu(menu);
            }
            else
            {
                AddControll(SubMenuControl(name,icon,subMenu));
            }
        }
        
        public void AddSubMenu(MenuCreater subMenu, string name = "", Texture2D icon = null)
        {
            AddControll(SubMenuControl(name,icon,subMenu.CreateAsset()));
        }

        public void AddButton(string name,Texture2D icon,string param,float val)
        {
            AddControll(ButtonControl(name,icon,param,val));
        }
        
        public void AddToggle(string name,Texture2D icon,string param,float val)
        {
            AddControll(ToggleControl(name,icon,param,val));
        }
        
        public void AddToggle(string name,Texture2D icon,string param)
        {
            AddControll(ToggleControl(name,icon,param,1));
        }

        public void AddAxis(
            string name, Texture2D icon,
            string param,
            string param_u, string param_r, string param_d = "", string param_l = "",
            string name_u = "", string name_r = "", string name_d = "", string name_l = "",
            Texture2D icon_u = null, Texture2D icon_r = null, Texture2D icon_d = null, Texture2D icon_l = null)
        {
            AddControll(AxisControl(
                name,icon,
                param,
                param_u,param_r,param_d,param_l,
                name_u,name_r,name_d,name_l,
                icon_u,icon_r,icon_d,icon_l));
        }
        public void AddAxis(
            string name, Texture2D icon,
            string param,
            string param_X, string param_Y,
            string name_u = "", string name_r = "", string name_d = "", string name_l = "",
            Texture2D icon_u = null, Texture2D icon_r = null, Texture2D icon_d = null, Texture2D icon_l = null)
        {
            AddControll(AxisControl(
                name,icon,
                param,
                param_X,param_Y,"","",
                name_u,name_r,name_d,name_l,
                icon_u,icon_r,icon_d,icon_l));
        }

        public void AddRadial(
            string name, Texture2D icon,
            string radialParam,
            string param = "", float value = 1f)
        {
            var c = new VRCExpressionsMenu.Control()
            {
                icon = icon,
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.RadialPuppet
            };

            if (!string.IsNullOrWhiteSpace(param))
            {
                c.parameter = new VRCExpressionsMenu.Control.Parameter(){name = param};
                c.value = value;
            }
            
            c.subParameters = new VRCExpressionsMenu.Control.Parameter[1]
            {
                new VRCExpressionsMenu.Control.Parameter(){name = radialParam}, 
            };
            AddControll(c);
        }

        static VRCExpressionsMenu.Control NextPageControl(VRCExpressionsMenu subMenu)
        {
            return SubMenuControl(
                    "NextPage",
                    AssetUtility.LoadAssetAtGuid<Texture2D>(EnvironmentVariable.arrowIcon),
                        subMenu);
        }
        
        static VRCExpressionsMenu.Control SubMenuControl(string name,Texture2D icon,VRCExpressionsMenu subMenu)
        {
            return new VRCExpressionsMenu.Control()
            {
                icon = icon,
                name = name,
                subMenu = subMenu,
                type = VRCExpressionsMenu.Control.ControlType.SubMenu
            };
        }
        
        static VRCExpressionsMenu.Control ToggleControl(string name,Texture2D icon,string param,float val)
        {
            return new VRCExpressionsMenu.Control()
            {
                icon = icon,
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.Toggle,
                parameter = new VRCExpressionsMenu.Control.Parameter(){name = param},
                value = val,
            };
        }
        
        static VRCExpressionsMenu.Control ButtonControl(string name,Texture2D icon,string param,float val)
        {
            return new VRCExpressionsMenu.Control()
            {
                icon = icon,
                name = name,
                type = VRCExpressionsMenu.Control.ControlType.Button,
                parameter = new VRCExpressionsMenu.Control.Parameter(){name = param},
                value = val,
            };
        }
        
        static VRCExpressionsMenu.Control AxisControl(
            string name,Texture2D icon,
            string param,
            string param_u,string param_r,string param_d = "",string param_l = "",
            string name_u = "",string name_r = "", string name_d = "",string name_l = "",
            Texture2D icon_u = null,Texture2D icon_r = null,Texture2D icon_d = null,Texture2D icon_l = null)
        {
            var c = new VRCExpressionsMenu.Control()
            {
                icon = icon,
                name = name,
            };
            
            if (string.IsNullOrWhiteSpace(param_d) && string.IsNullOrWhiteSpace(param_l))
            {
                c.type = VRCExpressionsMenu.Control.ControlType.TwoAxisPuppet;
            }
            else
            {
                c.type = VRCExpressionsMenu.Control.ControlType.FourAxisPuppet;
            }

            if (!string.IsNullOrWhiteSpace(param))
            {
                c.parameter = new VRCExpressionsMenu.Control.Parameter(){name = param};
            }
            c.subParameters = new VRCExpressionsMenu.Control.Parameter[4]
            {
                new VRCExpressionsMenu.Control.Parameter(){name = param_u}, 
                new VRCExpressionsMenu.Control.Parameter(){name = param_r}, 
                new VRCExpressionsMenu.Control.Parameter(){name = param_d}, 
                new VRCExpressionsMenu.Control.Parameter(){name = param_l}, 
            };
            c.labels = new VRCExpressionsMenu.Control.Label[4]
            {
                new VRCExpressionsMenu.Control.Label(){icon = icon_u,name = name_u}, 
                new VRCExpressionsMenu.Control.Label(){icon = icon_r,name = name_r}, 
                new VRCExpressionsMenu.Control.Label(){icon = icon_d,name = name_d}, 
                new VRCExpressionsMenu.Control.Label(){icon = icon_l,name = name_l}, 
            };
            return c;
        }
        
        public VRCExpressionsMenu Create()
        {
            return asset;
        }
        
        /// <summary>
        /// ディレクトリにアセットを作成する
        /// </summary>
        /// <returns></returns>
        public VRCExpressionsMenu CreateAsset(string path = null, bool subAsset = false)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (subAsset)
                {
                    foreach (var menu in menus)
                    {
                        AssetDatabase.AddObjectToAsset(menu,path);
                        //SaveSubMenus(menu,path);
                    }
                }
                else
                {
                    if (path.EndsWith(".asset"))
                    {
                    }
                    else
                    {
                        path = Path.Combine(path, asset.name+".asset");
                        path = AssetDatabase.GenerateUniqueAssetPath(path);
                    }
                    AssetDatabase.CreateAsset(asset,path);
                    foreach (var menu in menus)
                    {
                        //SaveSubMenus(menu,path);
                    }
                }
            }
            AssetDatabase.SaveAssets();
            return asset;
        }

        void SaveSubMenus(VRCExpressionsMenu menu,string path)
        {
            foreach (var controll in menu.controls)
            {
                if (controll.type == VRCExpressionsMenu.Control.ControlType.SubMenu &&
                    controll.subMenu != null)
                {
                    if (String.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(controll.subMenu)))
                    {
                        AssetDatabase.AddObjectToAsset(controll.subMenu,path);
                        SaveSubMenus(controll.subMenu,path);
                    }
                }
            }
            
        }
#endif
    }
}